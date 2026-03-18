export type RendererMode = "webgl2" | "canvas2d";

export interface RendererProfile {
  mode: RendererMode;
  pixelRatio: number;
  maxNodes: number;
  enableGlow: boolean;
}

export function selectRendererMode(webGl2Available: boolean): RendererMode {
  return webGl2Available ? "webgl2" : "canvas2d";
}

export function detectRendererProfile(): RendererProfile {
  // Use a temporary off-screen canvas to probe WebGL2 availability.
  // Never call getContext("webgl2") on the same canvas that will be used for 2D
  // rendering — a canvas may only hold one active context at a time.
  const probe = document.createElement("canvas");
  const webGl2Available = probe.getContext("webgl2") !== null;
  const mode = selectRendererMode(webGl2Available);

  return {
    mode,
    pixelRatio: Math.min(window.devicePixelRatio || 1, webGl2Available ? 2 : 1.5),
    maxNodes: webGl2Available ? 100 : 70,
    enableGlow: webGl2Available,
  };
}
