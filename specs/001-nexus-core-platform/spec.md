# Feature Specification: Robotics Semantic Nexus (PoLinks) — Core Platform

**Feature Branch**: `001-nexus-core-platform`  
**Created**: 2026-03-16  
**Status**: Ready for Implementation  
**Input**: Full product description — Robotics Semantic Nexus, Semantic Physics Engine, three-page UI

> **PoLinks Constitution alignment** (verify before submitting spec for review):
> - [x] Feature scope maps to a single VSA slice (Principle I) — this IS the initial full platform slice
> - [x] Acceptance criteria are unambiguous and testable — no open questions (Principle VIII)
> - [x] Test categories (Unit C#, Integration C#, E2E TypeScript) noted in requirements (Principle III)
> - [x] React offline behaviour described where the feature touches the UI (Principle VI) — Simulation Mode defined

## Clarifications

### Session 2026-03-16

- Q: What is the real-time data transport protocol between the .NET server and the React client? → A: **SignalR with MessagePack binary serialization** — the server broadcasts `PulseBatch` events to all connected React clients over a persistent SignalR connection; HTTP polling superseded by server-push delivery *(updated during planning phase 2026-03-16)*
- Q: What is the maximum number of simultaneous Super Hub nodes the constellation must support? → A: 100 nodes hard cap; server enforces this limit by evicting lowest-Hype-Score nodes first when the cap is reached
- Q: What is the upstream social data source the .NET server ingests from? → A: Bluesky Jetstream WebSocket (AT Protocol) for live ingest; author follower counts via `app.bsky.actor.getProfile` map to `authorityWeight`
- Q: What accessibility standard must the platform meet? → A: None — the platform is a visual-only experience; no WCAG compliance, screen-reader support, or keyboard navigation is required for v1
- Q: What is the uptime / availability target for the platform? → A: 99.5% soft target on Azure App Service Standard tier; acceptance and measurement follow SC-011, with Simulation Mode covering brief outages and no formal incident-response process required for v1

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Live Constellation View (Priority: P1)

A researcher opens the application for the first time and sees a "Classic Cyber" themed dark interface. After a brief "Cold Start" scanning-radar animation, the five Pinned Anchor nodes (e.g., Robotics, Reinforcement Learning, ML-Agents, NVIDIA Isaac, ROS) appear at their fixed positions. Every 10 seconds a Global Pulse fires: new Super Hub nodes animate in with a flash of light, and Organic Bezier links form between them and their nearest Anchors. The researcher can zoom and pan the 2D canvas freely to explore the emerging constellation.

**Why this priority**: Without a working physics canvas, no other feature can be meaningfully demonstrated. This is the core value proposition — the living data visualization.

**Independent Test**: Load the app in Simulation Mode (no live API), confirm the Cold Start radar animates, Anchor nodes appear, at least one Pulse fires within 15 seconds, new Super Hubs appear with connecting links, and the map is freely pan/zoomable.

**Acceptance Scenarios**:

1. **Given** the app is loaded in Simulation Mode, **When** the page fully renders, **Then** a scanning radar animation plays over an empty grid for at most 3 seconds before Anchor nodes appear.
2. **Given** Anchor nodes are visible, **When** 10 seconds elapse, **Then** between 1 and 5 new Super Hub nodes appear with an entrance flash animation and Organic Bezier links connect them to semantically related Anchors.
3. **Given** the constellation has nodes, **When** the user pinches to zoom or drags to pan, **Then** the canvas responds fluidly with no visible lag and all node labels scale/reposition correctly.
4. **Given** a Super Hub is present, **When** the Hype Score drops to zero, **Then** the node undergoes a 30-second linear fade-out and is removed from the physics simulation, causing surrounding nodes to elastically reposition.
5. **Given** a large new Super Hub arrives, **When** it enters the canvas, **Then** surrounding nodes visibly shift with slight elasticity (spring-like displacement and return).

---

### User Story 2 — Contextual Insight Panel (Priority: P2)

A researcher clicks on any node in the constellation. A right-side panel slides into view, displaying the "Semantic Roots" breadcrumb trail at the top (e.g., Robotics → NVIDIA Isaac → New API), followed by an impact-sorted feed of social posts driving the trend. Each post is colour-coded by sentiment (Electric Blue for positive, Deep Crimson for negative). The researcher can read the posts that best explain why this node is trending.

**Why this priority**: The constellation alone shows spatial relationships; this panel provides the textual intelligence that explains them. Together they form the core "why + where" research loop.

**Independent Test**: Click any node in Simulation Mode, confirm the panel slides in, breadcrumbs show two or more ancestor nodes, at least 3 mock posts appear sorted by descending impact score, and sentiment colour coding is visible.

**Acceptance Scenarios**:

1. **Given** a node is visible on the canvas, **When** the user clicks it, **Then** the right-side panel slides in from the right within 300 ms and the clicked node gets a "selected" visual highlight.
2. **Given** the panel is open, **When** it hydrates, **Then** Semantic Roots breadcrumbs show at least one Anchor ancestor and the immediate parent node.
3. **Given** the feed is populated, **When** the user views it, **Then** posts are ordered by descending impact score, each post displays author, text, impact value, and a visible sentiment colour chip.
4. **Given** a post has positive sentiment, **When** it is rendered, **Then** its left border or background accent is Electric Blue (#00BFFF range); for negative sentiment it is Deep Crimson (#DC143C range).
5. **Given** a new Pulse fires while the panel is open, **When** the node's feed updates, **Then** new posts are prepended without the panel closing or resetting scroll position.

---

### User Story 3 — Ghost Constellation History (Priority: P3)

A researcher toggles the "Ghost Constellation" button. A faint 10% opacity shadow-layer overlay appears showing node positions from the previous 60 minutes. The researcher can visually compare the current constellation with its historical state to observe semantic drift.

**Why this priority**: Temporal context is a distinct, optional capability. Delivering P1 and P2 first produces a complete research tool; ghost history enhances it.

**Independent Test**: Enable Ghost toggle in Simulation Mode with at least 2 completed Pulses of history, confirm ghost layer appears at reduced opacity, ghost positions differ from current positions, and toggling off removes the layer.

**Acceptance Scenarios**:

1. **Given** at least one prior Pulse has occurred, **When** the user activates the Ghost toggle, **Then** a semi-transparent (≤10% opacity) shadow layer of prior node positions is rendered beneath the live layer.
2. **Given** the Ghost toggle is active, **When** a new Pulse fires and nodes move, **Then** the ghost layer does not move — it remains frozen at its historical snapshot positions.
3. **Given** the Ghost toggle is active, **When** the user deactivates it, **Then** the ghost layer disappears immediately with no residual artefacts.

---

### User Story 4 — Focus Mode (Priority: P3)

A researcher double-clicks a Pinned Anchor node. The UI performs a global cleanup: all nodes and links not connected to that Anchor's direct ecosystem are hidden. The selected Anchor is centred on screen. The researcher can study a single sector in isolation, then exit Focus Mode to return to the full constellation.

**Why this priority**: Focus Mode is a productivity feature for dense maps. Core constellation and panel features deliver value without it.

**Independent Test**: Double-click an Anchor in Simulation Mode with 5+ nodes present; confirm only nodes connected to that Anchor remain visible, the selected Anchor is centred, an exit affordance is available, and activating it restores all nodes.

**Acceptance Scenarios**:

1. **Given** multiple nodes are on the canvas, **When** the user double-clicks an Anchor, **Then** all nodes with no path to that Anchor are hidden and the Anchor is animated to 50% of the viewport within 500 ms.
2. **Given** Focus Mode is active, **When** a new Pulse fires, **Then** only Super Hubs connected to the focused Anchor are rendered; others are discarded silently.
3. **Given** Focus Mode is active, **When** the user clicks an "Exit Focus" button or double-clicks the Anchor again, **Then** the full constellation is restored with a smooth unhide animation.

---

### User Story 5 — System Diagnostic Terminal (Priority: P2)

Any user navigates to the System Diagnostic page. They see the live health status of every external connection (data source, API endpoints). A masked JSON config block shows configuration values with sensitive keys partially obscured (middle characters replaced with asterisks). A toggleable Live Error Terminal drawer shows structured log entries with correlation IDs. The user can trust the system's health at a glance.

**Why this priority**: Transparency and trust are first-class product values. The diagnostic page is accessible to all users and supports operational confidence without requiring developer access.

**Independent Test**: Navigate to `/diagnostic`, confirm at least one health-check card displays a status, the masked config block shows at least one key where middle characters are replaced with `*`, open the error drawer and confirm structured log entries with `correlationId` fields are visible in Simulation Mode.

**Acceptance Scenarios**:

1. **Given** the user navigates to the Diagnostic page, **When** it loads, **Then** every registered external connection has a status card showing: name, connection type, last-checked timestamp, and a pass/fail indicator.
2. **Given** a health check card is in "failed" status, **When** the user views it, **Then** the card shows a "Deep Check" error message (not just "offline") using a red visual indicator.
3. **Given** the masked config section is rendered, **When** a value contains a sensitive key, **Then** characters 4 through (length-3) are replaced with `*`s; first 3 and last 3 characters remain visible.
4. **Given** the Live Error Terminal drawer is toggled open, **When** it renders, **Then** each log entry has: `timestamp`, `level`, `message`, `correlationId`, and entries scroll without the page layout breaking.
5. **Given** a new error occurs during Simulation Mode, **When** it is logged, **Then** it appears at the top of the Live Error Terminal within 2 seconds of being logged.

---

### User Story 6 — Snapshot Export (Priority: P4)

A researcher clicks "Export Snapshot." The current state of the constellation is captured as a high-resolution image file and downloaded to the user's device. The snapshot faithfully represents node positions, link paths, labels, and node sizes at the moment of capture.

**Why this priority**: A useful research artefact, but purely additive — the core platform is complete without it.

**Independent Test**: Click Export in Simulation Mode with at least 3 nodes visible; confirm a file is downloaded, its contents render the correct node labels and relative positions matching the canvas state at time of export.

**Acceptance Scenarios**:

1. **Given** the Nexus page is open with at least one node, **When** the user clicks "Export Snapshot," **Then** a file download is triggered with a descriptive filename including a timestamp.
2. **Given** the export is generated, **When** it is opened, **Then** it contains all visible node labels, link paths, and Anchor labels at correct relative positions; no UI chrome (buttons, header) is included in the image.

---

### Edge Cases

- **Empty constellation**: If no Hubs have arrived yet (Cold Start), the Export Snapshot button should be disabled or export only Anchor nodes with a "No active hubs" watermark.
- **Rapid Pulse overlap**: If two Pulses fire within < 5 seconds (e.g., backlog catch-up), the physics engine MUST handle concurrent Node insertions without duplicate node IDs or overlapping positions.
- **All nodes evaporated**: If the 60-minute window clears all Hubs, the canvas should display only Anchor nodes and show a "No recent activity" indicator — not a blank screen.
- **API source offline**: The system MUST enter Simulation Mode automatically and display a non-blocking banner; no crash or blank page is acceptable.
- **Single-node cluster**: A Hub with only one connection to an Anchor MUST still render with a valid Bezier path and pulse animation.
- **Extremely high Hype Score**: A node with a Hype Score orders of magnitude above others MUST NOT overflow its label outside the viewport or obscure neighbouring nodes unrecoverably.
- **Focus Mode with no connected nodes**: If the focused Anchor has zero connected Hubs, the canvas shows only the Anchor centred with a "No connected hubs" label.
- **Cap-eviction race**: If two new Super Hubs arrive in the same Pulse batch and both would exceed the 100-node cap, the server MUST resolve evictions before sending the Pulse response — the client must never receive a payload that would push it past 100 nodes.
- **Bluesky rate-limit**: If the Bluesky API returns HTTP 429, the server MUST serve the last cached Pulse batch for that cycle and log a warning. The client MUST NOT display an error to the user for a single rate-limited cycle; Simulation Mode only triggers after two consecutive failures (FR-035).
- **Sensitive key shorter than 7 characters**: Masking logic MUST NOT produce negative substring indices; keys of 6 characters or fewer should be fully masked as `******`.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Semantic Physics Engine

- **FR-001**: The system MUST maintain a set of "Pinned Anchor" nodes representing industry pillars (Robotics, Reinforcement Learning, ML-Agents, NVIDIA Isaac, ROS). Anchors MUST NOT move from their seeded positions under any physics force.
- **FR-002**: The system MUST create "Super Hub" nodes from data stream events. A Super Hub represents a high-volume emerging topic identified within the current 60-minute rolling window.
- **FR-003**: The system MUST compute a "Hype Score" for each Super Hub using a weighted formula: mention frequency × social authority weight. The score MUST use logarithmic scaling for influencer-originated mentions.
- **FR-004**: The physical size and brightness of each Super Hub MUST be directly proportional to its current Hype Score, with continuous visual updates as the score changes.
- **FR-005**: The distance between any two nodes MUST be governed by their co-occurrence frequency within the rolling window. Higher co-occurrence MUST reduce the rest-length of the gravitational link between them, physically pulling the nodes closer.
- **FR-006**: Every link between nodes MUST be rendered as an Organic Bezier curve with a "conveyor belt" pulse animation — pulses of light travelling from Anchor to Hub, accelerating as they approach the Hub.
- **FR-007**: When a new Super Hub arrives, it MUST push neighbouring nodes with slight elasticity (spring physics), so nodes displaced by the arrival rebound to their equilibrium positions.
- **FR-008**: A node exits the 60-minute rolling window by undergoing a 30-second linear fade animation, after which it is removed from the physics world and the constellation elastically readjusts.

#### Global Pulse / Heartbeat

- **FR-009**: The .NET server MUST broadcast a `PulseBatch` message to all connected React clients via **SignalR (MessagePack binary)**. The batch payload MUST contain: new SuperHubs (if any), updated Link co-occurrence strengths, eviction timestamps for expiring nodes, and the ID of the most active node. The server assembles and broadcasts each batch on a server-side 10-second Global Heartbeat timer. On receiving a non-empty batch, the client triggers the Global Pulse animation sequence (entrance flash, link recalculation, radar ripple). The React `SignalRContext` MUST fall back to `MockDataService` automatically after two consecutive connection failures.
- **FR-010**: A scanning radar ripple MUST originate from the most active node on the map and sweep across the grid at each Pulse, visually communicating the arrival of new data.
- **FR-011**: A countdown progress bar MUST display the time remaining until the next Pulse, updating at least once per second.

#### Nexus Page (Page 1)

- **FR-012**: The page MUST use a "Classic Cyber" aesthetic: deep-black background, subtle geometric grid overlay, monospace labels for node names.
- **FR-013**: Clicking a node MUST trigger a smooth "Zoom and Centre" camera transition, bringing the node to approximately 50% of the screen area.
- **FR-014**: The canvas MUST support fluid zoom (pinch/scroll) and pan (drag) gestures with no perceptible latency on modern hardware.
- **FR-015**: Double-clicking an Anchor MUST activate Focus Mode, hiding all nodes unconnected to that Anchor's ecosystem and centering the Anchor.
- **FR-016**: Focus Mode MUST provide a clearly labelled exit affordance that restores the full constellation.

#### Contextual Insight Panel (Page 2 / Slide-In Panel)

- **FR-017**: Clicking a node MUST slide in a right-side panel within 300 ms.
- **FR-018**: The panel MUST display "Semantic Roots" breadcrumbs showing the chain of influence from the nearest Anchor(s) to the selected node.
- **FR-019**: The panel MUST display a feed of social posts sorted by descending impact score (not chronological order).
- **FR-020**: Each post MUST be visually colour-coded by sentiment: Electric Blue for positive/excited, Deep Crimson for negative/friction, neutral colours for neutral sentiment.

#### System Diagnostic Terminal (Page 3)

- **FR-021**: A dedicated `/diagnostic` route MUST perform a "Deep Check" health verification of every registered external connection — not a simple ping, but a lightweight functional test (e.g., a minimal read operation).
- **FR-022**: Configuration values shown on the diagnostic page MUST be masked: characters 4 through (length−3) of any sensitive value MUST be replaced with asterisks. Values of 6 or fewer characters MUST be fully masked.
- **FR-023**: A toggleable Live Error Terminal drawer MUST display a scrolling feed of structured log entries, each containing: `timestamp`, `level`, `message`, `correlationId`.
- **FR-024**: Every application error MUST generate a correlation ID that is: (a) included in the server log, (b) returned in the API error response payload, and (c) displayed in the Live Error Terminal.

#### Ghost Constellation

- **FR-025**: A "Ghost Constellation" toggle MUST render a historical shadow layer at 10% opacity showing node positions from the previous 60 minutes.
- **FR-026**: The ghost layer MUST be static — it does not respond to physics updates or new Pulse arrivals while it is rendered.

#### Snapshot Export

- **FR-027**: An "Export Snapshot" action MUST capture the current canvas state as a high-resolution image and trigger a file download with a timestamped filename.
- **FR-028**: The exported image MUST include all visible nodes, their labels, and link paths. It MUST NOT include UI chrome (navigation bar, buttons, sidebar).

#### Simulation / Offline Mode

- **FR-029**: The application MUST enter "Simulation Mode" automatically when the primary data source is unavailable.
- **FR-030**: Simulation Mode MUST generate synthetic data that follows the same Hype Score, physics, and sentiment rules as live data, ensuring all UI features remain demonstrable.
- **FR-031**: A non-blocking banner MUST be displayed when Simulation Mode is active, clearly distinguishing simulated data from live data.

#### Adaptive Renderer

- **FR-032**: The renderer MUST detect client hardware capabilities at startup. High-capability devices MUST render high-density Bezier curves and complex particle physics. Low-capability or mobile devices MUST render a simplified visual profile while maintaining the core physics engine.

#### Bluesky Ingestion

- **FR-034**: The .NET server MUST maintain a **persistent Bluesky Jetstream WebSocket** connection to `wss://jetstream2.us-east.bsky.network/subscribe?wantedCollections=app.bsky.feed.post`, implemented as an `IHostedService`. The server MUST filter inbound Jetstream events by matching post text against the 5 Anchor keyword topics. For each matched post, the server MUST retrieve author follower counts via `app.bsky.actor.getProfile` (Bluesky REST) to compute the `authorityWeight` using logarithmic scaling. The server MUST persist the Jetstream cursor (`time_us`) and reconnect with `?cursor={last_time_us - 5_000_000}` for gapless replay after a disconnect. If the connection drops, the server MUST attempt reconnection with exponential back-off and serve the last cached Pulse batch to clients; Simulation Mode triggers after two consecutive reconnection failures (FR-035).
- **FR-035**: When the Bluesky API is unreachable or returns HTTP 429 for two or more consecutive Pulse cycles, the server MUST signal the client to enter Simulation Mode and log a structured error entry with a correlation ID (per FR-024).

#### Node Cap & Eviction

- **FR-033**: The server MUST enforce a hard cap of 100 simultaneously active Super Hub nodes within the 60-minute rolling window. When a new Super Hub would exceed the cap, the server MUST evict the active node with the lowest current Hype Score before admitting the newcomer. The eviction triggers the standard 30-second fade-out sequence on the client.

### Key Entities

- **Anchor**: A permanent, immovable node seeded at application start. Has: id, label, position (x, y), category, visual style. Source: static configuration.
- **SuperHub**: A transient node created from a data batch. Has: id, label, hypeScore, createdAt, expiresAt, position (x, y), size (derived from hypeScore), opacity (derived from age), parentAnchorIds[].
- **Link**: A directional connection between two nodes. Has: id, sourceNodeId, targetNodeId, coOccurrenceStrength, restLength (inverse of coOccurrenceStrength), pulseSpeed, pulsePhase.
- **Post**: A social discourse item. Has: id, authorId, text, authorAuthority, sentimentScore (−1 to +1), impactScore, publishedAt, linkedNodeIds[].
- **Author**: A social account. Has: id, displayName, followerCount, authorityWeight (logarithmically scaled from followerCount), isInfluencer.
- **Pulse**: A batch-processing event. Has: id, firedAt, newHubs[], updatedLinks[], mostActiveNodeId.
- **HealthCheck**: A diagnostic record. Has: connectionName, connectionType, status (pass/fail), lastCheckedAt, details, deepCheckMessage. Named connections include: `bluesky-api` (AT Protocol search endpoint), `pulse-cache` (server-side Pulse batch cache), `config` (application configuration validity).
- **LogEntry**: A structured log record. Has: correlationId, timestamp, level, message, context (route, userId, errorType).

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The Cold Start sequence (radar animation → Anchor nodes visible) completes within 3 seconds of the page becoming interactive.
- **SC-002**: A Global Pulse fires within 10 seconds ± 1 second of the previous Pulse (time precision within ±10%).
- **SC-003**: Users can identify the top trending topic (highest Hype Score node) without reading any documentation, within 30 seconds of first opening the app.
- **SC-004**: Clicking a node and reading the first impactful post in the Contextual Insight Panel takes under 5 seconds total (interaction response + feed visible).
- **SC-005**: The constellation supports at least 100 simultaneously active Super Hub nodes without visible frame-rate degradation (target ≥30 FPS) on a mid-range device using the adaptive renderer.
- **SC-006**: Node evaporation (30-second fade) does not cause any other node to "jump" — positional displacement during elastic snap-back MUST be visually smooth.
- **SC-007**: The Diagnostic page loads all health-check results within 5 seconds on a standard office network connection.
- **SC-008**: An Export Snapshot capture-to-download completes within 3 seconds for a standard constellation of up to 50 nodes.
- **SC-009**: The application is fully functional in Simulation Mode within 2 seconds of detecting a lost data-source connection; no user interaction is required to enter Simulation Mode.
- **SC-010**: All user-facing error messages include a correlation ID; 100% of logged errors must have this field populated.
- **SC-011**: The platform achieves a 99.5% uptime soft target on Azure App Service Standard tier. Planned maintenance windows are excluded from the calculation. Simulation Mode activates automatically within 2 seconds of detecting an upstream failure, ensuring the UI remains functional throughout any outage.

---

## Assumptions

- **A-001**: The React client receives `PulseBatch` events from the .NET server via a persistent **SignalR connection (MessagePack binary)**. The `SignalRContext` falls back to `MockDataService` automatically when the connection is unavailable. The .NET server is responsible for all upstream data ingestion via the **Bluesky Jetstream WebSocket** (`wss://jetstream*.us-east.bsky.network/subscribe`, AT Protocol); the React client never makes raw third-party calls directly.
- **A-002**: "Influencer" status is determined server-side at ingest time; the client receives a pre-computed `authorityWeight` on each Author record.
- **A-003**: The 60-minute rolling window is managed server-side; the client receives only in-window nodes and eviction timestamps.
- **A-004**: Authentication is deferred to a future spec (per `TODO(AUTH_STRATEGY)` in the constitution). The initial platform build does not require login.
- **A-005**: The React app is hosted inside the .NET 10 server project (Principle VII). The build pipeline produces a single deployable artefact suitable for Azure App Service.
- **A-006**: The "Classic Cyber" colour palette is defined in a design token file and implemented by the engineering team; no external design tool integration is required for this spec.
- **A-007**: Social authority weights are derived from Bluesky author follower counts retrieved via `app.bsky.actor.getProfile`. The .NET server computes the logarithmic `authorityWeight` at ingest time; the React client only receives the pre-computed weight value, never raw follower counts.
- **A-008**: The platform is intentionally visual-only for v1. No WCAG compliance, screen-reader support, keyboard-only canvas navigation, or ARIA live-region announcements are required. Accessibility is deferred to a future spec.
- **A-009**: The platform targets 99.5% uptime on Azure App Service Standard tier. Uptime interpretation and acceptance are defined by SC-011. No redundant deployment slots, health-probe auto-healing, or formal incident-response process is required for v1. The Simulation Mode fallback (FR-029) covers user experience during brief outages.

---

## Out of Scope (this spec)

- Accessibility / WCAG compliance (deferred — see A-008)
- User authentication and authorisation (future spec)
- Multi-user / collaborative sessions
- Historical data replay beyond the 60-minute rolling window
- Admin panel for editing Pinned Anchors
- Mobile native apps (iOS/Android) — this is a web-first platform
- Custom alert / notification system
