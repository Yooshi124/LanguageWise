interface ToolResult {
  tool: string
  isError: boolean
  result: unknown
}

type Row = { primary: string; secondary?: string }

const categoryLabels: Record<string, string> = {
  communityContribution: 'Community contributions',
  postEngagement: 'Post engagement',
  lessonCompletion: 'Lesson completion',
  courseCompletion: 'Course completion',
  quizResult: 'Quiz results',
  minigameWin: 'Mini game wins',
  loginStreak: 'Login streak',
  achievements: 'New achievements',
}

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : {}
}

function preferencesView(value: Record<string, unknown>) {
  const enabled = value.notificationsEnabled === true
  const email = typeof value.email === 'string' && value.email ? value.email : 'no email set'
  return {
    summary: `Notifications ${enabled ? 'on' : 'off'} · ${email}`,
    rows: Object.entries(categoryLabels).map(([key, label]): Row => ({
      primary: label,
      secondary: value[key] === true ? 'On' : 'Off',
    })),
  }
}

function achievementsView(value: Record<string, unknown>) {
  const achievements = Array.isArray(value.achievements) ? value.achievements.map(asRecord) : []
  const earned = achievements.filter((achievement) => achievement.earned === true).length
  return {
    summary: `${earned} of ${achievements.length} achievements earned`,
    rows: achievements.map((achievement): Row => {
      const progress = typeof achievement.progress === 'number' ? achievement.progress : 0
      const needed = typeof achievement.progressNeeded === 'number' ? achievement.progressNeeded : null
      return {
        primary: `${achievement.earned === true ? '✓ ' : ''}${String(achievement.name ?? '')}`,
        secondary: needed === null ? `Best: ${progress}` : `${Math.min(progress, needed)}/${needed}`,
      }
    }),
  }
}

export const assistant = {
  apiBase: '/quests-and-achievements/api',
  welcome: 'Ask me about your achievement progress, recent notifications, or email preferences.',
  placeholder: 'Ask Garry about your progress…',
  context: () => ({ routeName: 'quests-achievements-home' }),
  suggestions: () => [
    'What achievements should I aim for next?',
    'Explain my most recent notifications.',
    'Why am I not getting emails for post engagement?',
  ],
  tools: {
    chips: [
      { tool: 'quests_get_my_achievements', label: 'My achievements', arguments: () => ({}) },
      { tool: 'quests_get_my_preferences', label: 'My preferences', arguments: () => ({}) },
      { tool: 'quests_set_notifications_enabled', label: 'Pause notifications', arguments: () => ({ enabled: false }) },
      { tool: 'quests_set_notifications_enabled', label: 'Resume notifications', arguments: () => ({ enabled: true }) },
    ],
    view: (result: ToolResult) => {
      const value = asRecord(result.result)
      return result.tool === 'quests_get_my_achievements' ? achievementsView(value) : preferencesView(value)
    },
  },
}
