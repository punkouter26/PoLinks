# PoLinks ClientApp

PoLinks frontend built with React + TypeScript + Vite.

## Local development

- `npm run dev`
  Runs the client in normal mode. Vite proxies `/api`, `/diagnostic`, and `/hubs` to the backend if available.

- `npm run dev:standalone`
  Runs the client with offline fallback enabled. API requests are mocked and pulse data is simulated locally.

- `npm run build`
  Type-checks and builds a production bundle.

- `npm run lint`
  Runs ESLint across client source files.

- `npm run test:e2e`
  Runs Playwright end-to-end tests.

## Runtime modes

The client runtime is controlled with Vite env vars:

- `VITE_RUNTIME_MODE=auto|standalone|hosted`
- `VITE_API_BASE_URL=http://localhost:5000`

Behavior summary:

- `auto`: tries hosted API first, falls back to local offline behavior.
- `standalone`: always uses local fallback behavior.
- `hosted`: expects backend APIs and SignalR hub to be available.

## Backend-hosted SPA flow

When running the ASP.NET app, the client is built and served from backend static assets. Use:

- `dotnet run --project ../PoLinks.Web.csproj`

## Output folders

Generated folders such as `dist`, `playwright-report`, and `test-results` are build/test artifacts and should not be committed.
