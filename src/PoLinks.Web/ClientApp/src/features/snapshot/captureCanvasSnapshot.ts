export interface SnapshotCaptureOptions {
  fileName: string;
  scale: number;
  backgroundColor?: string;
  sourceCanvas?: HTMLCanvasElement | null;
}

function downloadBlob(blob: Blob, fileName: string) {
  const objectUrl = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = objectUrl;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(objectUrl);
}

function cloneCanvasAtScale(sourceCanvas: HTMLCanvasElement, scale: number, backgroundColor: string) {
  const targetCanvas = document.createElement('canvas');
  targetCanvas.width = sourceCanvas.width * scale;
  targetCanvas.height = sourceCanvas.height * scale;

  const context = targetCanvas.getContext('2d');
  if (!context) {
    throw new Error('Failed to create export canvas context');
  }

  context.fillStyle = backgroundColor;
  context.fillRect(0, 0, targetCanvas.width, targetCanvas.height);
  context.scale(scale, scale);
  context.drawImage(sourceCanvas, 0, 0, sourceCanvas.width, sourceCanvas.height);

  return targetCanvas;
}

export async function captureCanvasSnapshot({
  fileName,
  scale,
  sourceCanvas,
  backgroundColor = '#04070d',
}: SnapshotCaptureOptions) {
  if (!sourceCanvas) {
    throw new Error('No constellation canvas available for export');
  }

  const exportCanvas = cloneCanvasAtScale(sourceCanvas, scale, backgroundColor);
  const blob = await new Promise<Blob>((resolve, reject) => {
    exportCanvas.toBlob((value) => {
      if (value) {
        resolve(value);
        return;
      }

      reject(new Error('Canvas export failed'));
    }, 'image/png');
  });

  downloadBlob(blob, fileName);
}
