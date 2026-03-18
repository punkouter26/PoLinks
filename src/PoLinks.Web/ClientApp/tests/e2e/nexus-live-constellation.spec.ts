// T025: E2E test — cold start animation + first pulse arrival (FR-001, FR-009).
import { test, expect } from "@playwright/test";

test.describe("Live Constellation View – cold start flow", () => {
  const jsErrors: string[] = [];
  test.beforeEach(async ({ page }) => {
    jsErrors.length = 0;
    page.on("pageerror", (err) => jsErrors.push(err.message));
  });
  test.afterEach(async () => {
    expect(jsErrors, `JavaScript errors detected:\n${jsErrors.join("\n")}`).toHaveLength(0);
  });

  test("renders the constellation canvas on page load", async ({ page }) => {
    await page.goto("/");
    // The D3 canvas element must exist (aria-label set by ConstellationCanvas)
    const canvas = page.locator("canvas[aria-label='Constellation canvas']");
    await expect(canvas).toBeVisible({ timeout: 10_000 });
  });

  test("shows simulation banner when no live data is available", async ({ page }) => {
    await page.goto("/");
    // Simulation banner has role=status with text matching "Simulation Mode"
    const banner = page.getByRole("status").filter({ hasText: /simulation mode/i });
    await expect(banner).toBeVisible({ timeout: 10_000 });
  });

  test("countdown progress bar is visible and starts ticking", async ({ page }) => {
    await page.goto("/");
    const bar = page.getByRole("progressbar", { name: /next pulse/i });
    await expect(bar).toBeVisible({ timeout: 10_000 });
    // Read value at t=0 and t=2s; must decrease
    const v1 = await bar.getAttribute("aria-valuenow");
    await page.waitForTimeout(2_000);
    const v2 = await bar.getAttribute("aria-valuenow");
    expect(Number(v2)).toBeLessThan(Number(v1));
  });

  test("pulse updates arrive within 35 seconds", async ({ page }) => {
    await page.goto("/");
    // When a pulse arrives, the constellation canvas gets a data-pulse-count attribute
    const canvas = page.locator("canvas[aria-label='Constellation canvas']");
    await expect(canvas).toHaveAttribute("data-pulse-count", /[1-9]/, { timeout: 35_000 });
  });
});
