import { Posting } from "../jobs/posting";

export interface JobApplication {
  Id: number,
  JobId: number,
  Posting: Posting,
  CoverLetter: string,
  AppliedAt: Date,
  LastStatusChangeAt: Date,
  Status: ApplicationStatus
  StatusHistory: ApplicationStatusHistory[]
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

export interface ApplicationStatusHistory {
  Id: number,
  JobApplicationId: number,
  JobApplication: JobApplication,
  ApplicationStatus: ApplicationStatus,
  ChangedAt: Date,
  Note?: string
}