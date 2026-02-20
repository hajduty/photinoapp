import { Posting } from "../jobs/posting";

export interface JobApplication {
  Id: number,
  JobId: number,
  Posting: Posting,
  CoverLetter: string,
  AppliedAt: Date,
  LastStatusChangeAt: Date,
  Status: ApplicationStatus
}

export enum ApplicationStatus {
  Pending,
  Submitted,
  Interview,
  Offer,
  Accepted,
  Rejected,
  Ghosted
}