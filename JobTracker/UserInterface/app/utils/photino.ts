declare global {
  interface External {
    receiveMessage: (callback: (response: any) => void) => void;
    sendMessage: (message: string) => void;
  }
}

// Map to track pending requests
const pendingRequests = new Map<
  string,
  { resolve: Function; reject: Function }
>();

// Registry for event handlers (backend -> frontend events)
const eventHandlers = new Map<string, Set<(data: any) => void>>();

// Check if we're in a browser environment
const isBrowser = typeof window !== "undefined";

// Single global receiveMessage handler - only initialize in browser
if (isBrowser && window.external?.receiveMessage) {
  window.external.receiveMessage((raw: any) => {
  try {
    const response = typeof raw === "string" ? JSON.parse(raw) : raw;

    // Handle backend-initiated events
    if (response.type === "event") {
      console.log("[Photino Event]", response.name, response.data);
      const handlers = eventHandlers.get(response.name);
      if (handlers) {
        handlers.forEach((handler) => handler(response.data));
      }
      return;
    }

    // Handle RPC responses
    const id = response?.id;
    if (id && pendingRequests.has(id)) {
      const { resolve, reject } = pendingRequests.get(id)!;
      pendingRequests.delete(id);

      if (response.success === false) {
        reject(new Error(response.error ?? "RPC error"));
      } else {
        resolve(response.data);
      }
    }
    } catch (err) {
      console.error("Failed to handle IPC message", err);
    }
  });
}

/**
 * Subscribe to backend events
 * @param eventName - The event name (e.g., "jobTracker:completed")
 * @param handler - Callback function when event is received
 * @returns Unsubscribe function
 */
export function onPhotinoEvent(
  eventName: string,
  handler: (data: any) => void
): () => void {
  if (!eventHandlers.has(eventName)) {
    eventHandlers.set(eventName, new Set());
  }
  eventHandlers.get(eventName)!.add(handler);

  // Return unsubscribe function
  return () => {
    eventHandlers.get(eventName)?.delete(handler);
  };
}

/**
 * Subscribe to a backend event once
 * @param eventName - The event name
 * @param handler - Callback function
 */
export function oncePhotinoEvent(
  eventName: string,
  handler: (data: any) => void
): void {
  const unsubscribe = onPhotinoEvent(eventName, (data) => {
    unsubscribe();
    handler(data);
  });
}

/**
 * Remove all handlers for an event
 * @param eventName - The event name
 */
export function offPhotinoEvent(eventName: string): void {
  eventHandlers.delete(eventName);
}

// Generate a random ID (UUID v4)
function generateId() {
  return crypto.randomUUID?.() ?? Math.random().toString(36).slice(2);
}

export function sendPhotinoMessage<T = any>(message: {
  command: string;
  payload?: any;
}): Promise<T> {
  if (!isBrowser) {
    return Promise.reject(new Error("Photino is only available in the browser"));
  }

  if (!window.external?.sendMessage) {
    return Promise.reject(new Error("window.external.sendMessage is not available"));
  }

  const id = generateId();
  return new Promise((resolve, reject) => {
    pendingRequests.set(id, { resolve, reject });

    // Add the ID to the message so backend can echo it
    const messageWithId = { ...message, id };
    window.external.sendMessage(JSON.stringify(messageWithId));

    console.log("Sent message:", messageWithId);
  });
}

export function sendPhotinoRequest<T = any>(
  command: string,
  payload?: any
): Promise<T> {
  return sendPhotinoMessage<T>({
    command,
    payload,
  });
}
