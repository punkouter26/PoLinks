// T045: Semantic roots breadcrumb component (US2, AC-2).
// Shows the ancestry chain as a horizontal breadcrumb trail separated by chevrons.
interface SemanticRootsBreadcrumbsProps {
  roots: string[];
}

export function SemanticRootsBreadcrumbs({ roots }: SemanticRootsBreadcrumbsProps) {
  if (!roots?.length) return null;
  return (
    <nav aria-label="Semantic roots breadcrumbs">
      <ol
        style={{
          display:        "flex",
          flexWrap:       "wrap",
          alignItems:     "center",
          gap:            "4px",
          padding:        0,
          margin:         0,
          listStyle:      "none",
          fontSize:       "12px",
          fontFamily:     'var(--font-mono, "JetBrains Mono", monospace)',
          color:          "var(--colour-text-secondary, #9ca3af)",
          overflowX:      "auto",
        }}
      >
        {roots.map((root, idx) => (
          <li
            key={`${root}-${idx}`}
            style={{ display: "flex", alignItems: "center", gap: "4px" }}
          >
            {idx > 0 && (
              <span aria-hidden="true" style={{ color: "var(--colour-neon-violet, #7c3aed)" }}>
                ›
              </span>
            )}
            <span
              style={{
                color:          idx === 0
                  ? "var(--colour-neon-cyan, #00f5ff)"
                  : idx === roots.length - 1
                    ? "var(--colour-text-primary, #f9fafb)"
                    : "var(--colour-text-secondary, #9ca3af)",
                fontWeight:     idx === roots.length - 1 ? 600 : 400,
                whiteSpace:     "nowrap",
                maxWidth:       "120px",
                overflow:       "hidden",
                textOverflow:   "ellipsis",
              }}
              title={root}
            >
              {root}
            </span>
          </li>
        ))}
      </ol>
    </nav>
  );
}
