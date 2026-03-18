import type { ReactNode } from 'react';
import { useFocusModeKeyBindings } from './useFocusMode';
import styles from './FocusModeControls.module.css';

interface FocusModeControlsProps {
  anchorName: string;
  isActive: boolean;
  onExit: () => void;
  children?: ReactNode;
}

export function FocusModeControls({ anchorName, isActive, onExit, children }: FocusModeControlsProps) {
  useFocusModeKeyBindings(isActive, onExit);

  return (
    <>
      {isActive ? (
        <div
          data-testid="focus-mode-active"
          className={styles.banner}
        >
          <span>Focused on <strong className={styles.anchorName}>{anchorName}</strong></span>
          <button
            data-testid="focus-exit-button"
            type="button"
            onClick={onExit}
            className={styles.exitButton}
          >
            Exit Focus
            <span className={styles.kbdHint} aria-hidden="true">ESC</span>
          </button>
        </div>
      ) : null}
      {children}
    </>
  );
}

interface FocusModeUIProps {
  isActive: boolean;
  anchorName: string | null;
  error?: string | null;
  isLoading?: boolean;
  onExit: () => void;
  children: ReactNode;
}

export function FocusModeUI({
  isActive,
  anchorName,
  error,
  isLoading,
  onExit,
  children,
}: FocusModeUIProps) {
  return (
    <div>
      {error ? <div role="alert">{error}</div> : null}
      <FocusModeControls
        anchorName={anchorName ?? 'Unknown'}
        isActive={isActive}
        onExit={onExit}
      >
        {children}
      </FocusModeControls>
      {isLoading ? <div>Updating focus mode...</div> : null}
    </div>
  );
}
