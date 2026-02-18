/**
 * System event types that can be received from the Photino backend
 */
export type SystemEventType = 
  | 'jobs.added'
  | 'embeddings.generating'
  | 'embeddings.completed'
  | 'embeddings.error'
  | 'jobs.tracking.started'
  | 'jobs.tracking.completed'
  | 'jobs.tracking.error';

/**
 * System event severity level
 */
export type SystemEventSeverity = 'info' | 'success' | 'warning' | 'error';

/**
 * A system event received from the Photino backend
 */
export interface SystemEvent {
  id: string;
  type: SystemEventType;
  severity: SystemEventSeverity;
  title: string;
  message: string;
  timestamp: Date;
  data?: Record<string, unknown>;
}

/**
 * Payload for jobs.added event
 */
export interface JobsAddedPayload {
  count: number;
  source?: string;
}

/**
 * Payload for embeddings events
 */
export interface EmbeddingsPayload {
  progress?: number;
  total?: number;
  processed?: number;
  error?: string;
}
