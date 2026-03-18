<!--
## Sync Impact Report
**Version Change**: N/A → 1.0.0 (initial population from template)
**Modified Principles**: N/A — first-time fill; 8 principles defined
**Added Sections**: Core Principles (I–VIII), Technology Stack, Development Workflow, Governance
**Removed Sections**: N/A
**Templates Updated**:
  - ✅ `.specify/memory/constitution.md` — written (this file)
  - ✅ `.specify/templates/plan-template.md` — Constitution Check section updated with PoLinks gates
  - ✅ `.specify/templates/tasks-template.md` — Testing requirement updated (OPTIONAL → REQUIRED for major features)
  - ✅ `.specify/templates/spec-template.md` — VSA architecture note added to Technical Context
**Deferred TODOs**: None
-->

# PoLinks Constitution

## Core Principles

### I. Vertical Slice Architecture (NON-NEGOTIABLE)

All code MUST be organized by **feature**, not by layer. Each feature slice contains its own
DTOs, business logic, validators, and API endpoints co-located in a single feature folder.
Cross-cutting concerns (auth, logging, error handling) are the only permitted horizontal layers.

**Rules**:
- Feature folders MUST NOT import from sibling feature folders directly; shared contracts go in a
  dedicated `Shared/` namespace.
- No top-level `Services/`, `Repositories/`, or `Models/` folders that span multiple features.
- Every new feature MUST start with a scoped feature folder before any code is written.
- DTOs, handlers, validators, and endpoints for a feature MUST live in the same folder.

**Rationale**: VSA reduces cognitive load, enables independent delivery of each slice, and maps
directly to user stories — making the codebase navigable for any contributor.

### II. Zero-Waste Codebase

The codebase MUST contain no dead code, unused files, or obsolete assets at any time.

**Rules**:
- Unused imports, variables, methods, classes, files, and static assets MUST be deleted when
  discovered — never commented-out.
- When a feature is removed or replaced, ALL associated files (models, tests, migrations, UI
  components) MUST be deleted in the same PR.
- Compiler/linter warnings for unused symbols MUST be treated as errors and resolved immediately.
- "TODO: remove later" blocks are forbidden; removal MUST happen in the same PR.

**Rationale**: Dead code increases maintenance burden, confuses contributors, and hides defects.
A zero-waste baseline makes diffs meaningful and reviews faster.

### III. Comprehensive Testing (NON-NEGOTIABLE)

Every major feature MUST have all three test categories passing before the feature is considered
complete. Tests MUST be written first (TDD — Red → Green → Refactor).

