import { expect, test } from '@playwright/test';

test.describe('Snapshot Export (US6)', () => {
  test('metadata endpoint returns canonical PNG export settings', async ({ request }) => {
    const response = await request.get('/api/snapshot/export-metadata?format=png&scale=2');
    expect(response.ok()).toBe(true);

    const body = await response.json();
    expect(typeof body.fileName).toBe('string');
    expect(body.fileName).toMatch(/^polinks-constellation-\d{8}-\d{6}\.png$/);
    expect(body.contentType).toBe('image/png');
    expect(body.format).toBe('png');
    expect(body.scale).toBe(2);
  });
});
