# REST API Contracts

## Constellation

### `GET /api/constellation/insight/{nodeId}`
Returns insight panel content for a selected node.

### `GET /api/constellation/ghost-snapshots?startTime={ms}&endTime={ms}`
Returns historical constellation snapshots for the ghost overlay.

### `POST /api/constellation/focus-mode/enter`
Request body:

```json
{
  "anchorId": "robotics"
}
```

Response body:

```json
{
  "anchorId": "robotics",
  "isActive": true,
  "filteredNodeCount": 4,
  "filteredLinkCount": 3,
  "enteredAt": "2026-03-17T12:34:56Z"
}
```

### `POST /api/constellation/focus-mode/exit`
Returns focus-mode exit state and restored graph counts.

### `GET /api/constellation/focus-mode/status`
Returns whether focus mode is active and which anchor is currently isolated.

## Snapshot

### `GET /api/snapshot/export-metadata?format=png&scale=2`
Returns the canonical download filename and export settings.

Response body:

```json
{
  "fileName": "polinks-constellation-20260317-142345.png",
  "contentType": "image/png",
  "format": "png",
  "scale": 2,
  "generatedAtUtc": "2026-03-17T14:23:45Z"
}
```

## Diagnostic

### `GET /diagnostic/health`
Returns deep health-check results across external dependencies.

### `GET /diagnostic/config`
Returns masked runtime configuration values.

### `GET /diagnostic/uptime`
Returns current uptime SLI using recorded probes with maintenance-window exclusion.
