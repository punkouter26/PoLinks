import { useEffect, useState } from "react";

const handleCache = new Map<string, string>();
const pendingRequests = new Map<string, Promise<string>>();

function formatDidFallback(did: string): string {
  return did.length > 20 ? `…${did.slice(-18)}` : did;
}

async function fetchHandle(did: string): Promise<string> {
  const response = await fetch(`https://public.api.bsky.app/xrpc/app.bsky.actor.getProfile?actor=${encodeURIComponent(did)}`);
  if (!response.ok) {
    return formatDidFallback(did);
  }

  const payload = await response.json() as { handle?: string };
  return payload.handle ? `@${payload.handle}` : formatDidFallback(did);
}

export function useBlueskyHandle(did: string): string {
  const [displayHandle, setDisplayHandle] = useState<string>(() => handleCache.get(did) ?? formatDidFallback(did));

  useEffect(() => {
    if (!did.startsWith("did:")) {
      setDisplayHandle(did);
      return;
    }

    const cachedHandle = handleCache.get(did);
    if (cachedHandle) {
      setDisplayHandle(cachedHandle);
      return;
    }

    let active = true;
    const request = pendingRequests.get(did) ?? fetchHandle(did)
      .then((resolvedHandle) => {
        handleCache.set(did, resolvedHandle);
        return resolvedHandle;
      })
      .catch(() => formatDidFallback(did))
      .finally(() => {
        pendingRequests.delete(did);
      });

    pendingRequests.set(did, request);
    request.then((resolvedHandle) => {
      if (active) {
        setDisplayHandle(resolvedHandle);
      }
    });

    return () => {
      active = false;
    };
  }, [did]);

  return displayHandle;
}