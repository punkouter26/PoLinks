// T090: Simple countdown ring matching D3 timer interpolation (FR-001).
import { useEffect, useState } from "react";
import { usePulseState } from "../../context/PulseStateContext";
import styles from "./CountdownProgressBar.module.css";

export function CountdownProgressBar() {
  const { lastPulseAt } = usePulseState();
  const [progress, setProgress] = useState(0);
  const remaining = Math.max(0, 100 - progress);

  useEffect(() => {
    const CYCLE_MS = 30000;

    const tick = () => {
      const elapsed = Date.now() - (lastPulseAt ?? Date.now());
      setProgress(Math.min(100, (elapsed / CYCLE_MS) * 100));
    };

    tick();
    const id = setInterval(tick, 1000);
    return () => clearInterval(id);
  }, [lastPulseAt]);

  return (
    <div className={styles.widget}>
      <div className={styles.header}>
        <span>NEXT PULSE</span>
        <span data-testid="pulse-countdown-label">{Math.ceil((30000 - (progress / 100) * 30000) / 1000)}s</span>
      </div>
      <div
        role="progressbar"
        aria-label="Next Pulse"
        aria-valuenow={remaining}
        aria-valuemin={0}
        aria-valuemax={100}
        className={styles.track}
      >
        <div
          className={styles.fill}
          style={{ ['--fill-width' as string]: `${remaining}%` } as React.CSSProperties}
        />
      </div>
    </div>
  );
}
