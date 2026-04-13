/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Tag } from '../tag/tag';
import { KeywordRule } from './keyword-rule';

export interface Settings {
    Id: number;
    DiscordWebhookUrl: string;
    DiscordNotificationsEnabled: boolean;
    GenerateEmbeddings: boolean;
    AppVersion: string;
    LastUpdatedAt: Date;
    FirstStart: boolean | null;
    UserCV: string | null;
    SelectedTags: Tag[] | null;
    YearsOfExperience: number | null;
    BlockedKeywords: KeywordRule[] | null;
    MatchedKeywords: KeywordRule[] | null;
    AlertOnAllMatchingJobs: boolean | null;
    AlertOnHardMatchingJobs: boolean | null;
    Location: string | null;
    MaxJobAgeDays: number | null;
}
