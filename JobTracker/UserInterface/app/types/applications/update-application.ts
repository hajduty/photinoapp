import { ApplicationStatus, JobApplication } from "./jobApplication";

export interface UpdateApplicationRequest {
  Id: number,
  ApplicationStatus: ApplicationStatus
}

export interface UpdateApplicationResponse {
  JobApplication: JobApplication
}