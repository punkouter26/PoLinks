type JsonValue = null | boolean | number | string | JsonValue[] | { [key: string]: JsonValue };

import { isStandaloneClientMode, resolveRuntimeUrl, usesOfflineFallback } from './runtime';

const defaultFocusStatus = {
  isActive: false,
  anchorId: null,
  nodeCount: null,
  enteredAt: null,
};

function jsonResponse(data: JsonValue, status = 200): Response {
  return new Response(JSON.stringify(data), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function createMockResponse(url: URL, method: string): Response | null {
  const pathname = url.pathname;

  if (pathname === "/diagnostic/health" || pathname === "/health") {
    return jsonResponse({
      status: "healthy",
      totalDuration: "0 ms",
      details: {
        OfflineMock: {
          status: "healthy",
          description: "Client fallback mode is active",
          duration: 0,
        },
      },
      timestamp: new Date().toISOString(),
    });
  }

  if (pathname === "/diagnostic/config") {
    return jsonResponse({
      environment: "Offline",
      applicationName: "PoLinks.Web",
      applicationVersion: "1.0.0",
      maskedSettings: {
        "Offline:Mode": "true",
      },
    });
  }

  if (pathname === "/diagnostic/logs") {
    return jsonResponse({
      logs: [],
      mode: 'offline',
    });
  }

  if (pathname === "/diagnostic/analytics") {
    return jsonResponse({
      date: new Date().toISOString().slice(0, 10),
      totalPosts: 0,
      countsByAnchor: {},
    });
  }

  if (pathname === "/diagnostic/sentiment-status") {
    return jsonResponse({
      circuitOpen: false,
      usedToday: 0,
      cap: 3000,
      estimatedDailyCost: "$0.000",
      asOfUtc: new Date().toISOString(),
    });
  }

  if (pathname === "/api/constellation/focus-mode/status") {
    return jsonResponse(defaultFocusStatus);
  }

  if (pathname === "/api/constellation/focus-mode/enter" && method === "POST") {
    return jsonResponse({
      isActive: true,
      anchorId: "offline-anchor",
      nodeCount: 0,
      enteredAt: new Date().toISOString(),
    });
  }

  if (pathname === "/api/constellation/focus-mode/exit" && method === "POST") {
    return jsonResponse(defaultFocusStatus);
  }

  if (pathname.startsWith("/api/constellation/insight/")) {
    const nodeId = decodeURIComponent(pathname.split("/").pop() ?? "offline-node");
    return jsonResponse({
      nodeId,
      anchorId: nodeId,
      semanticRoots: [nodeId],
      posts: [],
    });
  }

  if (pathname === "/api/snapshot/export-metadata") {
    return jsonResponse({
      fileName: `snapshot-offline-${Date.now()}.png`,
      contentType: "image/png",
      format: "png",
      scale: 1,
      generatedAtUtc: new Date().toISOString(),
    });
  }

  return null;
}

// Capture originalFetch once at module load so it is never overwritten by re-calls.
const originalFetch = window.fetch.bind(window);

export function disableOfflineApiFallback(): void {
  window.fetch = originalFetch;
}

export function enableOfflineApiFallback(): void {
  window.fetch = async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    // Evaluate mode flags inside the replacement so they reflect live config on each call.
    const standaloneMode = isStandaloneClientMode();
    const allowFallback = usesOfflineFallback();

    const request = input instanceof Request
      ? input
      : new Request(
          resolveRuntimeUrl(input instanceof URL ? input.toString() : input.toString()),
          init,
        );
    const requestUrl = new URL(request.url, window.location.origin);

    if (standaloneMode) {
      const mockResponse = createMockResponse(requestUrl, request.method.toUpperCase());
      if (mockResponse) {
        return mockResponse;
      }
    }

    try {
      return await originalFetch(request);
    } catch {
      if (!allowFallback) {
        throw new Error('API request failed and offline fallback is disabled.');
      }

      const mockResponse = createMockResponse(requestUrl, request.method.toUpperCase());
      if (mockResponse) {
        return mockResponse;
      }

      throw new Error("Offline fallback has no mock for this endpoint.");
    }
  };
}

// Restore the real fetch on HMR hot-module disposal to prevent stale patches in dev.
if (import.meta.hot) {
  import.meta.hot.dispose(disableOfflineApiFallback);
}
