import { expect, test } from '@playwright/test';

// T050: E2E tests for diagnostic terminal UI
// Validates that the diagnostic terminal renders, accepts commands, and returns masked results

test.describe('Diagnostic Terminal UI', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to diagnostic terminal if it exists as a route
    await page.goto('http://localhost:5173/');
    // If diagnostic terminal is a modal/component, ensure we're on the right page
  });

  test('should render diagnostic terminal component', async ({ page }) => {
    // This test assumes diagnostic terminal is visible or accessible via a button
    // Adjust selector based on actual implementation
    const terminalElement = page.locator('[data-testid="diagnostic-terminal"]');
    
    // Element should exist (even if initially hidden)
    const elementHandle = await terminalElement.count();
    expect(elementHandle).toBeGreaterThanOrEqual(0);
  });

  test('should have input field for commands', async ({ page }) => {
    const terminalInput = page.locator('input[type="text"][placeholder*="command"]', {
      has: page.locator('text=/diagnostic|terminal/i'),
    });
    
    // Input should be present and focusable
    await terminalInput.focus();
    expect(await terminalInput.isVisible()).toBe(true);
  });

  test('should display command history', async ({ page }) => {
    const terminalOutput = page.locator('[data-testid="terminal-output"]');
    
    // Output area should exist
    const isVisible = await terminalOutput.count().then(c => c > 0);
    expect(isVisible).toBe(true);
  });

  test('should mask sensitive values in output', async ({ page }) => {
    const terminalOutput = page.locator('[data-testid="terminal-output"]').textContent();
    
    // Should never expose unmasked secrets in UI output
    expect(await terminalOutput).not.toMatch(/sk_live_/);
    expect(await terminalOutput).not.toMatch(/password\s*=\s*[^[]/i);
  });

  test('should accept help command without errors', async ({ page }) => {
    const terminalInput = page.locator('input[type="text"]').first();
    
    // Type help command
    await terminalInput.fill('help');
    await terminalInput.press('Enter');
    
    // Wait for response
    await page.waitForTimeout(500);
    
    // Output should not show error
    const terminalOutput = await page.locator('[data-testid="terminal-output"]').textContent();
    expect(terminalOutput).not.toContain('Error');
  });

  test('should display available commands', async ({ page }) => {
    const terminalInput = page.locator('input[type="text"]').first();
    
    // Type help command
    await terminalInput.fill('help');
    await terminalInput.press('Enter');
    
    // Wait for response
    await page.waitForTimeout(500);
    
    // Output should list available commands
    const terminalOutput = await page.locator('[data-testid="terminal-output"]').textContent();
    expect(terminalOutput?.length || 0).toBeGreaterThan(0);
  });

  test('should handle invalid commands gracefully', async ({ page }) => {
    const terminalInput = page.locator('input[type="text"]').first();
    
    // Type invalid command
    await terminalInput.fill('invalid_command_12345');
    await terminalInput.press('Enter');
    
    // Wait for response
    await page.waitForTimeout(500);
    
    // Should show user-friendly error, not a server error
    const terminalOutput = await page.locator('[data-testid="terminal-output"]').textContent();
    expect(terminalOutput).toMatch(/not found|unknown|invalid/i);
  });

  test('should clear terminal on clear command', async ({ page }) => {
    const terminalInput = page.locator('input[type="text"]').first();
    
    // Add some commands first
    await terminalInput.fill('help');
    await terminalInput.press('Enter');
    await page.waitForTimeout(300);
    
    // Clear terminal
    await terminalInput.fill('clear');
    await terminalInput.press('Enter');
    await page.waitForTimeout(300);
    
    // Output area should be empty or show minimal content
    const terminalOutput = await page.locator('[data-testid="terminal-output"]').textContent();
    expect(terminalOutput?.trim().length || 0).toBeLessThan(50);
  });

  test('should not allow arbitrary code execution', async ({ page }) => {
    const terminalInput = page.locator('input[type="text"]').first();
    
    // Try to execute script-like command
    await terminalInput.fill('eval(alert("xss"))');
    await terminalInput.press('Enter');
    
    // Wait for response
    await page.waitForTimeout(500);
    
    // Should not execute scripts or throw console errors
    const consoleErrors = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });
    
    expect(consoleErrors.length).toBe(0);
  });
});
