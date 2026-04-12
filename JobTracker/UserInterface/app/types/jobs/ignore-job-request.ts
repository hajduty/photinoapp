export type IgnoreReason = 'title' | 'location' | 'requirements' | 'experience' | 'tags';

export interface IgnoreJobRequest {
  JobId: number;
  Reason?: IgnoreReason;
  RejectedTags?: number[];
}
