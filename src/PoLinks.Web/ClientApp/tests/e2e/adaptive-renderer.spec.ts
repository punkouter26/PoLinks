// T092: E2E test — adaptive renderer fallback on a constrained device profile (FR-032).
import { test, expect } from "@playwright/test";

test.describe("Adaptive renderer — capability detection (FR-032)", () => {
  const jsErrors: string[] = [];
  test.beforeEach(async ({ page }) => {
    jsErrors.length = 0;
    page.on("pageerror", (err) => jsErrors.push(err.message));
  });
  test.afterEach(async () => {
    expect(jsErrors, `JavaScript errors detected:\n${jsErrors.join("\n")}`).toHaveLength(0);
  });

  test("renders constellation on a desktop viewport (WebGL2 expected)", async ({ page }) => {
    await page.goto("/");
    const canvas = page.locator("canvas[aria-label='Constellation canvas']");
    await expect(canvas).toBeVisible({ timeout: 10_000 });
    // On normal desktop, renderer mode should be set to webgl2
    await expect(canvas).toHaveAttribute("data-renderer", "webgl2");
  });

  test("falls back to 2D canvas on a device that cannot provide WebGL2", async ({
    browser,
  }) => {
    // Use a plain context and force WebGL2 failure so this runs consistently across browsers.
    const ctx = await browser.newContext();
    const page = await ctx.newPage();

    // Inject a script to remove WebGL2 support before page load
    await page.addInitScript(() => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (HTMLCanvasElement.prototype as any).getContext = (function (original: any) {
        return function (type: string, ...rest: unknown[]) {
          if (type === "webgl2") return null;
          return original.call(this, type, ...rest);
        };
      })(HTMLCanvasElement.prototype.getContext);
    });

    await page.goto("/");
    const canvas = page.locator("canvas[aria-label='Constellation canvas']");
    await expect(canvas).toBeVisible({ timeout: 10_000 });
    await expect(canvas).toHaveAttribute("data-renderer", "canvas2d");
    await ctx.close();
  });
});
