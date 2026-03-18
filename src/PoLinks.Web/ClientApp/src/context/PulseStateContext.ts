import { createContext, useContext } from "react";
import type { PulseBatch } from "../types/nexus";

export interface PulseState {
  batches: Record<string, PulseBatch>;
  lastPulseAt: number | null;
  isConnected: boolean;
  isConnecting: boolean;
  isSimulated: boolean;
  error: Error | null;
  reconnect: () => void;
}

export const PulseContext = createContext<PulseState | null>(null);

export function usePulseState() {
  const context = useContext(PulseContext);
  if (!context) throw new Error("usePulseState must be used within a PulseProvider");
  return context;
}