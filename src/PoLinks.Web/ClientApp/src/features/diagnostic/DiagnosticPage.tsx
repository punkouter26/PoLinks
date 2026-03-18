// T057: Main diagnostic page component
// Orchestrates health status, masked config panel, and live log terminal
import { useState, useEffect } from 'react';
import styles from './DiagnosticPage.module.css';
import { MaskedConfigPanel } from './MaskedConfigPanel';
import { LiveErrorTerminalDrawer } from './LiveErrorTerminalDrawer';

interface HealthCheckComponent {
  name: string;
  status: 'Healthy' | 'Degraded' | 'Unhealthy';
  description: string;
  duration: string;
}

interface DiagnosticHealth {
  status: 'Healthy' | 'Degraded' | 'Unhealthy';
  totalDuration: string;
  components: HealthCheckComponent[];
  timestamp: string;
}

interface BackendHealthResponse {
  status: string;
  totalDuration?: string;
  details?: Record<string, {
    status: string;
    description?: string;
    duration?: number;
  }>;
  components?: HealthCheckComponent[];
  timestamp: string;
}

function normalizeStatus(status: string | undefined): 'Healthy' | 'Degraded' | 'Unhealthy' {
  const value = (status ?? '').toLowerCase();
  if (value === 'healthy') return 'Healthy';
  if (value === 'degraded') return 'Degraded';
  return 'Unhealthy';
}

function mapHealthResponse(input: BackendHealthResponse): DiagnosticHealth {
  if (Array.isArray(input.components)) {
    return {
      status: normalizeStatus(input.status),
      totalDuration: input.totalDuration ?? 'n/a',
      components: input.components,
      timestamp: input.timestamp,
    };
  }

  const details = input.details ?? {};
  const components: HealthCheckComponent[] = Object.entries(details).map(([name, item]) => ({
    name,
    status: normalizeStatus(item.status),
    description: item.description ?? 'No description provided',
    duration: `${item.duration ?? 0} ms`,
  }));

  return {
    status: normalizeStatus(input.status),
    totalDuration: input.totalDuration ?? 'n/a',
    components,
    timestamp: input.timestamp,
  };
}

interface RecentLogEntry { id: string; level: string; message: string; timestamp: string; }

interface AnalyticsData {
  date: string;
  totalPosts: number;
  countsByAnchor: Record<string, number>;
}

function AnalyticsSummarySection() {
  const [data, setData] = useState<AnalyticsData | null>(null);
  const [err, setErr] = useState(false);

  useEffect(() => {
    const ctrl = new AbortController();
    fetch('/diagnostic/analytics', { signal: ctrl.signal })
      .then((r) => r.ok ? (r.json() as Promise<AnalyticsData>) : Promise.reject())
      .then(setData)
      .catch(() => setErr(true));
    return () => ctrl.abort();
  }, []);

  if (err || !data) return null;

  return (
    <section className={styles.section} data-testid="analytics-section">
      <div className={styles.sectionHeader}>
        <h2>Today&apos;s Ingestion ({data.date})</h2>
        <span className={styles.totalBadge}>{data.totalPosts.toLocaleString()} posts</span>
      </div>
      <div className={styles.analyticsGrid}>
        {Object.entries(data.countsByAnchor).sort(([, a], [, b]) => b - a).map(([anchor, count]) => (
          <div key={anchor} className={styles.analyticsCard}>
            <span className={styles.analyticsAnchor}>{anchor}</span>
            <span className={styles.analyticsCount}>{count.toLocaleString()}</span>
          </div>
        ))}
      </div>
    </section>
  );
}

