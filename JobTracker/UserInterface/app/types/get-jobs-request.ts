/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

export interface GetJobsRequest {
    Keyword: string;
    Page: number;
    PageSize: number;
    ActiveTagIds: number[];
    TimeSinceUpload: Date | null;
}
