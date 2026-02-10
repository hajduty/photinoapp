import { Posting } from "./posting";

export interface JobSearchResponse {
  TotalCount: number;
  Jobs: Posting[];
  // Add these fields to match backend response
  Page?: number;
  TotalPages?: number;
  HasPreviousPage?: boolean;
  HasNextPage?: boolean;
}
