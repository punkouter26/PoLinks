// UX-7: Maps raw HTTP/API error messages to user-friendly copy.
// Prevents internal API paths and status codes from leaking into the UI.

export function formatApiError(message: string): string {
  if (/502|bad gateway/i.test(message)) {
    return 'Backend offline — running in simulation mode';
  }
  if (/503|service unavailable/i.test(message)) {
    return 'Service temporarily unavailable';
  }
  if (/404|not found/i.test(message)) {
    return 'Node data not found';
  }
  if (/401|403|unauthorized|forbidden/i.test(message)) {
    return 'Access denied';
  }
  if (/5\d\d/.test(message)) {
    return 'Service error — check Diagnostic page for details';
  }
  return message;
}
