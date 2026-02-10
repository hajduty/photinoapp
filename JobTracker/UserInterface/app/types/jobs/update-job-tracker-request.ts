/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Tag } from "../tag/tag";

export interface UpdateJobTrackerRequest {
    TrackerId: number;
    Keyword: string;
    Source: string;
    Location: string;
    IsActive: boolean;
    Tags: Tag[];
    LastCheckedAt: Date;
    CheckIntervalHours: number;
}
