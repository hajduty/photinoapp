export interface Posting {
  Id: number
  Title: string
  Description: string
  DescriptionFormatted: string
  Company: string
  City: string | null
  County: string | null
  Country: string | null
  Longitude: number | null
  Latitude: number | null
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
