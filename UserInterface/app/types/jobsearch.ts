import { Posting } from "./posting";

export interface JobSearchResponse {
    TotalCount: number;
    Jobs: Posting[];
}