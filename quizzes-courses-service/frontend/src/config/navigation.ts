export type AppIconName =
  | 'analytics'
  | 'arrow-left'
  | 'arrow-right'
  | 'close'
  | 'courses'
  | 'discussion'
  | 'flashcards'
  | 'games'
  | 'home'
  | 'logout'
  | 'menu'
  | 'profile'
  | 'quizzes'
  | 'quests'

export interface ServiceNavigationItem {
  label: string
  href?: string
  icon: AppIconName
  current?: boolean
  disabled?: boolean
}

const sharedFrontend = `${window.location.protocol}//${window.location.hostname}:3000`
export const sharedHomeHref = `${sharedFrontend}/`

export const serviceNavigation: readonly ServiceNavigationItem[] = [
  { label: 'Home', href: sharedHomeHref, icon: 'home' },
  { label: 'Mini Games', href: `${sharedFrontend}/mini-games/`, icon: 'games' },
  {
    label: 'Discussion Forum',
    href: `${sharedFrontend}/chat-discussion/`,
    icon: 'discussion',
  },
  {
    label: 'Quizzes & Courses',
    href: `${sharedFrontend}/quizzes-and-courses/`,
    icon: 'courses',
    current: true,
  },
  {
    label: 'Achievements & Notifications',
    href: `${sharedFrontend}/quests-and-achievements/`,
    icon: 'quests',
  },
  {
    label: 'Leaderboard & Analytics',
    href: `${sharedFrontend}/analytics/`,
    icon: 'analytics',
  },
]
