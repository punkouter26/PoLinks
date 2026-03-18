# Data Model

## Anchor

- `id`
- `label`
- `type = Anchor`
- `anchorId`
- `firstSeen`
- `lastSeen`

## Super Hub / Topic Node

- `id`
- `label`
- `type = Topic`
- `hypeScore`
- `elasticity`
- `anchorId`
- `authorDid`
- `firstSeen`
- `lastSeen`

## Link

- `sourceId`
- `targetId`
- `weight`

## Constellation Snapshot

- `nodes[]`
- `links[]`
- `createdAt`

## Focus Mode State

- `anchorId`
- `nodeIds[]`
- `enteredAt`

## Snapshot Export Metadata

- `fileName`
- `contentType`
- `format`
- `scale`
- `generatedAtUtc`

## Uptime SLI

- `uptimePercentage`
- `availableProbeCount`
- `totalProbeCount`
- `targetPercentage`
- `asOfUtc`
- `note`
