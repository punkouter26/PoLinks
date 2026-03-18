// Shared TypeScript types mirroring the C# domain contracts in NexusEntities.cs.
export type NodeType = "Anchor" | "Topic" | "Post";
export type SentimentLabel = "Positive" | "Neutral" | "Negative";

export interface NexusNode {
  id:         string;
  label:      string;
  type:       NodeType;
  hypeScore:  number;
  elasticity: number;
  anchorId:   string;
  firstSeen:  string; // ISO-8601
  lastSeen:   string;
  authorDid?: string;
  /** Most frequently matched keyword for this author — used as the visible topic label. */
  topKeyword?: string;
  /** Sentiment colour hex — only present on client-side Post expansion nodes. */
  sentimentColour?: string;
}

export interface NexusLink {
  sourceId: string;
  targetId: string;
  weight:   number;
}

export interface PulseBatch {
  anchorId:    string;
  generatedAt: string; // ISO-8601
  nodes:       NexusNode[];
  links:       NexusLink[];
  isSimulated: boolean;
}

export interface SimulationModeState {
  isSimulated: boolean;
  reason?:     string;
  sinceUtc?:   string;
}

// ---- Insight Panel types (US2) -------------------------------------------

export interface InsightPost {
  postUri:        string;
  authorDid:      string;
  text:           string;
  impactScore:    number;
  sentiment:      SentimentLabel;
  sentimentColour: string; // CSS hex from server (spec AC-4)
  createdAt:      string;  // ISO-8601
}

export interface InsightResponse {
  nodeId:        string;
  anchorId:      string;
  semanticRoots: string[];
  posts:         InsightPost[];
}

/** Alias: the /related endpoint returns the same shape as InsightResponse. */
export type RelatedPostsResponse = InsightResponse;
