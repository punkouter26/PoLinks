// T062 (US3): E2E/API tests for ghost constellation data contract (FR-010)
import { test, expect } from '@playwright/test';

test.describe('Ghost Constellation Overlay (FR-010)', () => {
  test('dashboard renders the primary constellation canvas', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator("canvas[aria-label='Constellation canvas']")).toBeVisible({ timeout: 10_000 });
  });

  test('ghost snapshot endpoint returns JSON payload', async ({ request }) => {
    const now = Date.now();
    const oneHourAgo = now - 60 * 60 * 1000;

    const response = await request.get(
      `/api/constellation/ghost-snapshots?startTime=${oneHourAgo}&endTime=${now}`
    );

    expect(response.ok()).toBe(true);
    const body = await response.json();
    expect(body).toBeTruthy();
    expect(typeof body).toBe('object');
  });

  test('ghost snapshot endpoint handles narrow windows', async ({ request }) => {
    const now = Date.now();
    const oneMinuteAgo = now - 60 * 1000;

    const response = await request.get(
      `/api/constellation/ghost-snapshots?startTime=${oneMinuteAgo}&endTime=${now}`
    );

    expect(response.ok()).toBe(true);
    const body = await response.json();
    expect(body).toBeTruthy();
    expect(typeof body).toBe('object');
  });
});
