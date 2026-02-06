declare global {
  interface External {
    receiveMessage: (callback: (response: any) => void) => void;
    sendMessage: (message: string) => void;
  }
}

export function sendPhotinoMessage<T = any>(message: {
  command: string;
  payload?: any;
}): Promise<T> {
  return new Promise((resolve, reject) => {
    window.external.receiveMessage((raw: any) => {
      try {
        const response =
          typeof raw === "string" ? JSON.parse(raw) : raw;

        if (response?.success === false) {
          reject(new Error(response.error ?? "RPC error"));
          return;
        }

        resolve(response.data as T);
      } catch (err) {
        reject(err);
      }
    });

    window.external.sendMessage(JSON.stringify(message));
  });
}

export function sendPhotinoRequest<T = any>(
  command: string,
  payload?: any
): Promise<T> {
  return sendPhotinoMessage<T>({
    command,
    payload
  });
}
