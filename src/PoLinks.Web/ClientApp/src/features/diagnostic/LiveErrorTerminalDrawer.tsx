// T059: Live error terminal drawer component
// Displays real-time diagnostic logs with filtering and masking
import { useEffect, useRef, useState } from 'react';

interface DiagnosticLogEntry {
  id: string;
  level: string;
  message: string;
  exception?: string;
  context: Record<string, string>;
  timestamp: string;
}

type LogLevel = 'all' | 'error' | 'warning' | 'info' | 'debug';

interface LiveErrorTerminalDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  autoScroll?: boolean;
}

export const LiveErrorTerminalDrawer: React.FC<LiveErrorTerminalDrawerProps> = ({
  isOpen,
  onClose,
  autoScroll = true,
}) => {
  const [logs, setLogs] = useState<DiagnosticLogEntry[]>([]);
  const [filterLevel, setFilterLevel] = useState<LogLevel>('all');
  const [fetchError, setFetchError] = useState<string | null>(null);
  const terminalRef = useRef<HTMLDivElement>(null);

  // Fetch logs when opened; poll every 5 seconds while the drawer is open.
  useEffect(() => {
    if (!isOpen) return;

    const abortCtrl = new AbortController();

    async function fetchLogs() {
      try {
        const res = await fetch('/diagnostic/logs', { signal: abortCtrl.signal });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = (await res.json()) as { logs: DiagnosticLogEntry[] };
        setLogs(data.logs);
        setFetchError(null);
      } catch (err) {
        if ((err as Error).name === 'AbortError') return;
        setFetchError((err as Error).message ?? 'Failed to load logs');
      }
    }

    void fetchLogs();
    const intervalId = setInterval(() => { void fetchLogs(); }, 5000);

    return () => {
      abortCtrl.abort();
      clearInterval(intervalId);
    };
  }, [isOpen]);

  // Auto-scroll to bottom when new logs arrive
  useEffect(() => {
    if (autoScroll && terminalRef.current) {
      terminalRef.current.scrollTop = terminalRef.current.scrollHeight;
    }
  }, [logs, autoScroll]);

  const filteredLogs = logs.filter((log) => {
    if (filterLevel === 'all') return true;
    return log.level.toLowerCase() === filterLevel.toLowerCase();
  });

  const handleClearLogs = () => {
    setLogs([]);
  };

  const handleExportLogs = () => {
    const json = JSON.stringify(logs, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `diagnostic-logs-${new Date().toISOString()}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  if (!isOpen) return null;

  return (
    <div className="live-error-drawer" data-testid="log-drawer">
      <div className="drawer-overlay" onClick={onClose} />

      <div className="drawer-panel">
        <div className="drawer-header">
          <h2>Diagnostic Logs</h2>
          <button
            className="close-button"
            onClick={onClose}
            title="Close drawer"
            aria-label="Close diagnostic logs"
          >
            ×
          </button>
        </div>

        <div className="drawer-toolbar">
          <div className="filter-group">
            <label htmlFor="level-filter">Filter by level:</label>
            <select
              id="level-filter"
              value={filterLevel}
              onChange={(e) => setFilterLevel(e.target.value as LogLevel)}
              className="filter-select"
              data-testid="severity-filter"
            >
              <option value="all">All Levels</option>
              <option value="error">Error</option>
              <option value="warning">Warning</option>
              <option value="info">Info</option>
              <option value="debug">Debug</option>
            </select>
          </div>

          <div className="button-group">
            {logs.length > 0 && (
              <>
                <button
                  onClick={handleExportLogs}
                  className="toolbar-button"
                  title="Export logs as JSON"
                >
                  ⬇ Export
                </button>
                <button
                  onClick={handleClearLogs}
                  className="toolbar-button danger"
                  title="Clear all logs"
                  data-testid="clear-logs"
                >
                  🗑 Clear
                </button>
              </>
            )}
          </div>

          <div className="log-stats">
            <span className="log-count">
              {filteredLogs.length} / {logs.length} logs
            </span>
          </div>
        </div>

        <div
          className="drawer-content"
          ref={terminalRef}
          data-testid="log-drawer"
        >
          {logs.length === 0 ? (
            <div className="empty-state">
              <p>No logs yet. Check back soon!</p>
              {fetchError && <p role="alert" className="fetch-error">Error: {fetchError}</p>}
            </div>
          ) : filteredLogs.length === 0 ? (
            <div className="empty-state">
              <p>No logs match the selected filter.</p>
            </div>
          ) : (
            <div className="log-list">
              {filteredLogs.map((log) => (
                <div
                  key={log.id}
                  className="log-entry"
                  data-level={log.level.toLowerCase()}
                  data-testid="log-entry"
                >
                  <div className="log-header">
                    <span className="log-level">
                      [{log.level.toUpperCase()}]
                    </span>
                    <span className="log-time">
                      {new Date(log.timestamp).toLocaleTimeString()}
                    </span>
                  </div>
                  <div className="log-message">{log.message}</div>
                  {log.exception && (
                    <details className="log-exception">
                      <summary>Exception Details</summary>
                      <pre>{log.exception}</pre>
                    </details>
                  )}
                  {Object.keys(log.context).length > 0 && (
                    <details className="log-context">
                      <summary>Context</summary>
                      <div className="context-items">
                        {Object.entries(log.context).map(([key, value]) => (
                          <div key={key} className="context-item">
                            <span className="context-key">{key}:</span>
                            <span className="context-value">{value}</span>
                          </div>
                        ))}
                      </div>
                    </details>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      <style>{`
        /* UI-5: Dark cyber theme for LiveErrorTerminalDrawer */
        .live-error-drawer {
          position: fixed;
          top: 0;
          right: 0;
          bottom: 0;
          left: 0;
          z-index: 1000;
          display: flex;
          align-items: flex-end;
        }

        .drawer-overlay {
          position: absolute;
          inset: 0;
          background: rgba(0, 0, 0, 0.65);
        }

        .drawer-panel {
          position: relative;
          width: 100%;
          height: 50vh;
          background: rgba(10, 14, 26, 0.97);
          display: flex;
          flex-direction: column;
          box-shadow: 0 -4px 32px rgba(0, 0, 0, 0.7), 0 0 0 1px rgba(0, 245, 255, 0.15);
          border-radius: 10px 10px 0 0;
          overflow: hidden;
        }

        .drawer-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: 14px 18px;
          border-bottom: 1px solid rgba(0, 245, 255, 0.2);
          border-top: 3px solid var(--colour-neon-cyan, #00f5ff);
          flex-shrink: 0;
        }

        .drawer-header h2 {
          margin: 0;
          font-size: 14px;
          font-family: var(--font-mono, 'JetBrains Mono', monospace);
          font-weight: 700;
          letter-spacing: 0.1em;
          text-transform: uppercase;
          color: var(--colour-neon-cyan, #00f5ff);
        }

        .close-button {
          background: transparent;
          border: 1px solid transparent;
          font-size: 20px;
          cursor: pointer;
          color: var(--colour-text-secondary, #9ca3af);
          padding: 0;
          width: 30px;
          height: 30px;
          display: flex;
          align-items: center;
          justify-content: center;
          border-radius: 4px;
          transition: background 140ms ease, border-color 140ms ease, color 140ms ease;
        }

        .close-button:hover {
          background: rgba(0, 245, 255, 0.08);
          border-color: rgba(0, 245, 255, 0.25);
          color: var(--colour-text-primary, #f9fafb);
        }

        .drawer-toolbar {
          display: flex;
          gap: 14px;
          align-items: center;
          padding: 10px 18px;
          background: rgba(17, 24, 39, 0.9);
          border-bottom: 1px solid rgba(0, 245, 255, 0.1);
          flex-wrap: wrap;
          flex-shrink: 0;
        }

        .filter-group {
          display: flex;
          gap: 8px;
          align-items: center;
        }

        .filter-group label {
          font-size: 11px;
          font-family: var(--font-mono, monospace);
          color: var(--colour-text-secondary, #9ca3af);
          font-weight: 500;
        }

        .filter-select {
          padding: 4px 8px;
          border: 1px solid rgba(0, 245, 255, 0.3);
          border-radius: 4px;
          font-size: 11px;
          font-family: var(--font-mono, monospace);
          background: rgba(13, 17, 23, 0.9);
          color: var(--colour-text-primary, #f9fafb);
          cursor: pointer;
          outline: none;
        }

        .filter-select:focus {
          border-color: rgba(0, 245, 255, 0.6);
        }

        .button-group {
          display: flex;
          gap: 8px;
        }

        .toolbar-button {
          background: rgba(0, 245, 255, 0.1);
          color: var(--colour-neon-cyan, #00f5ff);
          border: 1px solid rgba(0, 245, 255, 0.35);
          padding: 4px 12px;
          border-radius: 4px;
          font-size: 11px;
          font-family: var(--font-mono, monospace);
          font-weight: 500;
          cursor: pointer;
          transition: background 140ms ease;
        }

        .toolbar-button:hover {
          background: rgba(0, 245, 255, 0.2);
        }

        .toolbar-button.danger {
          background: rgba(255, 100, 100, 0.1);
          color: var(--colour-neon-red, #ff6464);
          border-color: rgba(255, 100, 100, 0.35);
        }

        .toolbar-button.danger:hover {
          background: rgba(255, 100, 100, 0.2);
        }

        .log-stats {
          display: flex;
          gap: 16px;
          align-items: center;
          font-size: 11px;
          font-family: var(--font-mono, monospace);
          color: var(--colour-text-secondary, #9ca3af);
          margin-left: auto;
        }

        .log-count { font-weight: 600; }

        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50%       { opacity: 0.5; }
        }

        .drawer-content {
          flex: 1;
          overflow-y: auto;
          overflow-x: hidden;
          font-family: var(--font-mono, 'JetBrains Mono', 'Courier New', monospace);
          padding: 10px;
          background: rgba(5, 8, 16, 0.9);
        }

        .empty-state {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          height: 100%;
          color: var(--colour-text-secondary, #9ca3af);
          gap: 6px;
          font-family: var(--font-mono, monospace);
          font-size: 13px;
        }

        .fetch-error {
          color: var(--colour-neon-red, #ff6464);
          margin: 4px 0 0;
          font-size: 12px;
        }

        .log-list {
          display: flex;
          flex-direction: column;
          gap: 4px;
        }

        .log-entry {
          border-left: 3px solid rgba(255, 255, 255, 0.12);
          padding: 10px 12px;
          font-size: 11px;
          line-height: 1.6;
          border-radius: 0 4px 4px 0;
          background: rgba(255, 255, 255, 0.03);
          transition: background 140ms ease;
        }

        .log-entry:hover {
          background: rgba(255, 255, 255, 0.055);
        }

        .log-entry[data-level="error"]   { border-left-color: var(--colour-neon-red, #ff6464); }
        .log-entry[data-level="warning"] { border-left-color: var(--colour-neon-amber, #fbbf24); }
        .log-entry[data-level="info"]    { border-left-color: var(--colour-neon-cyan, #00f5ff); }
        .log-entry[data-level="debug"]   { border-left-color: rgba(255, 255, 255, 0.2); }

        .log-header {
          display: flex;
          gap: 12px;
          align-items: center;
          margin-bottom: 5px;
        }

        .log-level {
          font-weight: 700;
          min-width: 60px;
          font-size: 10px;
          letter-spacing: 0.08em;
        }

        .log-entry[data-level="error"]   .log-level { color: var(--colour-neon-red, #ff6464); }
        .log-entry[data-level="warning"] .log-level { color: var(--colour-neon-amber, #fbbf24); }
        .log-entry[data-level="info"]    .log-level { color: var(--colour-neon-cyan, #00f5ff); }
        .log-entry[data-level="debug"]   .log-level { color: var(--colour-text-muted, #6b7280); }

        .log-time {
          color: var(--colour-text-muted, #6b7280);
          font-size: 10px;
        }

        .log-message {
          color: var(--colour-text-secondary, #d1d5db);
          white-space: pre-wrap;
          word-break: break-word;
        }

        .log-exception,
        .log-context {
          margin-top: 8px;
        }

        .log-exception summary,
        .log-context summary {
          cursor: pointer;
          color: var(--colour-neon-cyan, #00f5ff);
          font-weight: 500;
          font-size: 10px;
          -webkit-user-select: none;
          user-select: none;
          opacity: 0.75;
        }

        .log-exception summary:hover,
        .log-context summary:hover {
          opacity: 1;
        }

        .log-exception pre {
          background: rgba(255, 100, 100, 0.06);
          border: 1px solid rgba(255, 100, 100, 0.2);
          padding: 8px;
          border-radius: 4px;
          overflow-x: auto;
          margin: 6px 0 0 0;
          color: var(--colour-neon-red, #ff6464);
          font-size: 10px;
        }

        .context-items {
          margin-top: 6px;
          padding: 8px;
          background: rgba(255, 255, 255, 0.04);
          border-radius: 4px;
          border: 1px solid rgba(255, 255, 255, 0.06);
        }

        .context-item {
          display: flex;
          gap: 8px;
          margin-bottom: 3px;
          font-size: 10px;
        }

        .context-item:last-child { margin-bottom: 0; }

        .context-key {
          color: var(--colour-text-muted, #6b7280);
          font-weight: 600;
          min-width: 100px;
        }

        .context-value {
          color: var(--colour-text-secondary, #9ca3af);
          word-break: break-all;
        }
      `}</style>
    </div>
  );
};
