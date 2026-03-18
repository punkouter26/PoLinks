# Research Notes

## Key Decisions

- Use a unified .NET host plus built React assets to keep deployment to a single App Service artifact.
- Use SignalR for pulse delivery rather than polling so the 10-second heartbeat remains synchronized across clients.
- Keep snapshot export browser-side; the server provides canonical metadata and filenames, while the browser captures only the render canvas and excludes UI chrome.
- Use a rolling in-memory constellation snapshot model for focus mode and ghost mode, with API endpoints projecting filtered state.
- Track uptime through lightweight probe recording and exclude configured maintenance windows from the SLI calculation.

## Dependency Rationale

- `d3` remains the physics/rendering backbone for the main constellation canvas.
- `@tanstack/react-query` is used for ghost snapshot retrieval and can be extended for other pull-based overlays.
- OpenTelemetry and Serilog remain the primary observability tools for backend health and tracing.
