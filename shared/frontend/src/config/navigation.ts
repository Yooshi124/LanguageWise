export type AppIconName =
  | 'analytics'
  | 'close'
  | 'courses'
  | 'discussion'
  | 'games'
  | 'home'
  | 'logout'
  | 'menu'
  | 'profile'
  | 'quests'

export interface ServiceNavigationItem {
  label: string
  href: string
  icon: AppIconName
  current?: boolean
}

export const serviceNavigation: readonly ServiceNavigationItem[] = [
  { label: 'Home', href: '/', icon: 'home', current: true },
  { label: 'Mini Games', href: '/mini-games/', icon: 'games' },
  { label: 'Discussion Forum', href: '/chat-discussion/', icon: 'discussion' },
  { label: 'Quizzes & Courses', href: '/quizzes-and-courses/', icon: 'courses' },
  {
    label: 'Quests & Achievements',
    href: '/quests-and-achievements/',
    icon: 'quests',
  },
  { label: 'Leaderboard & Analytics', href: '/analytics/', icon: 'analytics' },
]
