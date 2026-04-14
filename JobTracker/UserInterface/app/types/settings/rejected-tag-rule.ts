import { KeywordScope } from './keyword-rule'

export interface RejectedTagRule {
  TagId: number
  Scope: KeywordScope
}

export interface RejectedTagEntry {
  TagId: number
  TagName: string
  TagColor: string
  Scope: KeywordScope
}
