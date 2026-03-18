import { useCallback, useEffect, useState } from 'react';
import { apiFetch } from '../../utils/apiFetch';

interface FocusModeStatusDto {
  isActive: boolean;
  anchorId: string | null;
  nodeCount: number | null;
  enteredAt: string | null;
}

export function useFocusMode() {
  const [status, setStatus] = useState<FocusModeStatusDto>({
    isActive: false,
    anchorId: null,
    nodeCount: null,
    enteredAt: null,
  });
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    const value = await apiFetch<FocusModeStatusDto>('/api/constellation/focus-mode/status');
    setStatus(value);
  }, []);

  useEffect(() => {
    setIsLoading(true);
    void refresh()
      .catch((value) => {
        setError(value instanceof Error ? value.message : 'Failed to load focus mode status');
      })
      .finally(() => setIsLoading(false));
  }, [refresh]);

  const enterFocusMode = useCallback(async (anchorId: string) => {
    setIsLoading(true);
    setError(null);

    try {
      await apiFetch('/api/constellation/focus-mode/enter', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ anchorId }),
      });

      await refresh();
    } catch (value) {
      setError(value instanceof Error ? value.message : 'Failed to enter focus mode');
      throw value;
    } finally {
      setIsLoading(false);
    }
  }, [refresh]);

  const exitFocusMode = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      await apiFetch('/api/constellation/focus-mode/exit', {
        method: 'POST',
      });

      await refresh();
    } catch (value) {
      setError(value instanceof Error ? value.message : 'Failed to exit focus mode');
      throw value;
    } finally {
      setIsLoading(false);
    }
  }, [refresh]);

  return {
    focusedAnchorId: status.anchorId,
    focusedNodeCount: status.nodeCount,
    enteredAt: status.enteredAt,
    isFocused: status.isActive,
    isLoading,
    error,
    enterFocusMode,
    exitFocusMode,
    refresh,
  };
}

export function useFocusModeKeyBindings(isFocused: boolean, onExit: () => void) {
  useEffect(() => {
    if (!isFocused) {
      return undefined;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        onExit();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isFocused, onExit]);
}
