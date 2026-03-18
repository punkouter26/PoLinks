# Implementation Plan: Robotics Semantic Nexus (PoLinks) — Core Platform

**Branch**: `001-nexus-core-platform` | **Date**: 2026-03-16 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/001-nexus-core-platform/spec.md`

## Summary

Build the Robotics Semantic Nexus (PoLinks) as a unified .NET 10 Web API + React 19 SPA deployed to Azure App Service. The .NET server ingests live data from a persistent Bluesky Jetstream WebSocket connection, runs sentiment analysis via Phi-4 (Azure AI Foundry) using Semantic Kernel Manager/Worker orchestration, enforces a 100-node hard cap with Hype-Score-based eviction, persists batches to Azure Table Storage (YYYYMMDD partition key, 90-day retention), and broadcasts real-time Pulse batches to all connected React clients over SignalR (MessagePack binary). The React 19 client renders a 60-fps HTML5 Canvas physics constellation via D3.js, degrades gracefully to Simulation Mode when the API is unreachable, and is served as static files from the .NET Kestrel host under the unified-host architecture (Principle VII).

## Technical Context

**Language/Version**: C# 13 / .NET 10 (backend) + TypeScript 5.x / React 19 (frontend)  
**Primary Dependencies**:
- Backend: `Microsoft.SemanticKernel.Agents.Core` 1.73.0, `Microsoft.SemanticKernel.Agents.Orchestration` 1.73.0-preview, `Azure.AI.Inference` 1.0.0-beta.5, `Microsoft.Extensions.AI.AzureAIInference` 10.0.0-preview, `Microsoft.AspNetCore.SignalR.Protocols.MessagePack` 10.0.5, `Azure.Data.Tables` 12.11.0, `Azure.Security.KeyVault.Secrets`, `Serilog.AspNetCore`, `OpenTelemetry.Extensions.Hosting`
- Frontend: `react@19`, `@microsoft/signalr`, `@microsoft/signalr-protocol-msgpack`, `d3`, `tailwindcss`, `@tanstack/react-query`, `playwright`  

**Storage**: Azure Table Storage (`PartitionKey: YYYYMMDD`, 90-day custom `RetentionJob : BackgroundService`); Azurite (Docker) for local dev  
**Testing**: xUnit + FluentAssertions + TestContainers/Azurite (C# Unit + Integration); Playwright TypeScript Headed mode with `/dev-login` bypass (E2E)  
**Target Platform**: Azure App Service Standard tier (B1 staging/prod; F1 dev); single .NET 10 Kestrel host serving React SPA via `UseStaticFiles` + `MapFallbackToFile`  
**Project Type**: Unified full-stack web application  
**Performance Goals**: ≥60 fps Canvas physics with 100 simultaneous Super Hub nodes (SC-005); SignalR broadcast latency < 100 ms; Cold Start < 3 s (SC-001); Phi-4 inference < 1 s per batch  
**Constraints**: `TreatWarningsAsErrors` globally enforced; `Nullable` enabled; CPM via `Directory.Packages.props`; Azure Key Vault via System-Assigned Managed Identity in `PoShared` resource group; Bluesky Jetstream back-pressure resilience; 99.5% uptime soft target (A-009)  
**Scale/Scope**: Single-tenant; 100-node hard cap; Jetstream firehose filtered to 5 Anchor keyword topics; `YYYYMMDD` partition key time-series in Table Storage

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

> Based on the **PoLinks Constitution v1.0.0** — all items MUST be ✅ before proceeding.

| # | Principle | Gate Question | Status |
|---|-----------|---------------|--------|
| I | Vertical Slice Architecture | Does this feature have a dedicated folder? No cross-feature imports planned? | ✅ |
| II | Zero-Waste | Are all files/assets in this PR purposeful? No dead code / commented-out blocks? | ✅ |
| III | Comprehensive Testing | Are Unit (C#), Integration (C#), and E2E (TypeScript) tasks planned and written first? | ✅ |
| IV | GoF/SOLID | Are all pattern usages annotated with explanatory comments? | ✅ |
| V | Observability | Are all error paths logged with context + correlationId? UI error surfacing planned? | ✅ |
| VI | Resilient React Client | Does the React UI degrade gracefully if the API is unavailable? | ✅ |
| VII | Unified Host | Is the React build served by the .NET app? Single startup confirmed? | ✅ |
| VIII | Clarity Before Code | Are all acceptance criteria unambiguous and testable? No open questions deferred? | ✅ |

### Gate Notes

- **I**: VSA slices in `src/PoLinks.Web/Features/` — `Pulse/`, `Ingestion/`, `Constellation/`, `Diagnostic/`, `Snapshot/`. `Shared/` namespace for cross-cutting (correlation IDs, masking). No cross-feature imports. ✅
- **II**: No placeholder code; all packages intentional and documented in research.md. The archived `AgentGroupChat` API is explicitly avoided in favour of `GroupChatOrchestration`. ✅
- **III**: xUnit Unit + Integration (TestContainers + Azurite) + Playwright E2E (Headed, `/dev-login` bypass) all planned. ✅
- **IV**: Manager Agent (Strategy pattern for `SelectNextAgent`), `NodeCapEnforcer` (Chain of Responsibility), `MockDataService` (Null Object pattern), all to be annotated at implementation. ✅
- **V**: `CorrelationId` middleware on every request; `ProblemDetails` error responses with `correlationId`; Serilog + Application Insights sinks; `/diag` Live Error Terminal (FR-023); SC-010. ✅
- **VI**: `SignalRContext` falls back to `MockDataService` after 2 failures; banner shown (FR-031); Simulation Mode (FR-029–031). ✅
- **VII**: `ClientApp/` nested in `PoLinks.Web/`; MSBuild `Exec` runs `npm run build`; `UseStaticFiles`+`MapFallbackToFile("index.html")`. ✅
- **VIII**: All 35 FRs have measurable acceptance criteria; 5 clarification questions resolved; no `NEEDS CLARIFICATION` markers remain. ✅

### Spec Amendments Applied During Planning

| Original | Updated | Reason |
|---|---|---|
| Q1: HTTP Polling 10 s | **SignalR push (MessagePack)** | User architecture spec; push eliminates up-to-10s polling lag |
| FR-034: REST `searchPosts` polling | **Bluesky Jetstream WebSocket** | User architecture spec; event-driven firehose eliminates polling overhead |

> Spec updated in same session: Q1 clarification, A-001, FR-009, FR-034 revised.

## Project Structure

### Documentation (this feature)

```text
specs/001-nexus-core-platform/
├── plan.md
├── spec.md
├── tasks.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
  ├── signalr-pulse-hub.md
  └── rest-api.md
