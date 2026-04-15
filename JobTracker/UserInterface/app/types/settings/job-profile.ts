import { Tag } from '../tag/tag'
import { KeywordRule } from './keyword-rule'
import { RejectedTagRule } from './rejected-tag-rule'

export interface JobProfile {
  Id: number
  Name: string
  UserId: string | null
  CreatedAt: Date
  UserEmbedding: string | null
  UserCV: string | null
  SelectedTags: Tag[] | null
  YearsOfExperience: number | null
  BlockedKeywords: KeywordRule[] | null
  MatchedKeywords: KeywordRule[] | null
  AlertOnAllMatchingJobs: boolean | null
  AlertOnHardMatchingJobs: boolean | null
  Locations: string[] | null
  MaxJobAgeDays: number | null
  BlockedLocations: string[] | null
  RejectedSeniorityLevels: string[] | null
  RejectedTechKeywords: RejectedTagRule[] | null
}
