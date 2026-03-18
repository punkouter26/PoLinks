import { expect, test } from '@playwright/test';

// T051: E2E/API tests for diagnostic health and masked config endpoints

test.describe('Diagnostic API contracts', () => {
  test('health endpoint returns expected shape', async ({ request }) => {
    const response = await request.get('/diagnostic/health');
    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(typeof body.status).toBe('string');
    expect(body.details).toBeTruthy();
    expect(typeof body.details).toBe('object');
    expect(typeof body.timestamp).toBe('string');
  });

  test('config endpoint returns masked settings payload', async ({ request }) => {
    const response = await request.get('/diagnostic/config');
    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(body).toBeTruthy();
    expect(typeof body).toBe('object');
  });

  test('uptime endpoint returns uptime metrics', async ({ request }) => {
    const response = await request.get('/diagnostic/uptime');
    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(body).toBeTruthy();
    expect(typeof body).toBe('object');
  });
});
