export type AppIconName =
  | 'analytics'
  | 'arrow-left'
  | 'arrow-right'
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
  href?: string
  icon: AppIconName
  current?: boolean
  disabled?: boolean
}

export interface UtilityNavigationItem {
  label: string
  icon: AppIconName
  disabled: true
}

const loopbackHost = 'http://127.0.0.1'
const sharedFrontend = `${loopbackHost}:3000`
export const sharedHomeHref = `${sharedFrontend}/`

export const serviceNavigation: readonly ServiceNavigationItem[] = [
  { label: 'Home', href: sharedHomeHref, icon: 'home' },
  { label: 'Mini Games', href: `${loopbackHost}:3001/`, icon: 'games' },
  {
    label: 'Discussion Forum',
    href: `${loopbackHost}:3002/`,
    icon: 'discussion',
  },
  {
    label: 'Quizzes & Courses',
    href: `${sharedFrontend}/quizzes-and-courses/`,
    icon: 'courses',
    current: true,
  },
  {
    label: 'Quests & Achievements',
    href: `${loopbackHost}:3004/`,
    icon: 'quests',
  },
  {
    label: 'Leaderboard & Analytics',
    icon: 'analytics',
    disabled: true,
  },
]

export const utilityNavigation: readonly UtilityNavigationItem[] = [
  { label: 'Profile', icon: 'profile', disabled: true },
  { label: 'Logout', icon: 'logout', disabled: true },
]
