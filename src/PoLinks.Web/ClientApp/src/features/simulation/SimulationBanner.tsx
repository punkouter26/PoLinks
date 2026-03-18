// T038: Renders a full-width alert bar under the nav when using simulation/offline data
import { usePulseState } from "../../context/PulseStateContext";
import styles from "./SimulationBanner.module.css";

export function SimulationBanner() {
  const { isConnected, isConnecting, isSimulated } = usePulseState();

  if (isConnecting) return null; // Still establishing initial connection
  if (isConnected && !isSimulated) return null; // Live mode

  return (
    <div
      role="alert"
      aria-live="polite"
      data-testid="sim-banner"
      className={styles.banner}
    >
      <span aria-hidden="true" className={styles.dot} />
      <span className={styles.label}>SIMULATION MODE</span>
      <span className={styles.sub}>Live Bluesky connection unavailable &mdash; displaying generated data</span>
    </div>
  );
}
