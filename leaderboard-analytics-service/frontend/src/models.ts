export type AssistantRole = 'user' | 'assistant'

export interface AssistantMessage {
  id: string
  role: AssistantRole
  content: string
}

export interface AssistantRouteContext {
  routeName: 'leaderboard-analytics-home'
}

export interface AssistantMessageRequest {
  message: string
  history: Array<Pick<AssistantMessage, 'role' | 'content'>>
  context: AssistantRouteContext
}

export interface HostContext {
  readonly user: { id: number; name: string } | null
  signIn(returnUrl?: string): void
  signOut(): Promise<void>
  navigate(path: string): Promise<void>
}
