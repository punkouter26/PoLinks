import { useState } from 'react';
import { captureCanvasSnapshot } from './captureCanvasSnapshot';
import { apiFetch } from '../../utils/apiFetch';

interface SnapshotExportMetadataDto {
  fileName: string;
  contentType: string;
  format: string;
  scale: number;
  generatedAtUtc: string;
}

export function ExportSnapshotButton() {
  const [isExporting, setIsExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleExport = async () => {
    setIsExporting(true);
    setError(null);

    try {
      const metadata = await apiFetch<SnapshotExportMetadataDto>(
        '/api/snapshot/export-metadata?format=png&scale=2',
      );
      const sourceCanvas = document.querySelector<HTMLCanvasElement>('canvas[data-layer="main"]');

      await captureCanvasSnapshot({
        fileName: metadata.fileName,
        scale: metadata.scale,
        sourceCanvas,
      });
    } catch (value) {
      setError(value instanceof Error ? value.message : 'Snapshot export failed');
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <div
      style={{
        position: 'absolute',
        top: 60,
        right: 240,
        zIndex: 30,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'flex-end',
        gap: 8,
      }}
    >
      <button
        data-testid="export-snapshot-button"
        type="button"
        onClick={handleExport}
        disabled={isExporting}
        style={{
          border: '1px solid rgba(0, 245, 255, 0.4)',
          background: 'rgba(4, 7, 13, 0.88)',
          color: 'var(--colour-text-primary)',
          padding: '0.65rem 0.9rem',
          borderRadius: 10,
          fontSize: '0.875rem',
          letterSpacing: '0.04em',
          cursor: isExporting ? 'progress' : 'pointer',
          boxShadow: '0 8px 24px rgba(0, 0, 0, 0.25)',
        }}
      >
        {isExporting ? 'Exporting...' : 'Export Snapshot'}
      </button>
      {error ? (
        <div
          role="alert"
          style={{
            maxWidth: 280,
            fontSize: '0.75rem',
            color: '#ffb4b4',
            background: 'rgba(80, 10, 10, 0.85)',
            borderRadius: 8,
            padding: '0.5rem 0.65rem',
          }}
        >
          {error}
        </div>
      ) : null}
    </div>
  );
}
