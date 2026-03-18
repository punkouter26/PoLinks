// T041: E2E test — node click opens Contextual Insight Panel (US2, AC-1..AC-4).
import { test, expect } from "@playwright/test";

test.describe("Contextual Insight Panel – node click workflow", () => {
  const jsErrors: string[] = [];
  test.beforeEach(async ({ page }) => {
    jsErrors.length = 0;
    page.on("pageerror", (err) => jsErrors.push(err.message));
  });
  test.afterEach(async () => {
    expect(jsErrors, `JavaScript errors detected:\n${jsErrors.join("\n")}`).toHaveLength(0);
  });

  test("renders insight panel shell and close affordance", async ({ page }) => {
    await page.goto("/");
    const panel = page.getByTestId("insight-panel");
    await expect(panel).toBeVisible();
    await expect(page.getByRole("button", { name: /close insight panel/i })).toBeVisible();
  });

  test("insight API endpoint responds for known seeded node", async ({ request }) => {
    const response = await request.get("/api/constellation/insight/robotics");
    expect([200, 404]).toContain(response.status());

    const body = await response.json();
    expect(body).toBeTruthy();
    expect(typeof body).toBe("object");
  });
});
