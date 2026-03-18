// T034: Client SignalR context with fallback to mock data (FR-001).
// Connects to /hubs/pulse and listens for 'ReceivePulseBatch' events.
import { useEffect, useRef, useState, type ReactNode } from "react";
import * as signalR from "@microsoft/signalr";
import type { PulseBatch } from "../types/nexus";
import { isStandaloneClientMode, resolveRuntimeUrl } from "../runtime";
import { PulseContext, type PulseState } from "./PulseStateContext";

type PulseStateWithoutReconnect = Omit<PulseState, "reconnect">;

async function createSimulatedPulseState(error: Error | null = null): Promise<PulseStateWithoutReconnect> {
  const { generateAllAnchorBatches } = await import("../features/simulation/MockDataService");
  const mocks = generateAllAnchorBatches();
  const batches: Record<string, PulseBatch> = {};

  for (const batch of mocks) {
    batches[batch.anchorId] = batch;
  }

  return {
    batches,
    lastPulseAt: Date.now(),
    isConnected: false,
    isConnecting: false,
    isSimulated: true,
    error,
  };
}

export function PulseProvider({ children }: { children: ReactNode }) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [state, setState] = useState<PulseStateWithoutReconnect>({
    batches: {},
    lastPulseAt: null,
    isConnected: false,
    isConnecting: false,
    isSimulated: false,
    error: null,
  });

  useEffect(() => {
    if (isStandaloneClientMode()) {
      void createSimulatedPulseState().then(setState);
      return undefined;
    }

    // Determine the base URL. In development, the Vite proxy forwards /hubs to the ASP.NET server.
    const hubUrl = resolveRuntimeUrl("/hubs/pulse");

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      // JSON protocol inherits from server-side camelCase configuration (T017)
      // matching TypeScript interface contracts in nexus.ts
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (r) => (r.previousRetryCount < 5 ? 2000 : 5000),
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connectionRef.current = connection;

    connection.on("ReceivePulseBatch", (batch: PulseBatch) => {
      setState((prev) => ({
        ...prev,
        batches: { ...prev.batches, [batch.anchorId]: batch },
        lastPulseAt: Date.now(),
        isSimulated: batch.isSimulated,
      }));
    });

    let isMounted = true;

    async function startConnection() {
      setState((s) => ({ ...s, isConnecting: true }));
      try {
        await connection.start();
        if (isMounted) setState((s) => ({ ...s, isConnected: true, isConnecting: false, error: null }));
      } catch (err) {
        // Fallback to Simulation Mode if connection fails immediately
        if (isMounted) {
          const simState = await createSimulatedPulseState(err instanceof Error ? err : new Error(String(err)));
          setState(simState);
        }
      }
    }

    connection.onreconnecting(() => {
      setState((s) => ({ ...s, isConnected: false, isConnecting: true }));
    });

    connection.onreconnected(() => {
      setState((s) => ({ ...s, isConnected: true, isConnecting: false, isSimulated: false, error: null }));
    });

    startConnection();

    return () => {
      isMounted = false;
      connectionRef.current = null;
      connection.stop();
    };
  }, []);

  const reconnect = () => {
    const conn = connectionRef.current;
    if (!conn || isStandaloneClientMode()) return;
    setState((s) => ({ ...s, isConnecting: true, error: null }));
    void conn.start().then(() => {
      setState((s) => ({ ...s, isConnected: true, isConnecting: false, isSimulated: false, error: null }));
    }).catch((err: unknown) => {
      setState((s) => ({ ...s, isConnecting: false, error: err instanceof Error ? err : new Error(String(err)) }));
    });
  };

  return <PulseContext.Provider value={{ ...state, reconnect }}>{children}</PulseContext.Provider>;
}
