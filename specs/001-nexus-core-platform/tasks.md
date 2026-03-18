# Tasks: Robotics Semantic Nexus (PoLinks) — Core Platform

**Input**: Design documents from /specs/001-nexus-core-platform/
**Prerequisites**: plan.md, spec.md

**Tests**: Per Constitution Principle III, Unit (C#), Integration (C#), and E2E (TypeScript) tests are REQUIRED for each major user story and must be written first.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize repository-wide standards, package management, and baseline solution layout.

- [X] T001 Create solution and project skeleton in PoLinks.sln
- [X] T002 Configure global compiler policy in Directory.Build.props
- [X] T003 Configure Central Package Management versions in Directory.Packages.props
- [X] T004 [P] Add backend package references in src/PoLinks.Web/PoLinks.Web.csproj
- [X] T005 [P] Add frontend package dependencies in src/PoLinks.Web/ClientApp/package.json
- [X] T006 [P] Add local Azurite container configuration in docker-compose.yml
- [X] T007 [P] Add baseline app settings and secrets placeholders in src/PoLinks.Web/appsettings.Development.json
- [X] T008 [P] Configure unified host build pipeline (npm build before publish) in src/PoLinks.Web/PoLinks.Web.csproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build shared cross-cutting infrastructure required by all user stories.

**CRITICAL**: No user story implementation starts until this phase is complete.

- [X] T009 Create shared entity contracts in src/PoLinks.Web/Features/Shared/Entities/NexusEntities.cs
- [X] T010 [P] Implement correlation ID middleware in src/PoLinks.Web/Features/Shared/Correlation/CorrelationIdMiddleware.cs
- [X] T011 [P] Implement ProblemDetails error response mapping in src/PoLinks.Web/Features/Shared/Correlation/ErrorResponseExtensions.cs
- [X] T012 [P] Implement sensitive value masking utility in src/PoLinks.Web/Features/Shared/Masking/SensitiveValueMasker.cs
- [X] T013 [P] Configure Serilog + OpenTelemetry bootstrap in src/PoLinks.Web/Program.cs
- [X] T014 [P] Configure Azure Table Storage DI and client options in src/PoLinks.Web/Infrastructure/TableStorage/TableStorageExtensions.cs
- [X] T015 Implement 90-day retention background cleanup job in src/PoLinks.Web/Infrastructure/TableStorage/RetentionJob.cs
- [X] T016 [P] Configure Semantic Kernel + Phi-4 client registration in src/PoLinks.Web/Infrastructure/AgentOrchestration/AgentBootstrap.cs
- [X] T017 [P] Configure SignalR with MessagePack protocol in src/PoLinks.Web/Program.cs
- [X] T018 [P] Implement Simulation Mode status contract in src/PoLinks.Web/Features/Shared/Entities/SimulationModeState.cs
- [X] T019 [P] Add backend test project references and shared fixtures in tests/PoLinks.Integration/PoLinks.Integration.csproj
- [X] T020 [P] Add frontend Playwright baseline config and dev-login fixture in src/PoLinks.Web/ClientApp/tests/e2e/playwright.config.ts

**Checkpoint**: Foundation complete; user stories may proceed in priority order.

---

## Phase 3: User Story 1 - Live Constellation View (Priority: P1) 🎯 MVP

**Goal**: Deliver real-time constellation rendering with heartbeat pulse, node physics, and fallback simulation.

**Independent Test**: In Simulation Mode, Cold Start animation completes, Anchors render, pulse updates arrive every 10s, pan/zoom is smooth, and node fade-out/removal works.

### Tests for User Story 1 (write first, fail first)

- [X] T021 [P] [US1] Add unit tests for Hype Score and elasticity rules in tests/PoLinks.Unit/Constellation/HypeScoreCalculatorTests.cs
- [X] T022 [P] [US1] Add unit tests for node cap eviction ordering in tests/PoLinks.Unit/Constellation/NodeCapEnforcerTests.cs
- [X] T023 [P] [US1] Add integration tests for pulse batch assembly and broadcast in tests/PoLinks.Integration/Pulse/PulseBroadcastIntegrationTests.cs
- [X] T024 [P] [US1] Add integration tests for Jetstream reconnect cursor handling in tests/PoLinks.Integration/Ingestion/JetstreamReconnectIntegrationTests.cs
- [X] T025 [P] [US1] Add E2E test for cold start + pulse animation flow in src/PoLinks.Web/ClientApp/tests/e2e/nexus-live-constellation.spec.ts
- [X] T088 [P] [US1] Add unit tests for pulse countdown cadence (FR-011) in tests/PoLinks.Unit/Pulse/PulseCountdownTests.cs
- [X] T089 [P] [US1] Add E2E test for countdown visibility, 1-second updates, and reset-on-pulse (FR-011) in src/PoLinks.Web/ClientApp/tests/e2e/pulse-countdown.spec.ts
- [X] T091 [P] [US1] Add unit tests for renderer capability classification and profile switching (FR-032) in tests/PoLinks.Unit/Constellation/RendererProfileTests.cs
- [X] T092 [P] [US1] Add E2E test for adaptive renderer fallback on constrained device profile (FR-032) in src/PoLinks.Web/ClientApp/tests/e2e/adaptive-renderer.spec.ts
- [X] T094 [P] [US1] Add simulation/live parity unit tests for hype and physics rules (FR-030) in tests/PoLinks.Unit/Simulation/SimulationParityTests.cs
- [X] T095 [P] [US1] Add integration tests for Simulation Mode PulseBatch contract parity (FR-030) in tests/PoLinks.Integration/Simulation/SimulationParityIntegrationTests.cs

### Implementation for User Story 1

- [X] T026 [P] [US1] Implement Jetstream hosted worker ingestion loop in src/PoLinks.Web/Features/Ingestion/BlueskyJetstreamWorker.cs
- [X] T027 [P] [US1] Implement manager/worker agent orchestration for sentiment in src/PoLinks.Web/Features/Ingestion/ManagerAgent.cs
- [X] T028 [P] [US1] Implement sentiment worker with Phi-4 client in src/PoLinks.Web/Features/Ingestion/SentimentAgent.cs
- [X] T029 [US1] Implement ingestion DTO mapping and keyword filtering in src/PoLinks.Web/Features/Ingestion/IngestionDtos.cs
- [X] T030 [US1] Implement 60-minute rolling constellation state service in src/PoLinks.Web/Features/Constellation/ConstellationService.cs
- [X] T031 [US1] Implement hard-cap eviction policy in src/PoLinks.Web/Features/Constellation/NodeCapEnforcer.cs
- [X] T032 [US1] Implement pulse scheduler and batch composer in src/PoLinks.Web/Features/Pulse/PulseService.cs
- [X] T033 [US1] Implement SignalR Pulse hub endpoint in src/PoLinks.Web/Features/Pulse/PulseHub.cs
- [X] T034 [P] [US1] Implement client SignalR context with fallback to mock service in src/PoLinks.Web/ClientApp/src/context/SignalRContext.tsx
- [X] T035 [P] [US1] Implement D3 Canvas physics scene and anchor rendering in src/PoLinks.Web/ClientApp/src/features/constellation/ConstellationCanvas.tsx
- [X] T036 [US1] Implement pulse animation and radar ripple orchestration in src/PoLinks.Web/ClientApp/src/features/constellation/pulseAnimation.ts
- [X] T037 [US1] Implement pan/zoom controller and camera transition hooks in src/PoLinks.Web/ClientApp/src/features/constellation/usePanZoom.ts
- [X] T038 [US1] Implement Simulation Mode banner and state transitions in src/PoLinks.Web/ClientApp/src/features/simulation/SimulationBanner.tsx
- [X] T090 [P] [US1] Implement countdown progress bar UI and timer state wiring (FR-011) in src/PoLinks.Web/ClientApp/src/features/constellation/CountdownProgressBar.tsx
- [X] T093 [P] [US1] Implement renderer capability detection and adaptive profile selection (FR-032) in src/PoLinks.Web/ClientApp/src/features/constellation/rendererProfile.ts
- [X] T096 [P] [US1] Implement deterministic mock data generation parity with live rules (FR-030) in src/PoLinks.Web/ClientApp/src/features/simulation/MockDataService.ts
- [X] T097 [P] [US1] Implement Classic Cyber theme tokens and constellation layer styling (FR-012) in src/PoLinks.Web/ClientApp/src/styles/tokens.ts

**Checkpoint**: US1 is fully functional and demoable as MVP.

---

## Phase 4: User Story 2 - Contextual Insight Panel (Priority: P2)

**Goal**: Deliver node-click insight panel with semantic breadcrumbs and impact-ranked sentiment feed.

**Independent Test**: Clicking a node opens panel <300ms, shows semantic roots and impact-sorted posts with sentiment colors.

### Tests for User Story 2 (write first, fail first)

- [X] T039 [P] [US2] Add unit tests for impact score sort and sentiment color mapping in tests/PoLinks.Unit/Insight/InsightPanelLogicTests.cs
- [X] T040 [P] [US2] Add integration tests for insight feed endpoint projection in tests/PoLinks.Integration/Insight/InsightEndpointIntegrationTests.cs
- [X] T041 [P] [US2] Add E2E test for node click to panel workflow in src/PoLinks.Web/ClientApp/tests/e2e/insight-panel.spec.ts

### Implementation for User Story 2

- [X] T042 [P] [US2] Implement insight query endpoint and DTOs in src/PoLinks.Web/Features/Constellation/ConstellationEndpoints.cs
- [X] T043 [P] [US2] Implement semantic roots resolver service in src/PoLinks.Web/Features/Constellation/SemanticRootsResolver.cs
- [X] T044 [US2] Implement right-side panel shell and slide transition in src/PoLinks.Web/ClientApp/src/features/insight-panel/InsightPanel.tsx
- [X] T045 [P] [US2] Implement breadcrumbs component in src/PoLinks.Web/ClientApp/src/features/insight-panel/SemanticRootsBreadcrumbs.tsx
- [X] T046 [P] [US2] Implement impact-sorted post list and sentiment chips in src/PoLinks.Web/ClientApp/src/features/insight-panel/ImpactFeedList.tsx
- [X] T047 [US2] Wire node selection events to insight panel state in src/PoLinks.Web/ClientApp/src/features/insight-panel/useInsightPanelState.ts

**Checkpoint**: US2 operates independently with live or simulation data.

---

## Phase 5: User Story 5 - System Diagnostic Terminal (Priority: P2)

**Goal**: Provide transparent diagnostics via deep health checks, masked config values, and live error terminal.

**Independent Test**: /diagnostic shows connection cards, masked values, and streaming log entries with correlation IDs.

### Tests for User Story 5 (write first, fail first)

- [X] T048 [P] [US5] Add unit tests for masking rules including short keys in tests/PoLinks.Unit/Diagnostic/SensitiveValueMaskerTests.cs
- [X] T049 [P] [US5] Add integration tests for deep health endpoint responses in tests/PoLinks.Integration/Diagnostic/HealthChecksIntegrationTests.cs
- [X] T050 [P] [US5] Add integration tests for correlationId propagation in tests/PoLinks.Integration/Diagnostic/CorrelationIdIntegrationTests.cs
- [X] T051 [P] [US5] Add E2E test for diagnostic page and log drawer in src/PoLinks.Web/ClientApp/tests/e2e/diagnostic-terminal.spec.ts

### Implementation for User Story 5

- [X] T052 [P] [US5] Implement diagnostic route endpoints in src/PoLinks.Web/Features/Diagnostic/DiagnosticEndpoints.cs
- [X] T053 [P] [US5] Implement bluesky deep health check in src/PoLinks.Web/Features/Diagnostic/HealthChecks/BlueskyApiHealthCheck.cs
- [X] T054 [P] [US5] Implement table storage deep health check in src/PoLinks.Web/Features/Diagnostic/HealthChecks/TableStorageHealthCheck.cs
- [X] T055 [P] [US5] Implement configuration validity health check in src/PoLinks.Web/Features/Diagnostic/HealthChecks/ConfigHealthCheck.cs
- [X] T056 [US5] Implement live error terminal stream publisher in src/PoLinks.Web/Features/Diagnostic/DiagnosticLogStreamService.cs
- [X] T057 [US5] Implement diagnostic UI page and cards in src/PoLinks.Web/ClientApp/src/features/diagnostic/DiagnosticPage.tsx
- [X] T058 [P] [US5] Implement masked config viewer component in src/PoLinks.Web/ClientApp/src/features/diagnostic/MaskedConfigPanel.tsx
- [X] T059 [P] [US5] Implement live terminal drawer component in src/PoLinks.Web/ClientApp/src/features/diagnostic/LiveErrorTerminalDrawer.tsx

**Checkpoint**: US5 is independently testable and operationally useful.

---

## Phase 6: User Story 3 - Ghost Constellation History (Priority: P3)

**Goal**: Show historical 60-minute ghost layer at 10% opacity independent of live physics movement.

**Independent Test**: Enabling ghost overlay renders frozen historical positions; toggling off removes layer immediately.

### Tests for User Story 3 (write first, fail first)

- [X] T060 [P] [US3] Add unit tests for historical snapshot retention in tests/PoLinks.Unit/Ghost/GhostSnapshotStoreTests.cs
- [X] T061 [P] [US3] Add integration tests for ghost snapshot API shape in tests/PoLinks.Integration/Ghost/GhostSnapshotIntegrationTests.cs
- [X] T062 [P] [US3] Add E2E test for ghost toggle behavior in src/PoLinks.Web/ClientApp/tests/e2e/ghost-constellation.spec.ts

### Implementation for User Story 3

- [X] T063 [P] [US3] Implement historical snapshot store in src/PoLinks.Web/Features/Constellation/GhostSnapshotStore.cs
- [X] T064 [US3] Implement ghost snapshot endpoint in src/PoLinks.Web/Features/Constellation/ConstellationEndpoints.cs
- [X] T065 [US3] Implement ghost toggle state and fetch hook in src/PoLinks.Web/ClientApp/src/features/ghost/useGhostConstellation.ts
- [X] T066 [US3] Implement ghost layer rendering with 10% opacity in src/PoLinks.Web/ClientApp/src/features/ghost/GhostLayerCanvas.tsx

**Checkpoint**: US3 is independently releasable without regressions to US1/US2/US5.

---

## Phase 7: User Story 4 - Focus Mode (Priority: P3)

**Goal**: Isolate a selected Anchor ecosystem and restore full view on exit.

**Independent Test**: Double-clicking an anchor filters unrelated nodes, centers anchor, and exit restores full graph.

### Tests for User Story 4 (write first, fail first)

- [X] T067 [P] [US4] Add unit tests for anchor-connected subgraph filtering in tests/PoLinks.Unit/Focus/FocusModeFilterTests.cs
- [X] T068 [P] [US4] Add integration tests for focus-mode pulse filtering in tests/PoLinks.Integration/Focus/FocusModeIntegrationTests.cs
- [X] T069 [P] [US4] Add E2E test for focus entry/exit interactions in src/PoLinks.Web/ClientApp/tests/e2e/focus-mode.spec.ts

### Implementation for User Story 4

- [X] T070 [P] [US4] Implement focused-subgraph resolver service in src/PoLinks.Web/Features/Constellation/FocusSubgraphResolver.cs
- [X] T071 [US4] Implement focus mode server state projection in src/PoLinks.Web/Features/Constellation/ConstellationService.cs
- [X] T072 [US4] Implement anchor double-click focus interactions in src/PoLinks.Web/ClientApp/src/features/focus-mode/useFocusMode.ts
- [X] T073 [US4] Implement focus mode UI controls and exit affordance in src/PoLinks.Web/ClientApp/src/features/focus-mode/FocusModeControls.tsx

**Checkpoint**: US4 is independently testable with no dependency on US3.

---

## Phase 8: User Story 6 - Snapshot Export (Priority: P4)

**Goal**: Export current constellation as a high-resolution image without UI chrome.

**Independent Test**: Export action downloads timestamped image matching node/link/label positions in current canvas.

### Tests for User Story 6 (write first, fail first)

- [X] T074 [P] [US6] Add unit tests for snapshot filename and metadata formatting in tests/PoLinks.Unit/Snapshot/SnapshotNamingTests.cs
- [X] T075 [P] [US6] Add integration tests for snapshot export endpoint contract in tests/PoLinks.Integration/Snapshot/SnapshotEndpointIntegrationTests.cs
- [X] T076 [P] [US6] Add E2E test for export snapshot workflow in src/PoLinks.Web/ClientApp/tests/e2e/snapshot-export.spec.ts

### Implementation for User Story 6

- [X] T077 [P] [US6] Implement snapshot endpoint and metadata contract in src/PoLinks.Web/Features/Snapshot/SnapshotEndpoints.cs
- [X] T078 [US6] Implement client canvas capture utility excluding UI chrome in src/PoLinks.Web/ClientApp/src/features/snapshot/captureCanvasSnapshot.ts
- [X] T079 [US6] Implement export button flow and download trigger in src/PoLinks.Web/ClientApp/src/features/snapshot/ExportSnapshotButton.tsx

**Checkpoint**: US6 is complete and independently testable.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final hardening across stories.

- [X] T080 [P] Add architecture and runbook documentation updates in specs/001-nexus-core-platform/quickstart.md
- [X] T081 [P] Add API and SignalR contract documentation in specs/001-nexus-core-platform/contracts/signalr-pulse-hub.md
- [X] T082 [P] Add REST endpoint contract documentation in specs/001-nexus-core-platform/contracts/rest-api.md
- [X] T099 [P] Add research decisions and dependency rationale artifact in specs/001-nexus-core-platform/research.md
- [X] T100 [P] Add canonical entity/contract model artifact in specs/001-nexus-core-platform/data-model.md
- [X] T083 Run full unit test suite and fix failures in tests/PoLinks.Unit
- [X] T084 Run full integration test suite and fix failures in tests/PoLinks.Integration
- [X] T085 Run full Playwright headed suite and fix failures in src/PoLinks.Web/ClientApp/tests/e2e
- [X] T086 Execute performance validation for 100-node target and document results in specs/001-nexus-core-platform/quickstart.md
- [X] T087 Execute observability validation for correlationId coverage in src/PoLinks.Web/Features/Diagnostic/DiagnosticLogStreamService.cs
- [X] T098 [P] Add uptime SLI instrumentation and reporting with maintenance-window exclusion (SC-011) in src/PoLinks.Web/Features/Diagnostic/UptimeMetricsService.cs

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (Phase 1) has no dependencies and starts immediately.
- Foundational (Phase 2) depends on Setup and blocks all user stories.
- User stories depend on Foundational completion.
- Priority order is US1 → (US2, US5) → (US3, US4) → US6.
- Polish (Phase 9) depends on all selected user stories being complete.

### User Story Dependencies

- US1 (P1) is the MVP baseline and has no user-story dependencies.
- US2 depends on US1 node selection and data feed availability.
- US5 depends only on Foundational and can run in parallel with US2.
- US3 depends on US1 constellation state history, but not on US2/US5.
- US4 depends on US1 graph topology, but not on US3.
- US6 depends on US1 rendering and can be deferred to final increment.

### Within Each User Story

- Tests are written first and must fail before implementation.
- Backend services and DTOs precede frontend wiring.
- Frontend interactions follow contract readiness.
- Story completes only when Unit + Integration + E2E all pass.

## Parallel Opportunities

- Setup parallel: T004, T005, T006, T007, T008.
- Foundational parallel: T010, T011, T012, T013, T014, T016, T017, T018, T019, T020.
- US1 test parallel: T021-T025.
- US1 implementation parallel: T026/T027/T028 and T034/T035.
- US2 and US5 phases can execute in parallel after US1.
- US3 and US4 can execute in parallel after US1.
- US6 can run in parallel with late polish docs once US1 baseline is stable.

## Parallel Example: User Story 1

- Launch T021, T022, T023, T024, T025 together (all test files are independent).
- Launch T026, T027, T028 in parallel (different backend files).
- Launch T034 and T035 in parallel (frontend context vs canvas rendering).

## Implementation Strategy

### MVP First (US1 only)

1. Complete Phase 1 and Phase 2.
2. Complete US1 tests and implementation.
3. Validate MVP with T025 and performance checks.
4. Demo/deploy MVP baseline.

### Incremental Delivery

1. Deliver US1 (MVP).
2. Deliver US2 and US5 as next-value increment.
3. Deliver US3 and US4 for analytical depth and focus controls.
4. Deliver US6 export and execute final polish.

### Parallel Team Strategy

1. Team jointly completes Setup + Foundational.
2. Developer A leads US1 core physics + pulse.
3. Developer B leads US2 insight panel while Developer C leads US5 diagnostics.
4. Developer D handles US3/US4 overlays and focus flows.
5. US6 and Phase 9 are shared hardening and release preparation.

## Notes

- [P] tasks touch different files and have no incomplete-task dependency.
- [US#] labels map each task to a single user story for traceability.
- Every story has explicit independent test criteria from spec.md.
- FR-011, FR-012, FR-030, FR-032, and SC-011 have explicit remediation tasks in Phase 9 (T088-T098).
- Commit by logical task batch to keep PRs reviewable.
