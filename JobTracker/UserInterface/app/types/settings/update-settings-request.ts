export interface UpdateSettingsRequest {
  DiscordWebhookUrl: string | null
  DiscordNotificationsEnabled: boolean | null
  GenerateEmbeddings: boolean | null
  FirstStart: boolean | null
}
