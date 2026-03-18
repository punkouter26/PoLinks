export interface PulseAnimationFrame {
  progress: number;
  rippleRadius: number;
  rippleOpacity: number;
}

export function getPulseAnimationFrame(
  lastPulseAt: number | null,
  now: number,
  viewport: { width: number; height: number },
  cycleMs = 30_000,
): PulseAnimationFrame {
  const elapsed = Math.max(0, now - (lastPulseAt ?? now));
  const progress = Math.min(elapsed / cycleMs, 1);
  const rippleRadius = Math.max(viewport.width, viewport.height) * (0.12 + progress * 0.55);

  return {
    progress,
    rippleRadius,
    rippleOpacity: Math.max(0, 0.35 - progress * 0.3),
  };
}
