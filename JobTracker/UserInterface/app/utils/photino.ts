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

// Single global receiveMessage handler
window.external.receiveMessage((raw: any) => {
  try {
    const response = typeof raw === "string" ? JSON.parse(raw) : raw;
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

// Generate a random ID (UUID v4)
function generateId() {
  return crypto.randomUUID?.() ?? Math.random().toString(36).slice(2);
}

export function sendPhotinoMessage<T = any>(message: {
  command: string;
  payload?: any;
}): Promise<T> {
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
