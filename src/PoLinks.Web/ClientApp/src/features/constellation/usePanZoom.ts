import { useEffect, useRef } from "react";

export interface PanZoomTransform {
  x: number;
  y: number;
  scale: number;
}

export function usePanZoom(canvasRef: React.RefObject<HTMLCanvasElement | null>) {
  const transformRef = useRef<PanZoomTransform>({ x: 0, y: 0, scale: 1 });

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) {
      return;
    }

    let isDragging = false;
    let lastX = 0;
    let lastY = 0;

    function onPointerDown(event: PointerEvent) {
      isDragging = true;
      lastX = event.clientX;
      lastY = event.clientY;
      canvas?.setPointerCapture(event.pointerId);
    }

    function onPointerMove(event: PointerEvent) {
      if (!isDragging) {
        return;
      }

      transformRef.current = {
        ...transformRef.current,
        x: transformRef.current.x + (event.clientX - lastX),
        y: transformRef.current.y + (event.clientY - lastY),
      };

      lastX = event.clientX;
      lastY = event.clientY;
    }

    function onPointerUp(event: PointerEvent) {
      isDragging = false;
      if (canvas?.hasPointerCapture(event.pointerId)) {
        canvas.releasePointerCapture(event.pointerId);
      }
    }

    function onWheel(event: WheelEvent) {
      event.preventDefault();
      const delta = event.deltaY < 0 ? 0.08 : -0.08;
      const nextScale = Math.min(2.5, Math.max(0.7, transformRef.current.scale + delta));
      transformRef.current = {
        ...transformRef.current,
        scale: nextScale,
      };
    }

    canvas.addEventListener("pointerdown", onPointerDown);
    canvas.addEventListener("pointermove", onPointerMove);
    canvas.addEventListener("pointerup", onPointerUp);
    canvas.addEventListener("pointerleave", onPointerUp);
    canvas.addEventListener("wheel", onWheel, { passive: false });

    return () => {
      canvas.removeEventListener("pointerdown", onPointerDown);
      canvas.removeEventListener("pointermove", onPointerMove);
      canvas.removeEventListener("pointerup", onPointerUp);
      canvas.removeEventListener("pointerleave", onPointerUp);
      canvas.removeEventListener("wheel", onWheel);
    };
  }, [canvasRef]);

  return transformRef;
}
