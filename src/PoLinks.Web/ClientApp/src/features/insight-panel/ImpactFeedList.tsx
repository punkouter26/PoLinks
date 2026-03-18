// T046: Impact-sorted post list with sentiment colour chips (US2, AC-3, AC-4).
// Posts arrive already sorted descending by impactScore from the server.
import { useState, type CSSProperties } from "react";
import type { InsightPost } from "../../types/nexus";
import styles from "./ImpactFeedList.module.css";

interface ImpactFeedListProps {
  posts: InsightPost[];
}

/** Returns a short relative-time string, e.g. "2m ago", "1h ago", "just now". */
function relativeTime(isoString: string): string {
  const diff = Date.now() - new Date(isoString).getTime();
  if (diff < 60_000)  return "just now";
  if (diff < 3600_000) return `${Math.floor(diff / 60_000)}m ago`;
  if (diff < 86400_000) return `${Math.floor(diff / 3600_000)}h ago`;
  return `${Math.floor(diff / 86400_000)}d ago`;
}

function CopyButton({ text }: { text: string }) {
  const [copied, setCopied] = useState(false);
  const handleCopy = () => {
    void navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
  };
  return (
    <button onClick={handleCopy} className={styles.copyButton} aria-label="Copy post text" title="Copy to clipboard">
      {copied ? '\u2713' : '\u2398'}
    </button>
  );
}

export function ImpactFeedList({ posts }: ImpactFeedListProps) {
  if (posts.length === 0) {
    return <p className={styles.emptyText}>No recent posts for this node.</p>;
  }

  return (
    <ol aria-label="Impact feed" className={styles.list}>
      {posts.map((post, index) => (
        <li
          key={post.postUri}
          data-sentiment={post.sentiment}
          className={styles.item}
          style={{ borderLeft: `3px solid ${post.sentimentColour}`, '--item-index': index } as CSSProperties}
        >
          <div className={styles.itemHeader}>
            <span className={styles.authorDid} title={post.authorDid}>
              {post.authorDid.length > 20 ? `…${post.authorDid.slice(-18)}` : post.authorDid}
            </span>

            <CopyButton text={post.text} />
            <div className={styles.badges}>
              {post.createdAt && (
                <span
                  aria-label={`Posted ${new Date(post.createdAt).toLocaleString()}`}
                  className={styles.timeBadge}
                  title={new Date(post.createdAt).toLocaleString()}
                >
                  {relativeTime(post.createdAt)}
                </span>
              )}

              <span
                aria-label={`Impact score ${post.impactScore.toFixed(2)}`}
                className={styles.impactBadge}
              >
                ↑{post.impactScore.toFixed(2)}
              </span>

              <span
                aria-label={`Sentiment: ${post.sentiment}`}
                className={styles.sentimentChip}
                style={{
                  color:      post.sentimentColour,
                  background: `${post.sentimentColour}22`,
                  border:     `1px solid ${post.sentimentColour}55`,
                }}
              >
                {post.sentiment}
              </span>
            </div>
          </div>

          <p className={styles.postText}>{post.text}</p>
        </li>
      ))}
    </ol>
  );
}
