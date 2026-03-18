// T044: Right-side insight panel shell with slide-in transition (US2, AC-1).
// Slides in from the right edge within 300ms when a node is selected.
import type { InsightResponse, NodeType } from "../../types/nexus";
import { SemanticRootsBreadcrumbs } from "./SemanticRootsBreadcrumbs";
import { ImpactFeedList }           from "./ImpactFeedList";
import { formatApiError }           from "../../utils/errorMessages";
import styles from "./InsightPanel.module.css";

interface InsightPanelProps {
  insight:    InsightResponse | null;
  isLoading:  boolean;
  isOpen:     boolean;
  error?:     Error | null;
  onClose:    () => void;
  nodeType?:  NodeType | null;
}

export function InsightPanel({ insight, isLoading, isOpen, error, onClose, nodeType }: InsightPanelProps) {
  const headerClass =
    nodeType === 'Anchor' ? `${styles.header} ${styles.headerAnchor}`
    : nodeType === 'Topic'  ? `${styles.header} ${styles.headerTopic}`
    : styles.header;

  return (
    <aside
      aria-label="Node insight panel"
      data-testid="insight-panel"
      data-open={isOpen}
      className={styles.panel}
    >
      {/* Header with node-type accent border */}
      <div className={headerClass}>
        {/* Graph / network icon */}
        <svg
          className={styles.headerIcon}
          viewBox="0 0 20 20"
          fill="none"
          aria-hidden="true"
        >
          <circle cx="4" cy="10" r="2.5" stroke="currentColor" strokeWidth="1.5" />
          <circle cx="16" cy="4" r="2.5" stroke="currentColor" strokeWidth="1.5" />
          <circle cx="16" cy="16" r="2.5" stroke="currentColor" strokeWidth="1.5" />
          <line x1="6.5" y1="9" x2="13.5" y2="5" stroke="currentColor" strokeWidth="1.2" />
          <line x1="6.5" y1="11" x2="13.5" y2="15" stroke="currentColor" strokeWidth="1.2" />
        </svg>
        <span className={styles.title}>Contextual Insight</span>
        <button
          onClick={onClose}
          aria-label="Close insight panel"
          className={styles.closeButton}
        >
          ×
        </button>
      </div>

      {/* Breadcrumbs */}
      {insight && (
        <div className={styles.breadcrumbs}>
          <SemanticRootsBreadcrumbs roots={insight.semanticRoots} />
        </div>
      )}

      {/* Content area */}
      <div className={styles.content} data-testid="insight-panel-content">
        {isLoading && (
          <p className={styles.emptyText}>Loading…</p>
        )}

        {!isLoading && insight && <ImpactFeedList posts={insight.posts} />}

        {!isLoading && !insight && error && isOpen && (
          <p className={styles.emptyText} role="alert">{formatApiError(error.message)}</p>
        )}

        {!isLoading && !insight && !error && isOpen && (
          <p className={styles.emptyText}>No data available for this node.</p>
        )}
      </div>
    </aside>
  );
}

