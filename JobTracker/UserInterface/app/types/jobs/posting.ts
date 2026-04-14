export interface Posting {
  Id: number
  Title: string
  Description: string
  DescriptionFormatted: string
  Company: string
  Location: string
  PostedDate: Date
  Url: string
  OriginUrl: string
  CompanyImage: string
  CreatedAt: Date
  LastApplicationDate: Date
  Source: string | null
  Bookmarked: boolean | null
  YearsOfExperience: number | null
  Alerted: boolean | null
}
