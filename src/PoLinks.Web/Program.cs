// T013: Full Serilog + OpenTelemetry host bootstrap (FR-017, FR-018).
// T017: SignalR transport registration.
// This file owns all service registration and middleware pipeline configuration.
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Events;
using System.Text.Json.Serialization;
using Microsoft.ApplicationInsights.Extensibility;
using PoLinks.Web.Features.Shared.Correlation;
using PoLinks.Web.Infrastructure.TableStorage;
using PoLinks.Web.Features.Ingestion;
using PoLinks.Web.Features.Constellation;
using PoLinks.Web.Features.Diagnostic;
using PoLinks.Web.Features.Diagnostic.HealthChecks;
using PoLinks.Web.Features.Pulse;
using PoLinks.Web.Features.Simulation;
using PoLinks.Web.Features.Snapshot;
using Microsoft.Extensions.Options;

// --------------------------------------------------------------------------
// Serilog: bootstrap logger captures startup errors before full config loads
// --------------------------------------------------------------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // -----------------------------------------------------------------------
    // Azure Key Vault: load secrets before any other service reads configuration
    // -----------------------------------------------------------------------
    var kvUri = builder.Configuration["KeyVault:Uri"];
    var hasSentimentEndpoint = !string.IsNullOrWhiteSpace(builder.Configuration["AzureAI:Language:Endpoint"]);
    var hasSentimentApiKey = !string.IsNullOrWhiteSpace(builder.Configuration["AzureAI:Language:ApiKey"]);
    var hasAppInsightsConnectionString = !string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]);

    if (!string.IsNullOrEmpty(kvUri) && (!hasSentimentEndpoint || !hasSentimentApiKey || !hasAppInsightsConnectionString))
    {
        try
        {
            builder.Configuration.AddAzureKeyVault(new Uri(kvUri), new DefaultAzureCredential());
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Failed to load Azure Key Vault configuration from {KeyVaultUri}; continuing with existing configuration.",
                kvUri);
        }
    }

    var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];

    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            options.ConnectionString = appInsightsConnectionString;
        }
    });

    // -----------------------------------------------------------------------
    // Serilog: replace default logging with Serilog (reads appsettings config)
    // -----------------------------------------------------------------------
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("logs/polinks-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7)
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<TelemetryConfiguration>(),
            TelemetryConverter.Traces));

    // -----------------------------------------------------------------------
    // OpenTelemetry: traces + metrics (FR-018)
    // -----------------------------------------------------------------------
    var otlpEndpoint = builder.Configuration["Telemetry:OtlpEndpoint"];
    builder.Services
        .AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("PoLinks.Web"))
        .WithTracing(t =>
        {
            t.AddAspNetCoreInstrumentation();
            if (!string.IsNullOrEmpty(otlpEndpoint))
                t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        })
        .WithMetrics(m =>
        {
            m.AddAspNetCoreInstrumentation();
            if (!string.IsNullOrEmpty(otlpEndpoint))
                m.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        });

    // -----------------------------------------------------------------------
    // Azure Table Storage (FR-012)
    // -----------------------------------------------------------------------
    builder.Services.AddTableStorage(builder.Configuration);
    builder.Services.AddHostedService<RetentionJob>();
    builder.Services.AddSingleton<IPostStorageRepository, PostStorageRepository>();

    // -----------------------------------------------------------------------
    // Constellation + Ingestion (T026–T032)
    // -----------------------------------------------------------------------
    builder.Services.Configure<JetstreamOptions>(builder.Configuration.GetSection(JetstreamOptions.Section));
    builder.Services.Configure<ConstellationOptions>(builder.Configuration.GetSection(ConstellationOptions.Section));
    builder.Services.AddSingleton<ConstellationService>();
    builder.Services.AddSingleton<ISentimentAnalyzer, LanguageSentimentService>();
    // Expose the same singleton via ISentimentStatus so the diagnostic endpoint can read guardrail state.
    builder.Services.AddSingleton<ISentimentStatus>(sp => (ISentimentStatus)sp.GetRequiredService<ISentimentAnalyzer>());
    builder.Services.AddHostedService<BlueskyJetstreamWorker>();
    builder.Services.AddSingleton<IMockDataService, DefaultMockDataService>();
    builder.Services.AddHostedService<PulseService>();

    // -----------------------------------------------------------------------
    // Diagnostic Terminal (T048–T056 — US5 system health)
    // -----------------------------------------------------------------------
    builder.Services.AddHealthChecks()
        .AddCheck<BlueskyApiHealthCheck>("BlueskyApi")
        .AddCheck<TableStorageHealthCheck>("TableStorage")
        .AddCheck<ConfigHealthCheck>("Configuration");
    builder.Services.AddHttpClient<BlueskyApiHealthCheck>();
    builder.Services.Configure<UptimeMetricsOptions>(builder.Configuration.GetSection("Diagnostic:Uptime"));
    builder.Services.AddSingleton<UptimeMetricsService>();

    // T059: Diagnostic log buffer — captures Information+ entries for Live Error Terminal Drawer.
    var logBuffer = new DiagnosticLogBuffer();
    builder.Services.AddSingleton(logBuffer);
    builder.Logging.AddProvider(new DiagnosticLoggerProvider(logBuffer));

    // -----------------------------------------------------------------------
    // SignalR with JSON protocol (T017) — camelCase naming to match TypeScript
    // contracts in nexus.ts.
    // -----------------------------------------------------------------------
    builder.Services.AddSignalR()
        .AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    // -----------------------------------------------------------------------
    // ASP.NET Core built-ins
    // -----------------------------------------------------------------------
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // -----------------------------------------------------------------------
    // Build and configure middleware pipeline
    // -----------------------------------------------------------------------
    var app = builder.Build();

    // Correlation ID first so all subsequent log entries carry the ID
    app.UseMiddleware<CorrelationIdMiddleware>();

    var isDevelopment = app.Environment.IsDevelopment();
    app.AddCorrelatedProblemDetails(includeExceptionDetails: isDevelopment);

    if (isDevelopment)
    {
        app.MapOpenApi();
                app.MapGet("/scalar/v1", () => Results.Content(
                        """
                        <!doctype html>
                        <html lang=\"en\">
                        <head>
                            <meta charset=\"utf-8\" />
                            <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />
                            <title>PoLinks API Reference</title>
                            <script id=\"api-reference\" data-url=\"/openapi/v1.json\"></script>
                            <script src=\"https://cdn.jsdelivr.net/npm/@scalar/api-reference\"></script>
                        </head>
                        <body></body>
                        </html>
                        """,
                        "text/html"));
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (ctx, _, ex) =>
            ex != null || ctx.Response.StatusCode > 499
                ? LogEventLevel.Error
                : LogEventLevel.Information;
    });

    if (!isDevelopment)
    {
        app.UseHttpsRedirection();
    }
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();

    app.MapControllers();

    // Constellation REST endpoints (T042 — US2 insight panel)
    app.MapConstellationEndpoints();

    // Diagnostic Terminal endpoints (T052 — US5 system diagnostics)
    app.MapDiagnosticEndpoints();

    // Snapshot export metadata endpoint (T077 — US6)
    app.MapSnapshotEndpoints();

    // SignalR hub will be mapped per-feature in Phase 3 (T026)
    app.MapHub<PulseHub>("/hubs/pulse");

    // SPA fallback -- serves ClientApp/dist/index.html for all unmatched routes (FR-001)
    app.MapFallbackToFile("index.html");

    Log.Information("PoLinks starting up");
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "PoLinks failed to start");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;