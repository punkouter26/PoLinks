// T058: Masked configuration panel component
// Displays masked (redacted) configuration for safe inspection without exposing secrets
import { useEffect, useState } from 'react';
import styles from './MaskedConfigPanel.module.css';

interface MaskedConfig {
  environment: string;
  applicationName: string;
  applicationVersion: string;
  maskedSettings: Record<string, string>;
}

interface MaskedConfigPanelProps {
  refreshInterval?: number; // milliseconds
}

export const MaskedConfigPanel: React.FC<MaskedConfigPanelProps> = ({
  refreshInterval = 5000,
}) => {
  const [config, setConfig] = useState<MaskedConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const fetchConfig = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await fetch('/diagnostic/config');

      if (!response.ok) {
        throw new Error(`HTTP Error: ${response.status}`);
      }

      const data = (await response.json()) as MaskedConfig;
      setConfig(data);
      setLastUpdated(new Date());
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setError(`Failed to load configuration: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    // Initial fetch
    fetchConfig();

    // Set up refresh interval
    const interval = setInterval(fetchConfig, refreshInterval);

    return () => clearInterval(interval);
  }, [refreshInterval]);

  if (loading && !config) {
    return (
      <div className={`${styles.panel} ${styles.loading}`} data-testid="config-loading">
        <p>Loading configuration...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`${styles.panel} ${styles.error}`} data-testid="config-error">
        <p className={styles.errorMessage}>{error}</p>
        <button onClick={fetchConfig} className={styles.retryButton}>
          Retry
        </button>
      </div>
    );
  }

  if (!config) {
    return (
      <div className={`${styles.panel} ${styles.empty}`} data-testid="config-empty">
        <p>No configuration data available</p>
      </div>
    );
  }

  return (
    <div className={styles.panel} data-testid="masked-config-panel">
      <div className={styles.panelHeader}>
        <h2>Application Configuration</h2>
        <div className={styles.headerMetadata}>
          <span className={styles.environmentBadge}>{config.environment}</span>
          <span className={styles.versionText}>v{config.applicationVersion}</span>
          {lastUpdated && (
            <span className={styles.lastUpdated}>
              Updated: {lastUpdated.toLocaleTimeString()}
            </span>
          )}
          <button
            onClick={fetchConfig}
            className={styles.refreshButton}
            title="Refresh configuration"
          >
            ↻
          </button>
        </div>
      </div>

      <div className={styles.panelContent}>
        <section className={styles.configSection}>
          <h3>Environment</h3>
          <div className={styles.configItem}>
            <span className={styles.configLabel}>Name:</span>
            <span className={styles.configValue}>{config.environment}</span>
          </div>
          <div className={styles.configItem}>
            <span className={styles.configLabel}>Version:</span>
            <span className={styles.configValue}>{config.applicationVersion}</span>
          </div>
        </section>

        <section className={styles.configSection}>
          <h3>Settings</h3>
          {Object.keys(config.maskedSettings).length > 0 ? (
            <div className={styles.configTable}>
              <table>
                <thead>
                  <tr>
                    <th>Key</th>
                    <th>Value</th>
                  </tr>
                </thead>
                <tbody>
                  {Object.entries(config.maskedSettings).map(([key, value]) => (
                    <tr key={key}>
                      <td className={styles.configKey}>{key}</td>
                      <td className={styles.configValue}>
                        {value.includes('[REDACTED]') ? (
                          <span className={styles.maskedValue} title="This value contains secrets and is masked">
                            {value}
                          </span>
                        ) : (
                          <span>{value}</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className={styles.emptySection}>No settings configured</p>
          )}
        </section>
      </div>

      <div className={styles.panelFooter}>
        <p className={styles.securityNotice}>
          ⓘ Sensitive values are automatically masked with [REDACTED] for security.
        </p>
        <button onClick={fetchConfig} className={styles.primaryButton}>
          Refresh Configuration
        </button>
      </div>
    </div>
  );
};