export const DiagnosticPage: React.FC = () => {
  const [health, setHealth] = useState<DiagnosticHealth | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showTerminal, setShowTerminal] = useState(false);
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [refreshInterval, setRefreshInterval] = useState(5000);
  const [recentLogs, setRecentLogs] = useState<RecentLogEntry[]>([]);

  const fetchHealth = async (signal?: AbortSignal) => {
    try {
      setError(null);
      const response = await fetch('/diagnostic/health', { signal });

      if (!response.ok) {
        if (response.status === 503) {
          // Service unhealthy is still valid response
          const data = (await response.json()) as BackendHealthResponse;
          setHealth(mapHealthResponse(data));
        } else {
          throw new Error(`HTTP Error: ${response.status}`);
        }
      } else {
        const data = (await response.json()) as BackendHealthResponse;
        setHealth(mapHealthResponse(data));
      }
    } catch (err) {
      if ((err as Error).name === 'AbortError') return;
      const message = err instanceof Error ? err.message : 'Unknown error';
      setError(`Failed to load health status: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const abortCtrl = new AbortController();

    // Initial fetch
    void fetchHealth(abortCtrl.signal);

    // Set up auto-refresh interval
    let interval: ReturnType<typeof setInterval> | undefined;
    if (autoRefresh) {
      interval = setInterval(() => { void fetchHealth(); }, refreshInterval);
    }

    return () => {
      abortCtrl.abort();
      if (interval) clearInterval(interval);
    };
  }, [autoRefresh, refreshInterval]);

  // UX-8: Fetch recent log entries once on mount for inline preview
  useEffect(() => {
    const ctrl = new AbortController();
    fetch('/diagnostic/logs', { signal: ctrl.signal })
      .then((r) => (r.ok ? (r.json() as Promise<{ logs: RecentLogEntry[] }>) : Promise.reject()))
      .then((d) => setRecentLogs(d.logs ?? []))
      .catch(() => undefined);
    return () => ctrl.abort();
  }, []);

  const getStatusIcon = (status: string): string => {
    switch (status) {
      case 'Healthy':
        return '✓';
      case 'Degraded':
        return '⚠';
      case 'Unhealthy':
        return '✕';
      default:
        return '?';
    }
  };

  return (
    <div className={styles.page} data-testid="diagnostic-page">
      <div className={styles.pageHeader}>
        <h1>Diagnostic Terminal</h1>
        <p className={styles.subtitle}>Monitor application health and configuration in real-time</p>
      </div>

      {/* Health Status Section */}
      <section className={styles.section} data-testid="health-section">
        <div className={styles.sectionHeader}>
          <h2>System Health</h2>
          <div className={styles.controls}>
            <button
              onClick={() => { void fetchHealth(); }}
              className={styles.iconButton}
              title="Refresh health status"
              disabled={loading}
            >
              ↻
            </button>
            <label className={styles.checkboxLabel}>
              <input
                type="checkbox"
                checked={autoRefresh}
                onChange={(e) => setAutoRefresh(e.target.checked)}
              />
              Auto-refresh
            </label>
            {autoRefresh && (
              <select
                aria-label="Auto-refresh interval"
                value={refreshInterval}
                onChange={(e) => setRefreshInterval(parseInt(e.target.value))}
                className={styles.intervalSelect}
              >
                <option value={1000}>Every 1s</option>
                <option value={5000}>Every 5s</option>
                <option value={10000}>Every 10s</option>
                <option value={30000}>Every 30s</option>
              </select>
            )}
          </div>
        </div>

        {loading && !health ? (
          <div className={styles.loadingState}>
            <div className={styles.spinner} />
            <p>Loading health status...</p>
          </div>
        ) : error && !health ? (
          <div className={styles.errorState}>
            <p className={styles.errorMessage}>⚠ {error}</p>
            <button onClick={() => { void fetchHealth(); }} className={styles.retryButton}>
              Retry
            </button>
          </div>
        ) : health ? (
          <>
            {/* Overall Status Card */}
            <div
              className={`${styles.statusCard} ${styles.overall}`}
              data-status={health.status}
              data-testid="health-card"
            >
              <div className={styles.statusContent}>
                <div className={styles.statusBadge}>
                  <span className={styles.statusIcon}>
                    {getStatusIcon(health.status)}
                  </span>
                  <span>{health.status}</span>
                </div>
                <div className={styles.statusMeta}>
                  <span className={styles.duration}>Duration: {health.totalDuration}</span>
                  <span>{new Date(health.timestamp).toLocaleTimeString()}</span>
                </div>
              </div>
            </div>

            {/* Component Health Grid */}
            <div className={styles.componentsGrid} data-testid="components-grid">
              {health.components.map((component) => (
                <div
                  key={component.name}
                  className={`${styles.statusCard} ${styles.componentCard}`}
                  data-status={component.status}
                  data-testid="component-card"
                >
                  <div className={styles.componentHeader}>
                    <h3>{component.name}</h3>
                    <span className={`${styles.statusBadge} ${styles.compact}`}>
                      {getStatusIcon(component.status)} {component.status}
                    </span>
                  </div>
                  <p className={styles.componentDescription}>{component.description}</p>
                  <div className={styles.componentFooter}>
                    <span className={styles.duration}>{component.duration}</span>
                  </div>
                </div>
              ))}
            </div>
          </>
        ) : null}
      </section>

      {/* Configuration Section */}
      <section className={styles.section} data-testid="config-section">
        <div className={styles.sectionHeader}>
          <h2>Masked Configuration</h2>
        </div>
        <MaskedConfigPanel refreshInterval={refreshInterval} />
      </section>

      {/* Terminal Controls */}
      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <h2>Logs &amp; Diagnostics</h2>
        </div>
        {recentLogs.length > 0 && (
          <div className={styles.recentLogsPreview}>
            {recentLogs.slice(-3).map((log) => (
              <div key={log.id} className={styles.recentLogEntry}>
                <span className={styles.recentLogLevel} data-level={log.level}>
                  [{log.level}]
                </span>
                <span className={styles.recentLogMessage}>{log.message}</span>
                <span className={styles.recentLogTime}>
                  {new Date(log.timestamp).toLocaleTimeString()}
                </span>
              </div>
            ))}
          </div>
        )}
        <div className={styles.terminalControls}>
          <button
            onClick={() => setShowTerminal(true)}
            className={styles.primaryButton}
            data-testid="open-terminal-button"
          >
            📋 Open Terminal
          </button>
          <p className={styles.helpText}>
            {recentLogs.length > 0
              ? `${recentLogs.length} log entr${recentLogs.length === 1 ? 'y' : 'ies'} available — Open Terminal for the full console`
              : 'Click “Open Terminal” to view real-time diagnostic logs and filter by severity.'
            }
          </p>
        </div>
      </section>
      {/* Analytics Section */}
      <AnalyticsSummarySection />
      {/* Help Section */}
      <section className={styles.helpSection}>
        <details>
          <summary>Help &amp; Keyboard Shortcuts</summary>
          <div className={styles.helpContent}>
            <h3>Keyboard Shortcuts</h3>
            <table className={styles.shortcutsTable}>
              <tbody>
                <tr>
                  <td className={styles.key}>Shift + D</td>
                  <td>Toggle diagnostic terminal</td>
                </tr>
                <tr>
                  <td className={styles.key}>Shift + R</td>
                  <td>Refresh health status</td>
                </tr>
                <tr>
                  <td className={styles.key}>Shift + C</td>
                  <td>Clear logs</td>
                </tr>
              </tbody>
            </table>

            <h3>Status Indicators</h3>
            <ul>
              <li><span className={styles.legendHealthy}>✓ Healthy</span> — All systems operational</li>
              <li><span className={styles.legendDegraded}>⚠ Degraded</span> — Some functionality limited</li>
              <li><span className={styles.legendUnhealthy}>✕ Unhealthy</span> — Critical issues detected</li>
            </ul>
          </div>
        </details>
      </section>

      {/* Live Terminal Drawer */}
      <LiveErrorTerminalDrawer isOpen={showTerminal} onClose={() => setShowTerminal(false)} />

      {/* 🚧 Styles migrated to DiagnosticPage.module.css. This <style> block is dead code (no JSX references global class names any more) and can be safely deleted. */}
      <style>{`
        .diagnostic-page {
          max-width: 1200px;
          margin: 0 auto;
          padding: 24px;
          font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', sans-serif;
        }

        .page-header {
          margin-bottom: 32px;
        }

        .page-header h1 {
          margin: 0 0 8px 0;
          font-size: 32px;
          color: var(--colour-text-primary, #f9fafb);
        }

        .subtitle {
          margin: 0;
          color: var(--colour-text-secondary, #9ca3af);
          font-size: 16px;
        }

        .health-section,
        .config-section,
        .terminal-section,
        .help-section {
          margin-bottom: 32px;
          padding: 24px;
          background: rgba(13, 17, 23, 0.85);
          border-radius: 8px;
          border: 1px solid rgba(0, 245, 255, 0.18);
        }

        .section-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 20px;
          padding-bottom: 12px;
          border-bottom: 2px solid rgba(0, 245, 255, 0.35);
        }

        .section-header h2 {
          margin: 0;
          font-size: 20px;
          color: var(--colour-text-primary, #f9fafb);
        }

        .controls {
          display: flex;
          gap: 16px;
          align-items: center;
        }

        .icon-button {
          background: none;
          border: none;
          cursor: pointer;
          font-size: 18px;
          padding: 8px;
          border-radius: 4px;
          color: var(--colour-neon-cyan, #00f5ff);
        }

        .icon-button:hover:not(:disabled) {
          background: rgba(0, 245, 255, 0.1);
        }

        .icon-button:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }

        .checkbox-label {
          display: flex;
          gap: 6px;
          align-items: center;
          font-size: 12px;
          color: var(--colour-text-secondary, #9ca3af);
          cursor: pointer;
          user-select: none;
        }

        .checkbox-label input {
          cursor: pointer;
        }

        .interval-select {
          padding: 4px 8px;
          border: 1px solid rgba(0, 245, 255, 0.3);
          border-radius: 4px;
          font-size: 12px;
          background: rgba(13, 17, 23, 0.9);
          color: var(--colour-text-primary, #f9fafb);
          cursor: pointer;
        }

        .loading-state {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          min-height: 200px;
          color: var(--colour-text-secondary, #9ca3af);
        }

        .spinner {
          width: 32px;
          height: 32px;
          border: 3px solid rgba(0, 245, 255, 0.15);
          border-top: 3px solid var(--colour-neon-cyan, #00f5ff);
          border-radius: 50%;
          animation: spin 1s linear infinite;
          margin-bottom: 12px;
        }

        @keyframes spin {
          0% { transform: rotate(0deg); }
          100% { transform: rotate(360deg); }
        }

        .error-state {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          min-height: 200px;
          text-align: center;
        }

        .error-message {
          color: #ff6464;
          margin: 0 0 12px 0;
          font-size: 14px;
        }

        .retry-button {
          background: rgba(0, 245, 255, 0.15);
          color: var(--colour-neon-cyan, #00f5ff);
          border: 1px solid rgba(0, 245, 255, 0.4);
          padding: 8px 16px;
          border-radius: 4px;
          cursor: pointer;
          font-size: 14px;
        }

        .retry-button:hover {
          background: rgba(0, 245, 255, 0.25);
        }

        .status-card {
          border: 2px solid;
          border-radius: 8px;
          padding: 16px;
          margin-bottom: 12px;
        }

        .status-card.overall {
          padding: 24px;
        }

        .status-content {
          display: flex;
          justify-content: space-between;
          align-items: center;
        }

        .status-badge {
          display: flex;
          gap: 8px;
          align-items: center;
          font-size: 16px;
          font-weight: bold;
        }

        .status-badge.compact {
          display: inline-flex;
          gap: 4px;
          font-size: 13px;
        }

        .status-icon {
          font-size: 20px;
        }

        .status-meta {
          display: flex;
          gap: 16px;
          font-size: 12px;
          color: var(--colour-text-secondary, #9ca3af);
        }

        .duration {
          font-weight: 500;
        }

        .components-grid {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
          gap: 16px;
          margin-top: 20px;
        }

        .status-card.component {
          display: flex;
          flex-direction: column;
        }

        .component-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: 8px;
        }

        .component-header h3 {
          margin: 0;
          font-size: 14px;
          color: var(--colour-text-primary, #f9fafb);
        }

        .component-description {
          margin: 0 0 12px 0;
          font-size: 12px;
          color: var(--colour-text-secondary, #9ca3af);
          flex: 1;
        }

        .component-footer {
          display: flex;
          justify-content: space-between;
          font-size: 11px;
          color: var(--colour-text-secondary, #9ca3af);
        }

        .terminal-controls {
          text-align: center;
          padding: 20px;
        }

        .primary-button {
          background: rgba(0, 245, 255, 0.15);
          color: var(--colour-neon-cyan, #00f5ff);
          border: 1px solid rgba(0, 245, 255, 0.5);
          padding: 12px 24px;
          border-radius: 4px;
          cursor: pointer;
          font-size: 14px;
          font-weight: 500;
        }

        .primary-button:hover {
          background: rgba(0, 245, 255, 0.25);
        }

        .help-text {
          margin-top: 12px;
          color: var(--colour-text-secondary, #9ca3af);
          font-size: 12px;
        }

        .help-section details {
          margin: 0;
        }

        .help-section summary {
          cursor: pointer;
          font-weight: 500;
          color: var(--colour-neon-cyan, #00f5ff);
          user-select: none;
        }

        .help-section summary:hover {
          text-decoration: underline;
        }

        .help-content {
          margin-top: 16px;
          padding-top: 16px;
          border-top: 1px solid rgba(255, 255, 255, 0.1);
        }

        .help-content h3 {
          margin: 0 0 12px 0;
          font-size: 14px;
          color: var(--colour-text-primary, #f9fafb);
        }

        .shortcuts-table {
          width: 100%;
          border-collapse: collapse;
          margin-bottom: 20px;
        }

        .shortcuts-table tbody tr {
          border-bottom: 1px solid rgba(255, 255, 255, 0.08);
        }

        .shortcuts-table tbody tr:last-child {
          border-bottom: none;
        }

        .shortcuts-table td {
          padding: 8px;
          text-align: left;
          font-size: 12px;
        }

        .shortcuts-table .key {
          font-family: var(--font-mono, 'Courier New', monospace);
          background: rgba(0, 245, 255, 0.1);
          padding: 4px 8px;
          border-radius: 3px;
          font-weight: 500;
          color: var(--colour-neon-cyan, #00f5ff);
          border: 1px solid rgba(0, 245, 255, 0.3);
        }

        .help-content ul {
          margin: 0;
          padding-left: 20px;
          font-size: 12px;
          color: var(--colour-text-secondary, #9ca3af);
        }

        .help-content li {
          margin-bottom: 8px;
        }
      `}</style>
    </div>
  );
};
