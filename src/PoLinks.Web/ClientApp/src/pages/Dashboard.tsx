// T039: Main dashboard gluing components
import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { usePulseState } from "../context/PulseStateContext";
import { useFocusMode } from "../features/focus-mode/useFocusMode";
import { FocusModeControls } from "../features/focus-mode/FocusModeControls";
import { ConstellationCanvas } from "../features/constellation/ConstellationCanvas";
import { CountdownProgressBar } from "../features/constellation/CountdownProgressBar";
import { SimulationBanner } from "../features/simulation/SimulationBanner";
import { InsightPanel } from "../features/insight-panel/InsightPanel";
import { useInsightPanelState } from "../features/insight-panel/useInsightPanelState";
import { useExpansionGraph } from "../features/constellation/useExpansionGraph";
import { ExportSnapshotButton } from "../features/snapshot/ExportSnapshotButton";
import { formatApiError } from "../utils/errorMessages";
import styles from './Dashboard.module.css';

interface NodeSearchOverlayProps {
  nodes: Array<{ id: string; label: string; type: string }>;
  query: string;
  onQueryChange: (q: string) => void;
  onSelect: (nodeId: string) => void;
  onClose: () => void;
}

function filterNodes(nodes: NodeSearchOverlayProps["nodes"], query: string) {
  const value = query.trim().toLowerCase();
  if (!value) return nodes;

  return nodes.filter((n) =>
    n.label.toLowerCase().includes(value) ||
    n.id.toLowerCase().includes(value)
  );
}

