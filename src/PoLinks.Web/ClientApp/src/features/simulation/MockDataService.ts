// T096: Deterministic mock data service for Simulation Mode (FR-013, FR-030).
// Same seed → identical NexusNode list; mirrors the C# DefaultMockDataService.
import type { NexusNode, NexusLink, PulseBatch } from "../../types/nexus";

function seededRandom(seed: number): () => number {
  // Mulberry32 PRNG — fast, 32-bit, reproducible
  let s = seed >>> 0;
  return () => {
    s += 0x6d2b79f5;
    let t = Math.imul(s ^ (s >>> 15), 1 | s);
    t ^= t + Math.imul(t ^ (t >>> 7), 61 | t);
    return ((t ^ (t >>> 14)) >>> 0) / 0xffffffff;
  };
}

export function generateNodes(anchorId: string, seed = 42, count = 30): NexusNode[] {
  const rng = seededRandom(seed);
  const cap = 100;
  const actual = Math.min(count, cap);

  const anchor: NexusNode = {
    id:        anchorId,
    label:     anchorId,
    type:      "Anchor",
    hypeScore: 0,
    elasticity:1.0,
    anchorId,
    firstSeen: new Date().toISOString(),
    lastSeen:  new Date().toISOString(),
  };

  const topics: NexusNode[] = Array.from({ length: actual - 1 }, (_, i) => ({
    id:         `${anchorId}-${seed}-${String(i).padStart(3, "0")}`,
    label:      `${anchorId}-${i}`,
    type:       "Topic" as const,
    hypeScore:  rng() * 10,
    elasticity: Math.max(0.1, 1.0 - (i / (actual - 1)) * 0.9),
    anchorId,
    firstSeen:  new Date(Date.now() - rng() * 3_600_000).toISOString(),
    lastSeen:   new Date().toISOString(),
  }));

  return [anchor, ...topics];
}

export function generatePulseBatch(anchorId: string, seed = 42): PulseBatch {
  const nodes = generateNodes(anchorId, seed);
  const links: NexusLink[] = nodes
    .filter((n) => n.type === "Topic")
    .map((n) => ({ sourceId: n.id, targetId: anchorId, weight: n.hypeScore }));

  return {
    anchorId,
    generatedAt: new Date().toISOString(),
    nodes,
    links,
    isSimulated: true,
  };
}

const ANCHOR_IDS = ["robotics", "dronetech", "ai", "autonomy", "sensors"] as const;

export function generateAllAnchorBatches(): PulseBatch[] {
  return ANCHOR_IDS.map((id) => generatePulseBatch(id));
}
