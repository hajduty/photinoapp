/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

export interface UpdatePreferencesRequest {
    UserCV: string | null;
    SelectedTagIds: number[] | null;
    YearsOfExperience: number | null;
    BlockedKeywords: string[] | null;
    MatchedKeywords: string[] | null;
    AlertOnAllMatchingJobs: boolean | null;
    AlertOnHardMatchingJobs: boolean | null;
    Location: string | null;
    MaxJobAgeDays: number | null;
}