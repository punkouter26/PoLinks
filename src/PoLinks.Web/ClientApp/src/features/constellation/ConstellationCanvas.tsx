// T035: D3 Canvas physics scene and anchor rendering (FR-001).
// Expects an empty array initially, updates graph state on pulses.
import { useEffect, useRef } from "react";
import * as d3 from "d3";
import { usePulseState } from "../../context/PulseStateContext";
import { getPulseAnimationFrame } from "./pulseAnimation";
import { detectRendererProfile } from "./rendererProfile";
import { usePanZoom } from "./usePanZoom";
import type { NexusNode, NexusLink } from "../../types/nexus";

// D3 simulation expects nodes to be objects mutated in place with x,y coords
type SimNode = NexusNode & d3.SimulationNodeDatum;
type SimLink = d3.SimulationLinkDatum<SimNode> & NexusLink;

interface Props {
  /** Single click — focus/select the node in the constellation. */
  onNodeClick?: (nodeId: string) => void;
  /** Double click — open the full detail panel for the node. */
  onNodeDoubleClick?: (nodeId: string) => void;
  /** Expansion Post nodes added by recursive double-click drilldown. */
  expansionNodes?: NexusNode[];
  /** Expansion links connecting parent Topic/Post nodes to new Post children. */
  expansionLinks?: NexusLink[];
}

interface CanvasTheme {
  colourNeonCyan: string;
  colourNeonViolet: string;
  colourNeonPink: string;
  colourTextPrimary: string;
  linkOpacity: number;
  nodeAnchorRadius: number;
  nodeTopicRadius: number;
  nodePostRadius: number;
  fontMono: string;
}

