export type KeywordScope = 'Both' | 'TitleOnly' | 'DescriptionOnly'

export interface KeywordRule {
  Keyword: string
  Scope: KeywordScope
}
