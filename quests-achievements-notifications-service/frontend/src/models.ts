export interface Preferences {
  email: string
  notifyAll: boolean
  notifyCommunityContribution: boolean
  notifyPostEngagement: boolean
  notifyLessonCompletion: boolean
  notifyCourseCompletion: boolean
  notifyQuizResult: boolean
  notifyMinigameWin: boolean
  notifyLoginStreak: boolean
  notifyAchievements: boolean
}

export interface Achievement {
  achievementId: number
  name: string
  image: string
  progress: number
  progressNeeded: number
}

export interface Notification {
  notificationId: number
  trigger: string
  time: string
  emailSubject: string
  emailBody: string
}

export interface Profile {
  username: string
  preferences: Preferences
  achievements: Achievement[]
  notifications: Notification[]
}

export type AssistantRole = 'user' | 'assistant'

export interface AssistantMessage {
  id: string
  role: AssistantRole
  content: string
}

export interface AssistantRouteContext {
  routeName: 'quests-achievements-home'
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