```

### Source Code (repository root)

```text
src/
└── PoLinks.Web/
  ├── Features/
  │  ├── Pulse/
  │  ├── Ingestion/
  │  ├── Constellation/
  │  ├── Diagnostic/
  │  ├── Snapshot/
  │  └── Shared/
  ├── Infrastructure/
  │  ├── AgentOrchestration/
  │  └── TableStorage/
  ├── ClientApp/
  │  ├── src/
  │  │  ├── features/
  │  │  ├── context/
  │  │  ├── services/
  │  │  └── styles/
  │  └── tests/e2e/
  ├── wwwroot/
  ├── Program.cs
  └── PoLinks.Web.csproj

tests/
├── PoLinks.Unit/
└── PoLinks.Integration/

infra/
├── main.bicep
├── app-service.bicep
└── key-vault.bicep
```

**Structure Decision**: Unified host architecture. React is nested in `src/PoLinks.Web/ClientApp` and served by the .NET app (`UseStaticFiles` + `MapFallbackToFile("index.html")`). Feature code follows VSA under `src/PoLinks.Web/Features/*` and tests are split into `tests/PoLinks.Unit` and `tests/PoLinks.Integration` plus Playwright E2E under ClientApp.

## Complexity Tracking

> Entries below are approved complexity tradeoffs for this feature.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Semantic Kernel orchestration preview package | Required for Manager/Worker agent orchestration with Phi-4 | Basic single-call inference cannot coordinate specialized workers or termination strategy |
| Custom 90-day Table retention BackgroundService | Azure Table Storage has no native lifecycle retention policy | Blob-style lifecycle rules do not apply to Tables; external job would add deployment complexity |
| SignalR + MessagePack real-time push | Needed to satisfy low-latency pulse delivery and reduce payload size | HTTP polling adds latency and avoidable request overhead for synchronized heartbeat events |
