import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import { DiagnosticPage } from './features/diagnostic/DiagnosticPage';
import { ErrorBoundary } from './components/ErrorBoundary';
import { PulseProvider } from './context/PulseContext';
import { usePulseState } from './context/PulseStateContext';
import navStyles from './components/NavBar.module.css';
import './styles/tokens.css';
import './index.css';

function StatusDot() {
  const { isConnected, isConnecting, isSimulated, reconnect } = usePulseState();
  const status = isConnecting ? 'connecting' : (isConnected && !isSimulated) ? 'live' : 'simulation';
  const labels: Record<string, string> = { live: 'Live', connecting: 'Connecting', simulation: 'Simulation' };

  if (status !== 'live') {
    return (
      <button
        aria-label={`Connection status: ${labels[status]} — click to reconnect`}
        title={`${labels[status]} — click to reconnect`}
        className={navStyles.reconnectBtn}
        onClick={reconnect}
      >
        <span data-status={status} className={navStyles.statusDot} aria-hidden="true" />
        {labels[status]}
      </button>
    );
  }

  return (
    <span
      aria-label={`Connection status: ${labels[status]}`}
      title={labels[status]}
      data-status={status}
      className={navStyles.statusDot}
    />
  );
}

function NavBar() {
  const linkClass = ({ isActive }: { isActive: boolean }) =>
    isActive ? `${navStyles.link} ${navStyles.linkActive}` : navStyles.link;

  return (
    <nav aria-label="Main navigation" className={navStyles.nav}>
      <div className={navStyles.brand} aria-hidden="true">
        <span className={navStyles.brandDot} />
        <span className={navStyles.brandName}>PoLinks</span>
      </div>
      <div className={navStyles.links}>
        <NavLink to="/" end className={linkClass}>Constellation</NavLink>
        <NavLink to="/diagnostic" className={linkClass}>
          Diagnostic
          <span className={navStyles.kbdBadge} aria-hidden="true">⇧D</span>
        </NavLink>
        <StatusDot />
      </div>
    </nav>
  );
}

function App() {
  return (
    <BrowserRouter>
      <PulseProvider>
        <NavBar />
        <Routes>
          <Route path="/" element={<ErrorBoundary><Dashboard /></ErrorBoundary>} />
          <Route path="/diagnostic" element={<DiagnosticPage />} />
          <Route path="/diag" element={<DiagnosticPage />} />
        </Routes>
      </PulseProvider>
    </BrowserRouter>
  );
}

export default App;
