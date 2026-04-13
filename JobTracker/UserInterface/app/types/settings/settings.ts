export interface Settings {
  Id: number
  DiscordWebhookUrl: string
  DiscordNotificationsEnabled: boolean
  GenerateEmbeddings: boolean
  AppVersion: string
  LastUpdatedAt: Date
  FirstStart: boolean | null
  ActiveProfileId: number | null
}
