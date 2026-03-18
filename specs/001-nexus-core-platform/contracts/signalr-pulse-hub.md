# SignalR Pulse Hub Contract

## Hub Route

`/hubs/pulse`

## Primary Payload

### `PulseBatch`

```json
{
  "anchorId": "robotics",
  "generatedAt": "2026-03-17T14:23:45Z",
  "nodes": [],
  "links": [],
  "isSimulated": false
}
```

## Client Expectations

- The client treats each batch as the current in-window graph for that anchor.
- JSON payloads use camelCase naming.
- Simulation Mode remains functional if live ingestion is unavailable.
- Focus Mode is applied client-side through the REST endpoints and current server snapshot projection.

## Notes

- MessagePack support remains enabled for binary consumers.
- The current SPA uses the JSON protocol for compatibility with the TypeScript client model.
