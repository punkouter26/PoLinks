# PoLinks

PoLinks is a unified .NET + React platform for real-time robotics trend intelligence. It ingests social stream data, computes hype and sentiment, and renders a pulse-driven constellation with integrated diagnostics and snapshot export workflows.

## Consolidated Architecture Overview
- Unified host model: backend and SPA deployed together on Azure App Service.
- Real-time delivery: SignalR pulse hub updates clients on the heartbeat cycle.
- Observability-first: diagnostic endpoints, structured logs, correlation IDs, and uptime metrics.
- Data services: relational state, cache acceleration, and table-based time-series persistence.

## Documentation Map
- [Architecture.mmd](docs/Architecture.mmd)
- [Architecture_SIMPLE.mmd](docs/Architecture_SIMPLE.mmd)
- [SystemFlow.mmd](docs/SystemFlow.mmd)
- [SystemFlow_SIMPLE.mmd](docs/SystemFlow_SIMPLE.mmd)
- [DataModel.mmd](docs/DataModel.mmd)
- [DataModel_SIMPLE.mmd](docs/DataModel_SIMPLE.mmd)
- [ProductSpec.md](docs/ProductSpec.md)
- [DevOps.md](docs/DevOps.md)
- [screenshots/dashboard.png](docs/screenshots/dashboard.png)
- [screenshots/diagnostic.png](docs/screenshots/diagnostic.png)

## Local Run
1. docker compose up -d
2. dotnet build src/PoLinks.Web/PoLinks.Web.csproj
3. cd src/PoLinks.Web/ClientApp
4. npm run dev:standalone

## Notes
- The docs folder has been rebuilt around high-signal, glanceable assets.
- Existing detailed implementation assets in specs/ are preserved for engineering depth.
