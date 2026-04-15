import { KeywordRule } from './keyword-rule'

export interface UpdatePreferencesRequest {
    UserCV: string | null;
    SelectedTagIds: number[] | null;
    YearsOfExperience: number | null;
    BlockedKeywords: KeywordRule[] | null;
    MatchedKeywords: KeywordRule[] | null;
    AlertOnAllMatchingJobs: boolean | null;
    AlertOnHardMatchingJobs: boolean | null;
    Locations: string[] | null;
    MaxJobAgeDays: number | null;
}
