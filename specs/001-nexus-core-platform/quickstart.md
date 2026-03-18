# PoLinks Quickstart

## Prerequisites

- .NET 10 SDK
- Node.js 20+
- Docker Desktop for Azurite-backed integration tests

## Run the Web App

```powershell
Set-Location src/PoLinks.Web/ClientApp
npm install
npm run build
Set-Location ../../..
dotnet build src/PoLinks.Web/PoLinks.Web.csproj
```

```powershell
dotnet run --project src/PoLinks.Web/PoLinks.Web.csproj
```

Browse to `/` for the dashboard and `/diagnostic` for diagnostics.

## Test Commands

```powershell
dotnet test tests/PoLinks.Unit/PoLinks.Unit.csproj --no-restore
```

```powershell
dotnet test tests/PoLinks.Integration/PoLinks.Integration.csproj --no-restore
```

```powershell
Set-Location src/PoLinks.Web/ClientApp
npm run test:e2e
```

## Operational Notes

- The backend serves the built React app from the unified .NET host.
- If Bluesky ingestion is unavailable, the UI falls back to Simulation Mode.
- Snapshot export downloads the main constellation canvas only, excluding UI chrome.
- Uptime SLI reporting excludes configured maintenance windows and targets 99.5% availability.

## Validation Results (2026-03-17)

### Performance Validation (T086)

- Probe method: Playwright headless runtime probe on `http://localhost:5295/?devLogin=true`, 5-second `requestAnimationFrame` sample.
- Result: `fps = 60.2` (`frames = 301`, `sampleMs = 5000`).
- Interpretation: Rendering cadence meets the 60 FPS interaction target under local Simulation Mode load.

### Observability Validation (T087)

- Probe method: Parse integration-suite server logs and measure correlation ID presence in HTTP response log lines.
- Result: `9 / 26` HTTP response lines carried an explicit correlation ID token in the log message (`34.62%` line-level coverage in this sample).
- Interpretation: Correlation IDs are present and propagated, but log-line coverage is below desired full-request visibility and should be improved in future telemetry hardening.
