/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { ExtendedPosting } from "./extended-posting";

export interface GetJobsResponse {
    Postings: ExtendedPosting[];
    Page: number;
    PageSize: number;
    TotalResults: number;
    TotalPages: number;
    HasPreviousPage: boolean;
    HasNextPage: boolean;
}
