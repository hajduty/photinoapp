import { Settings } from './settings'
import { JobProfile } from './job-profile'

export interface GetSettingsResponse {
  Settings: Settings
  ActiveProfile: JobProfile | null
  Profiles: JobProfile[]
}
