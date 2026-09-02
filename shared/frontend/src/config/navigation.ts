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
  key: string
  label: string
  href: string
  icon: AppIconName
  description?: string
  color?: string
  federation?: {
    basePath: string
    remoteEntryPath: string
    requiresAuth: boolean
  }
}

export const featureServices: readonly ServiceNavigationItem[] = [
  {
    key: 'mini-games',
    label: 'Mini Games',
    description: 'Practise vocabulary through matching, word search, and quick language activities.',
    href: '/mini-games/',
    icon: 'games',
    color: 'deep-purple',
    federation: {
      basePath: '/mini-games',
      remoteEntryPath: '/remotes/mini-games/remoteEntry.js',
      requiresAuth: true,
    },
  },
  {
    key: 'discussion-forum',
    label: 'Discussion Forum',
    description: 'Share progress, create posts, and learn alongside the LanguageWise community.',
    href: '/chat-discussion/',
    icon: 'discussion',
    color: 'blue',
    federation: {
      basePath: '/chat-discussion',
      remoteEntryPath: '/remotes/chat-discussion/remoteEntry.js',
      requiresAuth: true,
    },
  },
  {
    key: 'quizzes-courses',
    label: 'Quizzes & Courses',
    description: 'Follow structured lessons, test your knowledge, and revise with flashcards.',
    href: '/quizzes-and-courses/',
    icon: 'courses',
    color: 'primary',
    federation: {
      basePath: '/quizzes-and-courses',
      remoteEntryPath: '/remotes/quizzes-courses/remoteEntry.js',
      requiresAuth: true,
    },
  },
  {
    key: 'quests-achievements-notifications',
    label: 'Quests & Achievements',
    description: 'Track goals, unlock achievements, and receive learning notifications.',
    href: '/quests-and-achievements/',
    icon: 'quests',
    color: 'amber-darken-3',
    federation: {
      basePath: '/quests-and-achievements',
      remoteEntryPath: '/remotes/quests-achievements/remoteEntry.js',
      requiresAuth: true,
    },
  },
  {
    key: 'leaderboard-analytics',
    label: 'Leaderboard & Analytics',
    description: 'Compare progress, explore learning trends, and see how your results rank.',
    href: '/analytics/',
    icon: 'analytics',
    color: 'teal',
    federation: {
      basePath: '/analytics',
      remoteEntryPath: '/remotes/leaderboard-analytics/remoteEntry.js',
      requiresAuth: true,
    },
  },
]

export const serviceNavigation: readonly ServiceNavigationItem[] = [
  { key: 'home', label: 'Home', href: '/', icon: 'home' },
  ...featureServices,
]
