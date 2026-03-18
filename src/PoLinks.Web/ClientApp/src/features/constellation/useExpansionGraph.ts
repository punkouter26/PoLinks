/**
 * useExpansionGraph — manages the client-side recursive expansion layer.
 *
 * When the user double-clicks a Topic or Post node, this hook fetches the top
 * related posts from /api/constellation/related and adds them as "Post" nodes
 * attached to the clicked parent. Expansions accumulate (no collapse). Node IDs
 * are PostUris so duplicates across different expansion branches are naturally
 * deduplicated by a Set.
 *
 * Returns:
 *  - expansionNodes / expansionLinks  — merged into the ConstellationCanvas
 *  - expandNode(parent)               — trigger expansion for any Topic or Post node
 *  - isExpanding                      — true while any fetch is in flight
 *  - error                            — last network/parse error, if any
 */
import { useReducer, useCallback, useRef, useState } from "react";
import type { NexusNode, NexusLink, RelatedPostsResponse } from "../../types/nexus";

// ---- State ------------------------------------------------------------------

interface ExpansionState {
  nodes: NexusNode[];
  links: NexusLink[];
  seenPostUris: Set<string>;
}

type ExpansionAction =
  | { type: "EXPAND"; newNodes: NexusNode[]; newLinks: NexusLink[] }
  | { type: "CLEAR" };

function expansionReducer(state: ExpansionState, action: ExpansionAction): ExpansionState {
  switch (action.type) {
    case "EXPAND": {
      // Deduplicate by id — if the same PostUri comes in from two branches, keep first.
      const deduped = action.newNodes.filter((n) => !state.seenPostUris.has(n.id));
      if (deduped.length === 0) return state;

      const newSeen = new Set(state.seenPostUris);
      deduped.forEach((n) => newSeen.add(n.id));

      return {
        nodes: [...state.nodes, ...deduped],
        links: [...state.links, ...action.newLinks],
        seenPostUris: newSeen,
      };
    }
    case "CLEAR":
      return { nodes: [], links: [], seenPostUris: new Set() };
    default:
      return state;
  }
}

const INITIAL_STATE: ExpansionState = { nodes: [], links: [], seenPostUris: new Set() };

// ---- Hook -------------------------------------------------------------------

export interface UseExpansionGraphResult {
  expansionNodes: NexusNode[];
  expansionLinks: NexusLink[];
  expandNode: (parent: NexusNode) => void;
  isExpanding: boolean;
  error: string | null;
  clearExpansion: () => void;
}

export function useExpansionGraph(limit = 5): UseExpansionGraphResult {
  const [state, dispatch] = useReducer(expansionReducer, INITIAL_STATE);
  // Track in-flight expansions to avoid duplicate concurrent fetches for the same parent
  const inFlightRef = useRef<Set<string>>(new Set());
  const [isExpanding, setIsExpanding] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const expandNode = useCallback(
    (parent: NexusNode) => {
      // Only Topic and Post nodes support expansion; Anchor nodes open focus mode instead.
      if (parent.type === "Anchor") return;

      const keyword = parent.topKeyword ?? parent.label;
      const anchorId = parent.anchorId;

      if (!keyword || !anchorId) return;
      if (inFlightRef.current.has(parent.id)) return;

      inFlightRef.current.add(parent.id);
      setIsExpanding(true);
      setError(null);

      const url = `/api/constellation/related?anchorId=${encodeURIComponent(anchorId)}&keyword=${encodeURIComponent(keyword)}&limit=${limit}`;

      fetch(url)
        .then((res) => {
          if (!res.ok) throw new Error(`GET ${url} → ${res.status}`);
          return res.json() as Promise<RelatedPostsResponse>;
        })
        .then((data) => {
          const now = new Date().toISOString();

          const newNodes: NexusNode[] = data.posts
            .filter((p) => p.postUri !== parent.id) // don't re-add the parent itself
            .map((p) => ({
              id: p.postUri,
              label: p.text.length > 40 ? `${p.text.slice(0, 40)}\u2026` : p.text,
              type: "Post" as const,
              hypeScore: p.impactScore,
              elasticity: 0.5,
              anchorId,
              firstSeen: now,
              lastSeen: now,
              topKeyword: keyword,
              sentimentColour: p.sentimentColour,
            }));

          const newLinks: NexusLink[] = newNodes.map((n) => ({
            sourceId: parent.id,
            targetId: n.id,
            weight: 1,
          }));

          dispatch({ type: "EXPAND", newNodes, newLinks });
        })
        .catch((err: unknown) => {
          const message = err instanceof Error ? err.message : String(err);
          console.error("[useExpansionGraph] fetch error:", message);
          setError(message);
        })
        .finally(() => {
          inFlightRef.current.delete(parent.id);
          setIsExpanding(false);
        });
    },
    [limit, setIsExpanding, setError],
  );

  const clearExpansion = useCallback(() => {
    dispatch({ type: "CLEAR" });
  }, []);

  return {
    expansionNodes: state.nodes,
    expansionLinks: state.links,
    expandNode,
    isExpanding,
    error,
    clearExpansion,
  };
}