**Rules**:
- **Unit tests** (C#): Cover all business logic, validators, and domain rules in isolation.
  MUST fail before implementation begins.
- **Integration tests** (C#): Cover API endpoints, database interactions, and feature wiring.
  Use TestContainers or in-memory equivalents for real infrastructure simulation.
- **E2E tests** (TypeScript): Cover critical user journeys from the React UI through the full
  stack using Playwright or Cypress.
- Tests MUST be committed in the same PR as the feature code — deferral is not permitted.
- A PR MUST NOT be merged if any test category is missing or any test is failing.

**Rationale**: The three-layer pyramid ensures confidence at unit, integration, and user-journey
levels. TypeScript E2E tests validate the full React + .NET stack as a single deployable unit.

### IV. GoF/SOLID Design Patterns

Code MUST apply established design patterns (Gang of Four) and SOLID principles where they
reduce complexity or improve maintainability.

**Rules**:
- Every pattern application MUST include a short inline comment citing the pattern name and intent
  (e.g., `// Strategy pattern: allows swapping link-resolution algorithms at runtime`).
- **Single Responsibility**: each class/method MUST have one reason to change.
- **Open/Closed**: features MUST be extendable without modifying existing code where practical.
- **Dependency Inversion**: all service dependencies MUST be injected (constructor injection
  preferred); no `new ConcreteService()` inside business logic.
- Patterns MUST NOT be applied speculatively — only when a concrete need exists (YAGNI applies).

**Rationale**: Named patterns communicate intent at a glance, aid onboarding, and prevent fragile,
tangled code. Unannotated patterns are invisible abstractions that slow down future maintainers.

### V. Observability & Error Transparency

All errors MUST be logged with actionable diagnostic context, and error details MUST be surfaced
to the UI in developer-facing environments.

**Rules**:
- Every catch block or error handler MUST log: error type, message, stack trace, request context
  (route, authenticated user ID if available), and a correlation ID.
- When fixing a defect, detailed diagnostic logs MUST be added around the failure site BEFORE the
  fix is applied and MUST remain in the codebase after.
- API error responses MUST include a structured error payload: `{ code, message, correlationId }`.
- **Development/staging**: full error details (stack, context) MUST be visible in the UI.
- **Production**: detailed errors MUST be logged server-side; the UI MUST show a user-friendly
  message with the correlation ID for support escalation.

**Rationale**: Silent failures are the hardest bugs to diagnose. A consistent observability
contract accelerates debugging and reduces mean time to resolution.

### VI. Resilient React Client (Offline-First Capability)

The React application MUST remain functionally usable even when no API connection is available.

**Rules**:
- All API calls MUST be wrapped in graceful-degradation logic: show cached data, placeholder
  state, or an informative message when the API is unreachable.
- No React component MUST crash or render a blank screen due solely to an API failure.
- The app MUST display a clear, non-blocking connectivity status indicator when the API is
  unavailable.
- Mock/stub data used for offline mode MUST be clearly marked and toggled via an environment flag
  — never silently present in a production build.

**Rationale**: Users on flaky connections, developers without a running backend, and CI pipelines
all benefit from a client that degrades gracefully rather than breaking entirely.

### VII. Unified Host Architecture

The React application MUST be hosted inside the .NET server project. Only one process MUST need
to be started to run the complete application in any environment.

**Rules**:
- The React production build MUST be served as static files by the .NET app via `UseStaticFiles`
  and `MapFallbackToFile("index.html")`.
- The React build MUST be triggered automatically as part of the .NET publish pipeline
  (e.g., `dotnet publish` runs `npm run build` first).
- A single VS Code / IDE launch configuration MUST start both the .NET dev server and the React
  dev server (with hot-reload) concurrently for inner-loop development.
- No separate static hosting container or CDN setup is required for local or single-server
  deployment.

**Rationale**: A single startup point reduces operational complexity, simplifies CI/CD pipelines,
and aligns with Azure App Service's single-app deployment model.

### VIII. Clarity Before Code (NON-NEGOTIABLE)

No implementation work MUST begin on any task that is ambiguous or underspecified.

**Rules**:
- If a requirement, scope boundary, or design decision is unclear, the implementer MUST stop and
  ask clarifying questions before writing any code.
- Questions MUST be specific and reference the ambiguous requirement by ID or description.
- Silent assumptions are forbidden; all assumptions MUST be documented in the spec or task before
  implementation proceeds.
- A task is only "ready to implement" when its acceptance criteria are unambiguous and testable.

**Rationale**: The cost of ambiguity rises exponentially the later it is discovered. Asking upfront
eliminates rework and ensures the right feature is built.

## Technology Stack

**Backend**: .NET 10 Web API (C#), deployed to **Azure App Service**
**Frontend**: React (TypeScript), hosted inside the .NET app; optionally deployable to
**Azure Static Web Apps**
**Architecture**: Vertical Slice Architecture (VSA) — feature-first folder organization
**Testing**:
- Unit & Integration: xUnit (C#) with FluentAssertions and TestContainers
- E2E: Playwright (TypeScript)
**Hosting model**: React SPA served by .NET Kestrel (`UseStaticFiles` / `MapFallbackToFile`)
**Logging**: Structured logging via `ILogger<T>` with Serilog sinks (console + Azure Application
Insights in production); correlation IDs propagated on every request
**API data (React)**: TanStack Query (React Query) with stale-while-revalidate and offline
fallback support
**Auth**: TODO(AUTH_STRATEGY): authentication strategy not yet specified — clarify before
implementing any protected endpoints

## Development Workflow

### Feature Implementation Gates (in order)

Every feature MUST pass these gates sequentially before the PR is opened:

1. **Spec gate** — Feature spec exists, is approved, and all acceptance criteria are unambiguous
   and testable (Principle VIII).
2. **VSA gate** — Feature folder created under the correct slice; no cross-feature imports
   planned (Principle I).
3. **Test-first gate** — Unit and integration test skeletons written and confirmed *failing*
   before implementation begins (Principle III).
4. **Implementation gate** — Feature code written; all unit and integration tests pass green.
5. **E2E gate** — TypeScript E2E tests written and passing against the running full-stack app
   (Principle III).
6. **Zero-waste gate** — No unused files, dead imports, commented-out code, or orphaned assets
   in the PR diff (Principle II).
7. **Observability gate** — All error paths are logged with context; structured error payload
   returned by API; UI surfaces errors per environment rules (Principle V).

### Code Review Requirements

- Reviewers MUST verify all 7 gates before approving a PR.
- Any pattern application without an explanatory comment MUST be rejected (Principle IV).
- Ambiguities discovered during review MUST be resolved in the spec before merging — not
  resolved in PR comments and silently assumed (Principle VIII).

### CI/CD Pipeline

- **CI**: Build + unit + integration tests run on every PR push.
- **CD**: E2E tests run on merge to `main` against a staging environment.
- **Production release**: Single Azure App Service deployment serving both the .NET API and the
  React SPA via the unified host model (Principle VII).

## Governance

This constitution supersedes all other coding conventions, style guides, and informal practices
in this repository. When a conflict arises between this document and any other source, the
constitution takes precedence.

**Amendment procedure**:
1. Propose the amendment as a PR modifying `.specify/memory/constitution.md`.
2. Increment the version number following the semantic versioning policy below.
3. Update the Sync Impact Report HTML comment at the top of this file.
4. Propagate required changes to all dependent templates in `.specify/templates/`.
5. The PR MUST be reviewed and approved before merging.

**Versioning policy**:
- MAJOR: Removal or redefinition of an existing principle (backward-incompatible governance change).
- MINOR: Addition of a new principle or a new section with substantive guidance.
- PATCH: Wording clarification, typo fix, or non-semantic refinement.

**Compliance review**: Each sprint retrospective MUST include a constitution compliance check.
Violations MUST be filed as tracked tasks before the following sprint begins.

**Version**: 1.0.0 | **Ratified**: 2026-03-16 | **Last Amended**: 2026-03-16