function NodeSearchOverlay({ nodes, query, onQueryChange, onSelect, onClose }: NodeSearchOverlayProps) {
  const filtered = filterNodes(nodes, query);

  const onResultKeyDown = (event: React.KeyboardEvent<HTMLLIElement>, nodeId: string) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      onSelect(nodeId);
    }
  };

  return (
    <div className={styles.searchBackdrop} onClick={onClose} role="dialog" aria-modal="true" aria-label="Node search">
      <div className={styles.searchModal} onClick={(e) => e.stopPropagation()}>
        <input
          autoFocus
          type="search"
          className={styles.searchInput}
          placeholder="Search nodes…"
          value={query}
          onChange={(e) => onQueryChange(e.target.value)}
          aria-label="Search nodes"
        />
        {filtered.length === 0 ? (
          <p className={styles.searchEmpty}>{query ? `No nodes match “${query}”` : 'Start typing to search…'}</p>
        ) : (
          <ul className={styles.searchResults} role="listbox" aria-label="Node search results">
            {filtered.slice(0, 12).map((node) => (
              <li
                key={node.id}
                className={styles.searchResultItem}
                role="option"
                tabIndex={0}
                onClick={() => onSelect(node.id)}
                onKeyDown={(e) => onResultKeyDown(e, node.id)}
              >
                <span className={styles.searchResultLabel}>{node.label}</span>
                <span className={styles.searchResultType}>{node.type}</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

function DashboardInner() {
  const { insight, isLoading, selectedNodeId, selectNode, error: insightError } = useInsightPanelState();
  const { batches, isSimulated } = usePulseState();
  const { isFocused, focusedAnchorId, enterFocusMode, exitFocusMode, error: focusError } = useFocusMode();
  const { expansionNodes, expansionLinks, expandNode } = useExpansionGraph(5);
  const navigate = useNavigate();

  const allNodes = [
    ...Object.values(batches).flatMap((b) => b.nodes),
    ...expansionNodes,
  ];
  const hasTopicNodes = allNodes.some((n) => n.type === 'Topic');
  const [isSearchOpen, setIsSearchOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [overlayDismissed, setOverlayDismissed] = useState(false);
  const shouldShowOnboarding = !overlayDismissed && !hasTopicNodes;

  // Derive node type for InsightPanel accent color — check pulse nodes first, then expansion nodes
  const selectedNodeType = selectedNodeId
    ? (allNodes.find((n) => n.id === selectedNodeId)?.type ?? null)
    : null;

  const handleNodeClick = useCallback((nodeId: string) => {
    // Single click: focus on clicked node (enter Focus Mode for Anchors; visual select for Topics)
    const node = allNodes.find((n) => n.id === nodeId);
    if (node?.type === 'Anchor') {
      enterFocusMode(nodeId).catch(() => undefined);
    } else {
      // For Topic/Post nodes, single click still selects so the ring renders
      selectNode(nodeId);
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectNode, batches, expansionNodes, enterFocusMode]);

  const handleNodeDoubleClick = useCallback((nodeId: string) => {
    // Double click: always open insight panel AND trigger expansion for Topic/Post nodes
    selectNode(nodeId);

    const node = allNodes.find((n) => n.id === nodeId);
    if (node && (node.type === 'Topic' || node.type === 'Post')) {
      expandNode(node);
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectNode, expandNode, batches, expansionNodes]);

  const handleClose = useCallback(() => {
    selectNode(null);
  }, [selectNode]);

  const handleExitFocus = useCallback(() => {
    void exitFocusMode();
  }, [exitFocusMode]);

  // Keyboard shortcut: Shift+D for diagnostics
  useEffect(() => {
    const handleKeyPress = (event: KeyboardEvent) => {
      if (event.shiftKey && event.key === 'D') {
        event.preventDefault();
        navigate('/diagnostic');
      }
    };

    window.addEventListener('keydown', handleKeyPress);
    return () => window.removeEventListener('keydown', handleKeyPress);
  }, [navigate]);

  // Keyboard shortcut: Cmd/Ctrl+K opens node search; Escape closes it
  useEffect(() => {
    const handleSearch = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        setIsSearchOpen(true);
      }
      if (e.key === 'Escape') setIsSearchOpen(false);
    };
    window.addEventListener('keydown', handleSearch);
    return () => window.removeEventListener('keydown', handleSearch);
  }, []);

  return (
    <div className={styles.root} data-banner-active={isSimulated || undefined}>
      <FocusModeControls
        anchorName={focusedAnchorId ?? 'Unknown'}
        isActive={isFocused}
        onExit={handleExitFocus}
      >
        <SimulationBanner />
        {focusError && (
          <div role="alert" aria-live="assertive" className={styles.focusErrorBanner}>
            Focus mode: {formatApiError(focusError)}
          </div>
        )}
        <ExportSnapshotButton />
        <ConstellationCanvas
          onNodeClick={handleNodeClick}
          onNodeDoubleClick={handleNodeDoubleClick}
          expansionNodes={expansionNodes}
          expansionLinks={expansionLinks}
        />
        <CountdownProgressBar />
        <InsightPanel
          insight={insight}
          isLoading={isLoading}
          isOpen={selectedNodeId !== null}
          error={insightError}
          nodeType={selectedNodeType}
          onClose={handleClose}
        />
        {isFocused && focusedAnchorId && (
          <div className={styles.focusWatermark} aria-hidden="true">{focusedAnchorId}</div>
        )}
        {shouldShowOnboarding && (
          <div className={styles.onboardingOverlay}>
            <p className={styles.onboardingHint}>
              Click an anchor to focus · Double-click any node for details · Drag to pan · Scroll to zoom
            </p>
            <button className={styles.onboardingDismiss} onClick={() => setOverlayDismissed(true)}>Got it</button>
          </div>
        )}
      </FocusModeControls>
      {isSearchOpen && (
        <NodeSearchOverlay
          nodes={allNodes}
          query={searchQuery}
          onQueryChange={setSearchQuery}
          onSelect={(nodeId) => { setIsSearchOpen(false); setSearchQuery(''); handleNodeDoubleClick(nodeId); }}
          onClose={() => { setIsSearchOpen(false); setSearchQuery(''); }}
        />
      )}
    </div>
  );
}

export default function Dashboard() {
  return <DashboardInner />;
}