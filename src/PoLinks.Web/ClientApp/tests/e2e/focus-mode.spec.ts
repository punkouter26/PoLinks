// T069 (US4): E2E/API tests for focus-mode lifecycle (FR-024, FR-025)
import { test, expect } from '@playwright/test';

test.describe('Focus Mode (FR-024, FR-025)', () => {
  test('dashboard canvas is visible for focus interactions', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator("canvas[aria-label='Constellation canvas']")).toBeVisible({ timeout: 10_000 });
  });

  test('focus mode enter/status/exit contract works', async ({ request }) => {
    const enterResponse = await request.post('/api/constellation/focus-mode/enter', {
      data: { anchorId: 'robotics' },
    });
    expect(enterResponse.ok()).toBe(true);

    const enterBody = await enterResponse.json();
    expect(enterBody.anchorId).toBe('robotics');
    expect(enterBody.isActive).toBe(true);

    const statusResponse = await request.get('/api/constellation/focus-mode/status');
    expect(statusResponse.ok()).toBe(true);
    const statusBody = await statusResponse.json();
    expect(statusBody.isActive).toBe(true);

    const exitResponse = await request.post('/api/constellation/focus-mode/exit');
    expect(exitResponse.ok()).toBe(true);

    const exitStatusResponse = await request.get('/api/constellation/focus-mode/status');
    expect(exitStatusResponse.ok()).toBe(true);
    const exitStatusBody = await exitStatusResponse.json();
    expect(exitStatusBody.isActive).toBe(false);
  });
});
