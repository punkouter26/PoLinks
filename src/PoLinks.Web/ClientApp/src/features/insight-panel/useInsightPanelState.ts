// T047: Insight panel state — manages selected node and fetches insight data (US2).
import { useState, useEffect, useCallback, useRef } from "react";
import type { InsightResponse } from "../../types/nexus";
import { apiFetch } from "../../utils/apiFetch";

export interface InsightPanelState {
  selectedNodeId: string | null;
  insight:        InsightResponse | null;
  isLoading:      boolean;
  error:          Error | null;
  selectNode:     (id: string | null) => void;
}

export function useInsightPanelState(): InsightPanelState {
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null);
  const [insight,        setInsight]        = useState<InsightResponse | null>(null);
  const [isLoading,      setIsLoading]      = useState(false);
  const [error,          setError]          = useState<Error | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    // Cancel any in-flight request when the selected node changes.
    abortRef.current?.abort();

    if (!selectedNodeId) return;

    const controller = new AbortController();
    abortRef.current = controller;

    apiFetch<InsightResponse>(
      `/api/constellation/insight/${encodeURIComponent(selectedNodeId)}`,
      { signal: controller.signal },
    )
      .then((data) => {
        setInsight(data);
        setIsLoading(false);
      })
      .catch((err: unknown) => {
        if ((err as Error).name === "AbortError") return; // unmounted / node changed
        setInsight(null);
        setError(err instanceof Error ? err : new Error("Failed to load insight"));
        setIsLoading(false);
      });

    return () => controller.abort();
  }, [selectedNodeId]);

  const selectNode = useCallback((id: string | null) => {
    if (id === null) {
      abortRef.current?.abort();
      setInsight(null);
      setError(null);
      setIsLoading(false);
    } else {
      setIsLoading(true);
      setError(null);
    }

    setSelectedNodeId(id);
  }, []);

  return { selectedNodeId, insight, isLoading, error, selectNode };
}
