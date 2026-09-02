export interface Preferences {
  email: string
  notifyAll: boolean
  notifyPostEngagement: boolean
  notifyCourseCompletion: boolean
  notifyQuizResults: boolean
  notifyStreaks: boolean
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

export interface HostContext {
  readonly user: { id: number; name: string } | null
  signIn(returnUrl?: string): void
  signOut(): Promise<void>
  navigate(path: string): Promise<void>
}