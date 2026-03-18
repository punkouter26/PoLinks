// T047/T059: Shared fetch utility with typed JSON response and consistent error contract.
// Throws ApiError on non-2xx responses so callers can distinguish API failures
// from network errors without inspecting raw Response objects.

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

export async function apiFetch<T = unknown>(
  url: string,
  options?: RequestInit,
): Promise<T> {
  const response = await fetch(url, options);
  if (!response.ok) {
    throw new ApiError(response.status, `HTTP ${response.status} — ${url}`);
  }
  return response.json() as Promise<T>;
}
