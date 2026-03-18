// T099: Error boundary wraps critical views so a render crash never shows a blank page.
// React error boundaries must be class components.
import { Component, type ReactNode } from 'react';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  message: string;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, message: '' };
  }

  static getDerivedStateFromError(error: unknown): State {
    const message = error instanceof Error ? error.message : String(error);
    return { hasError: true, message };
  }

  componentDidCatch(error: unknown, info: { componentStack?: string }) {
    console.error('[ErrorBoundary]', error, info.componentStack);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div
          role="alert"
          style={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            height: '100vh',
            background: 'var(--colour-bg, #0a0e1a)',
            color: 'var(--colour-text-primary, #f9fafb)',
            fontFamily: 'var(--font-mono)',
            gap: 16,
            padding: 24,
            textAlign: 'center',
          }}
        >
          <span style={{ fontSize: 40 }}>⚠</span>
          <p style={{ fontSize: '1.25rem', fontWeight: 700, margin: 0, color: 'var(--colour-neon-cyan, #00f5ff)' }}>
            Something went wrong
          </p>
          <p style={{ fontSize: '0.875rem', color: 'var(--colour-text-secondary, #9ca3af)', margin: 0, maxWidth: 480 }}>
            {this.state.message || 'An unexpected error occurred in the constellation view.'}
          </p>
          <button
            type="button"
            onClick={() => this.setState({ hasError: false, message: '' })}
            style={{
              marginTop: 8,
              background: 'rgba(0, 245, 255, 0.15)',
              color: 'var(--colour-neon-cyan, #00f5ff)',
              border: '1px solid rgba(0, 245, 255, 0.5)',
              padding: '8px 20px',
              borderRadius: 6,
              fontSize: '0.875rem',
              cursor: 'pointer',
            }}
          >
            Try again
          </button>
        </div>
      );
    }

    return this.props.children;
  }
}
