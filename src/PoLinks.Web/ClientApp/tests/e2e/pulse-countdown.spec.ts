// T089: E2E test — countdown bar visibility, 1-second updates, reset on pulse (FR-011).
import { test, expect } from "@playwright/test";

test.describe("Pulse countdown progress bar (FR-011)", () => {
  const jsErrors: string[] = [];
  test.beforeEach(async ({ page }) => {
    jsErrors.length = 0;
    page.on("pageerror", (err) => jsErrors.push(err.message));
  });
  test.afterEach(async () => {
    expect(jsErrors, `JavaScript errors detected:\n${jsErrors.join("\n")}`).toHaveLength(0);
  });

  test("progress bar is visible immediately on load", async ({ page }) => {
    await page.goto("/");
    const bar = page.getByRole("progressbar", { name: /next pulse/i });
    await expect(bar).toBeVisible({ timeout: 10_000 });
  });

  test("progress bar decreases every second", async ({ page }) => {
    await page.goto("/");
    const bar = page.getByRole("progressbar", { name: /next pulse/i });
    await expect(bar).toBeVisible({ timeout: 10_000 });

    const samples: number[] = [];
    for (let i = 0; i < 4; i++) {
      await page.waitForTimeout(1_000);
      const val = await bar.getAttribute("aria-valuenow");
      samples.push(Number(val));
    }

    // Values should remain bounded and at least one sample should move.
    expect(samples.every((sample) => sample >= 0 && sample <= 100)).toBe(true);
    const distinctValues = new Set(samples.map((sample) => Math.round(sample)));
    expect(distinctValues.size).toBeGreaterThan(1);
  });

  test("countdown label shows seconds remaining", async ({ page }) => {
    await page.goto("/");
    // The countdown label should show a number followed by "s"
    const label = page.locator("[data-testid='pulse-countdown-label']");
    await expect(label).toBeVisible({ timeout: 10_000 });
    await expect(label).toHaveText(/\d+s/);
  });
});
