export type ClientRuntimeMode = 'auto' | 'standalone' | 'hosted';

function normalizeMode(value: string | undefined): ClientRuntimeMode {
  switch ((value ?? '').trim().toLowerCase()) {
    case 'standalone':
      return 'standalone';
    case 'hosted':
      return 'hosted';
    default:
      return 'auto';
  }
}

function normalizeApiBaseUrl(value: string | undefined): string {
  const trimmed = (value ?? '').trim();
  return trimmed.endsWith('/') ? trimmed.slice(0, -1) : trimmed;
}

export const clientRuntime = {
  mode: normalizeMode(import.meta.env.VITE_RUNTIME_MODE),
  apiBaseUrl: normalizeApiBaseUrl(import.meta.env.VITE_API_BASE_URL),
};

export function resolveRuntimeUrl(path: string): string {
  if (!clientRuntime.apiBaseUrl || /^https?:\/\//i.test(path)) {
    return path;
  }

  return new URL(path, `${clientRuntime.apiBaseUrl}/`).toString();
}

export function isStandaloneClientMode(): boolean {
  return clientRuntime.mode === 'standalone';
}

export function usesOfflineFallback(): boolean {
  return clientRuntime.mode !== 'hosted';
}