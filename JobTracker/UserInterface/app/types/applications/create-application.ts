import { JobApplication } from "./jobApplication";

export interface CreateApplicationRequest {
    JobId: number,
    CoverLetter: string,
}

export interface CreateApplicationResponse
{
    JobApplication: JobApplication
}