function parseCssNumber(value: string, fallback: number): number {
  const parsed = Number.parseFloat(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function getCanvasTheme(): CanvasTheme {
  const css = getComputedStyle(document.documentElement);

  return {
    colourNeonCyan: css.getPropertyValue("--colour-neon-cyan").trim() || "#00f5ff",
    colourNeonViolet: css.getPropertyValue("--colour-neon-violet").trim() || "#7c3aed",
    colourNeonPink: css.getPropertyValue("--colour-neon-pink").trim() || "#f472b6",
    colourTextPrimary: css.getPropertyValue("--colour-text-primary").trim() || "#f9fafb",
    linkOpacity: parseCssNumber(css.getPropertyValue("--link-opacity"), 0.4),
    nodeAnchorRadius: parseCssNumber(css.getPropertyValue("--node-anchor-radius"), 20),
    nodeTopicRadius: parseCssNumber(css.getPropertyValue("--node-topic-radius"), 8),
    nodePostRadius: parseCssNumber(css.getPropertyValue("--node-post-radius"), 16),
    fontMono: css.getPropertyValue("--font-mono").trim() || '"JetBrains Mono", "Fira Code", monospace',
  };
}

function getAnchorLabelLines(ctx: CanvasRenderingContext2D, label: string, maxWidth: number): string[] {
  const trimmedLabel = label.trim();
  if (trimmedLabel.length === 0 || ctx.measureText(trimmedLabel).width <= maxWidth) {
    return [trimmedLabel];
  }

  const words = trimmedLabel.split(/\s+/);
  if (words.length < 2) {
    return [trimmedLabel];
  }

  const lines: string[] = [];
  let currentLine = words[0];

  for (let index = 1; index < words.length; index += 1) {
    const nextLine = `${currentLine} ${words[index]}`;
    const remainingWords = words.length - index;

    if (ctx.measureText(nextLine).width <= maxWidth || lines.length === 1 || remainingWords === 1) {
      currentLine = nextLine;
      continue;
    }

    lines.push(currentLine);
    currentLine = words[index];
  }

  lines.push(currentLine);

  if (lines.length <= 2) {
    return lines;
  }

  return [lines[0], lines.slice(1).join(" ")];
}

export function ConstellationCanvas({
  onNodeClick,
  onNodeDoubleClick,
  expansionNodes = [],
  expansionLinks = [],
}: Props) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const { batches, lastPulseAt } = usePulseState();
  const panZoomRef = usePanZoom(canvasRef);
  const selectedNodeIdRef = useRef<string | null>(null);
  // Keep stable refs to callbacks so the event handlers always use the latest version
  const onNodeClickRef = useRef(onNodeClick);
  const onNodeDoubleClickRef = useRef(onNodeDoubleClick);
  useEffect(() => { onNodeClickRef.current = onNodeClick; }, [onNodeClick]);
  useEffect(() => { onNodeDoubleClickRef.current = onNodeDoubleClick; }, [onNodeDoubleClick]);

  // Hoisted simulation state — stable refs so data updates never rebuild the canvas or RAF loop
  const simulationRef = useRef<d3.Simulation<SimNode, SimLink> | null>(null);
  const currentNodesRef = useRef<SimNode[]>([]);
  const currentLinksRef = useRef<SimLink[]>([]);
  // Keep lastPulseAt accessible inside the render loop without recreating the effect
  const lastPulseAtRef = useRef<number | null>(null);
  useEffect(() => { lastPulseAtRef.current = lastPulseAt; }, [lastPulseAt]);

  // Tracks nodes that have been evicted and are in the process of fading out
  type FadeGhost = { x: number; y: number; label: string; isAnchor: boolean; opacity: number; evictedAt: number };
  const fadeOutMapRef = useRef<Map<string, FadeGhost>>(new Map());
  // Snapshot of last rendered node set — used to detect evictions across pulses
  const prevNodesRef = useRef<SimNode[]>([]);
  // Tracks when each node was first introduced — drives heat ring fade (#2)
  const nodeFirstSeenRef = useRef<Map<string, number>>(new Map());
  // Maps target node ID → incoming link count — drives edge weight encoding (#3)
  const nodeLinkCountRef = useRef<Map<string, number>>(new Map());

  // Mount effect: create canvas context, simulation, RAF loop, resize handler, and click handlers.
  // Runs ONCE — the simulation is never torn down on data updates, so nodes retain their positions.
  useEffect(() => {
    const canvas = canvasRef.current;
    const container = containerRef.current;
    if (!canvas || !container) return;

    const rendererProfile = detectRendererProfile();
    const theme = getCanvasTheme();
    canvas.setAttribute("data-renderer", rendererProfile.mode);

    const ctx = canvas.getContext("2d");
    if (!ctx) return; // For MVP, we render via 2D canvas even if webgl2 is detected as available

    let width = container.clientWidth;
    let height = container.clientHeight;

    const dpr = rendererProfile.pixelRatio;
    function resize() {
      if (!container || !canvas || !ctx) return;
      width = container.clientWidth;
      height = container.clientHeight;
      canvas.width = width * dpr;
      canvas.height = height * dpr;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    }
    resize();
    window.addEventListener("resize", resize);

    // Returns the visual radius for a node. Anchor, Topic, and Post each have fixed theme radii.
    function getNodeRadius(node: SimNode): number {
      if (node.type === "Anchor") return theme.nodeAnchorRadius;
      if (node.type === "Post")   return theme.nodePostRadius;
      return theme.nodeTopicRadius;
    }

    // Initialise empty simulation
    const simulation = d3
      .forceSimulation<SimNode>()
      .force("charge", d3.forceManyBody().strength(-30))
      .force("center", d3.forceCenter(width / 2, height / 2))
      .force("collide", d3.forceCollide<SimNode>().radius((d) => (d.type === "Anchor" ? theme.nodeAnchorRadius + 20 : getNodeRadius(d) + 6)));
    simulationRef.current = simulation;

    function ticked() {
      if (!ctx) return;
      // Read from refs so we always use the latest data without recreating this closure
      const nodes = currentNodesRef.current;
      const links = currentLinksRef.current;

      ctx.clearRect(0, 0, width, height);

      const animation = getPulseAnimationFrame(lastPulseAtRef.current, Date.now(), { width, height });
      ctx.save();
      ctx.strokeStyle = `rgba(0, 245, 255, ${animation.rippleOpacity})`;
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.arc(width / 2, height / 2, animation.rippleRadius, 0, Math.PI * 2);
      ctx.stroke();
      ctx.restore();

      ctx.save();
      const transform = panZoomRef.current;
      ctx.translate(transform.x, transform.y);
      ctx.scale(transform.scale, transform.scale);

      // Draw links with edge-weight encoding.
      // Expansion links (target is a Post) are drawn in neon-pink at lower opacity.
      for (const link of links) {
        const s = link.source as unknown as { x: number; y: number; id: string };
        const t = link.target as unknown as { x: number; y: number; id: string; type?: string };
        if (s && t) {
          const isExpansionLink = (t as SimNode).type === "Post";
          ctx.strokeStyle = isExpansionLink ? theme.colourNeonPink : theme.colourNeonViolet;
          const linkCount = nodeLinkCountRef.current.get(t.id) ?? 1;
          ctx.lineWidth = Math.min(0.8 + linkCount * 0.4, 4);
          ctx.globalAlpha = Math.min(theme.linkOpacity * (0.6 + linkCount * 0.15), 0.8);
          ctx.beginPath();
          ctx.moveTo(s.x, s.y);
          ctx.lineTo(t.x, t.y);
          ctx.stroke();
        }
      }
      ctx.globalAlpha = 1.0;
      // Draw fading-out ghost nodes (evicted nodes, opacity 1→0 over 600 ms)
      const now = Date.now();
      const FADE_DURATION = 600;
      for (const [id, ghost] of fadeOutMapRef.current) {
        const elapsed = now - ghost.evictedAt;
        if (elapsed >= FADE_DURATION) {
          fadeOutMapRef.current.delete(id);
          continue;
        }
        const opacity = (1 - elapsed / FADE_DURATION) * 0.7;
        const r = ghost.isAnchor ? theme.nodeAnchorRadius : theme.nodeTopicRadius;
        ctx.globalAlpha = opacity;
        ctx.beginPath();
        ctx.arc(ghost.x, ghost.y, r, 0, 2 * Math.PI);
        ctx.fillStyle = ghost.isAnchor ? theme.colourNeonCyan : theme.colourNeonViolet;
        ctx.fill();
        ctx.globalAlpha = 1.0;
      }

      // Draw active nodes
      const HEAT_DURATION = 10_000;
      for (const node of nodes) {
        const r = getNodeRadius(node);
        const nx = node.x ?? 0;
        const ny = node.y ?? 0;

        // Topic nodes: fade opacity for low-hype nodes so high-hype nodes stand out visually.
        // Anchors are always fully opaque.
        const nodeAlpha = node.type === "Anchor"
          ? 1.0
          : Math.max(0.3, Math.min(1.0, 0.3 + (node.hypeScore / 10) * 0.7));
        ctx.globalAlpha = nodeAlpha;

        // Heat ring — glow that fades over HEAT_DURATION after a node first appears (#2)
        const firstSeen = nodeFirstSeenRef.current.get(node.id);
        if (firstSeen !== undefined) {
          const age = now - firstSeen;
          if (age < HEAT_DURATION) {
            ctx.globalAlpha = nodeAlpha * (1 - age / HEAT_DURATION) * 0.5;
            ctx.beginPath();
            ctx.arc(nx, ny, r + 9, 0, 2 * Math.PI);
            ctx.strokeStyle = node.type === "Anchor"
              ? theme.colourNeonCyan
              : node.type === "Post"
                ? theme.colourNeonPink
                : theme.colourNeonViolet;
            ctx.lineWidth = 2.5;
            ctx.stroke();
            ctx.globalAlpha = nodeAlpha;
          }
        }

        ctx.beginPath();
        ctx.arc(nx, ny, r, 0, 2 * Math.PI);
        // Post nodes use their sentiment colour when available, otherwise neon-pink
        ctx.fillStyle = node.type === "Anchor"
          ? theme.colourNeonCyan
          : node.type === "Post"
            ? (node.sentimentColour ?? theme.colourNeonPink)
            : theme.colourNeonViolet;
        ctx.fill();
        if (rendererProfile.enableGlow) {
          ctx.shadowBlur = node.type === "Anchor" ? 18 : (node.hypeScore > 6 ? 8 : 4);
          ctx.shadowColor = ctx.fillStyle;
        }

        // Anchor label — drawn inside the circle, centred vertically
        if (node.type === "Anchor") {
          ctx.globalAlpha = 1.0;
          ctx.fillStyle = theme.colourTextPrimary;
          ctx.font = `10px ${theme.fontMono}`;
          ctx.textAlign = "center";
          ctx.textBaseline = "middle";
          const labelLines = getAnchorLabelLines(ctx, node.label, r * 1.75);
          const lineHeight = 13;
          const totalHeight = (labelLines.length - 1) * lineHeight;
          labelLines.forEach((line, index) => {
            ctx.fillText(line, nx, ny - totalHeight / 2 + index * lineHeight);
          });
          ctx.textBaseline = "alphabetic";
        }

        // Post label — first 30 chars of text, truncated, inside the node
        if (node.type === "Post") {
          ctx.globalAlpha = nodeAlpha * 0.9;
          ctx.fillStyle = theme.colourTextPrimary;
          ctx.font = `7px ${theme.fontMono}`;
          ctx.textAlign = "center";
          ctx.textBaseline = "middle";
          const maxLabelWidth = r * 1.7;
          let postLabel = node.label;
          while (postLabel.length > 2 && ctx.measureText(postLabel).width > maxLabelWidth) {
            postLabel = postLabel.slice(0, -1);
          }
          if (postLabel.length < node.label.length) postLabel = postLabel.slice(0, -1) + "\u2026";
          ctx.fillText(postLabel, nx, ny);
          ctx.textBaseline = "alphabetic";
        }

        // Topic label — keyword truncated to fit inside the node circle
        if (node.type === "Topic" && node.topKeyword) {
          ctx.globalAlpha = nodeAlpha * 0.9;
          ctx.fillStyle = theme.colourTextPrimary;
          ctx.font = `8px ${theme.fontMono}`;
          ctx.textAlign = "center";
          ctx.textBaseline = "middle";
          const maxLabelWidth = r * 1.7;
          let label = node.topKeyword;
          while (label.length > 2 && ctx.measureText(label).width > maxLabelWidth) {
            label = label.slice(0, -1);
          }
          if (label.length < node.topKeyword.length) label = label.slice(0, -1) + "\u2026";
          ctx.fillText(label, nx, ny);
          ctx.textBaseline = "alphabetic";
        }

        ctx.shadowBlur = 0;
        ctx.globalAlpha = 1.0;

        // Selection ring — pulsing outline on the selected node (#10)
        if (selectedNodeIdRef.current === node.id) {
          const pulse = Math.sin(now / 400);
          ctx.beginPath();
          ctx.arc(nx, ny, r + 5 + pulse * 3, 0, 2 * Math.PI);
          ctx.strokeStyle = "#ffffff";
          ctx.lineWidth = 2;
          ctx.globalAlpha = 0.65 + pulse * 0.35;
          ctx.stroke();
          ctx.globalAlpha = 1.0;
        }
      }

      ctx.restore();
    }

    // Continuous render loop — keeps canvas live for pan/zoom after simulation cools
    let rafId = 0;
    function renderLoop() {
      ticked();
      rafId = requestAnimationFrame(renderLoop);
    }
    rafId = requestAnimationFrame(renderLoop);

    // Click/double-click: record pointer-down position, use 300 ms timer to
    // distinguish single (focus) from double (detail panel) on the same node.
    let pointerDownX = 0;
    let pointerDownY = 0;
    let singleClickTimer: ReturnType<typeof setTimeout> | null = null;

    function onPointerDownClick(e: PointerEvent) {
      pointerDownX = e.clientX;
      pointerDownY = e.clientY;
    }

    function hitNode(e: MouseEvent): typeof currentNodesRef.current[number] | null {
      const dx = e.clientX - pointerDownX;
      const dy = e.clientY - pointerDownY;
      if (Math.sqrt(dx * dx + dy * dy) > 5) return null; // drag, not click

      const rect = canvas!.getBoundingClientRect();
      const canvasX = e.clientX - rect.left;
      const canvasY = e.clientY - rect.top;
      const transform = panZoomRef.current;
      const simX = (canvasX - transform.x) / transform.scale;
      const simY = (canvasY - transform.y) / transform.scale;

      for (const node of currentNodesRef.current) {
        const nx = node.x ?? 0;
        const ny = node.y ?? 0;
        const r = getNodeRadius(node);
        if (Math.sqrt((simX - nx) ** 2 + (simY - ny) ** 2) <= r + 4) return node;
      }
      return null;
    }

    function onClick(e: MouseEvent) {
      const node = hitNode(e);
      if (!node) return;

      // Arm a 300 ms timer; if dblclick fires first, the timer is cancelled
      if (singleClickTimer !== null) clearTimeout(singleClickTimer);
      singleClickTimer = setTimeout(() => {
        singleClickTimer = null;
        selectedNodeIdRef.current = node.id;
        onNodeClickRef.current?.(node.id);
      }, 300);
    }

    function onDblClick(e: MouseEvent) {
      const node = hitNode(e);
      if (!node) return;

      // Cancel the pending single-click to prevent both firing
      if (singleClickTimer !== null) {
        clearTimeout(singleClickTimer);
        singleClickTimer = null;
      }
      selectedNodeIdRef.current = node.id;
      onNodeDoubleClickRef.current?.(node.id);
    }

    canvas.addEventListener("pointerdown", onPointerDownClick);
    canvas.addEventListener("click", onClick);
    canvas.addEventListener("dblclick", onDblClick);

    return () => {
      cancelAnimationFrame(rafId);
      simulation.stop();
      simulationRef.current = null;
      window.removeEventListener("resize", resize);
      canvas.removeEventListener("pointerdown", onPointerDownClick);
      canvas.removeEventListener("click", onClick);
      canvas.removeEventListener("dblclick", onDblClick);
      if (singleClickTimer !== null) clearTimeout(singleClickTimer);
    };
  }, [panZoomRef]); // mount-only behavior; panZoomRef is stable for component lifetime

  // Data effect: update nodes/links refs and reheat the simulation when batches or expansions change.
  // Does NOT rebuild the canvas or RAF loop, preventing node teleportation on each pulse.
  useEffect(() => {
    const simulation = simulationRef.current;
    if (!simulation) return;

    // Flatten batches into arrays, then merge expansion Post nodes/links
    const pulseNodes = Object.values(batches).flatMap((b) => b.nodes) as SimNode[];
    const pulseLinks = Object.values(batches).flatMap((b) => b.links) as SimLink[];

    // Expansion Post nodes are positioned near their parent so the sim doesn't teleport them.
    const postNodes: SimNode[] = expansionNodes.map((n) => {
      const existing = (simulationRef.current?.nodes() as SimNode[] | undefined)?.find((sn) => sn.id === n.id);
      if (existing) return existing; // preserve position across re-renders
      const parent = (simulationRef.current?.nodes() as SimNode[] | undefined)?.find((sn) => sn.id === expansionLinks.find((l) => l.targetId === n.id)?.sourceId);
      return { ...n, x: (parent?.x ?? 0) + (Math.random() - 0.5) * 60, y: (parent?.y ?? 0) + (Math.random() - 0.5) * 60 } as SimNode;
    });

    const newNodes = [...pulseNodes, ...postNodes];
    const newLinks = [...pulseLinks, ...expansionLinks as SimLink[]];

    // Ensure link source/target references point to the actual SimNode objects
    const nodeById = new Map(newNodes.map((n) => [n.id, n]));
    const validLinks = newLinks
      .map((l) => ({
        ...l,
        source: nodeById.get(l.sourceId as unknown as string),
        target: nodeById.get(l.targetId as unknown as string),
      }))
      .filter((l) => l.source && l.target) as typeof newLinks;

    currentNodesRef.current = newNodes;
    currentLinksRef.current = validLinks;

    // Detect evicted nodes and add them to the fade-out map
    const newNodeIds = new Set(newNodes.map((n) => n.id));
    for (const prev of prevNodesRef.current) {
      if (!newNodeIds.has(prev.id)) {
        fadeOutMapRef.current.set(prev.id, {
          x: prev.x ?? 0,
          y: prev.y ?? 0,
          label: prev.label,
          isAnchor: prev.type === "Anchor",
          opacity: 1,
          evictedAt: Date.now(),
        });
      }
    }
    prevNodesRef.current = newNodes;

    // Track first-seen timestamps for heat ring display (#2)
    const seenNow = Date.now();
    for (const node of newNodes) {
      if (!nodeFirstSeenRef.current.has(node.id)) nodeFirstSeenRef.current.set(node.id, seenNow);
    }
    for (const id of Array.from(nodeFirstSeenRef.current.keys())) {
      if (!newNodeIds.has(id)) nodeFirstSeenRef.current.delete(id);
    }
    // Count incoming links per target node for edge weight encoding (#3)
    const counts = new Map<string, number>();
    for (const link of validLinks) {
      const tgt = (link.target as unknown as SimNode)?.id;
      if (tgt) counts.set(tgt, (counts.get(tgt) ?? 0) + 1);
    }
    nodeLinkCountRef.current = counts;

    // D3 link force needs to be recreated when data structure changes
    simulation.force(
      "link",
      d3.forceLink<SimNode, SimLink>(validLinks)
        .id((d) => d.id)
        .distance((d) => 100 * (d.target as SimNode).elasticity) // closer the more elastic
    );
    simulation.nodes(newNodes);
    simulation.alpha(0.3).restart();
  }, [batches, expansionNodes, expansionLinks, panZoomRef]);

  return (
    <div
      ref={containerRef}
      style={{
        position: "absolute",
        inset: 0,
        background: "transparent",
      }}
    >
      <canvas
        ref={canvasRef}
        aria-label="Constellation canvas"
        data-layer="main"
        data-testid="constellation-canvas"
        data-pulse-count={lastPulseAt ?? 0}
        style={{
          width: "100%",
          height: "100%",
          display: "block",
          cursor: "grab",
        }}
      />
    </div>
  );
}
