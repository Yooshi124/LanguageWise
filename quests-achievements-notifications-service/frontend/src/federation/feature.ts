import QuestsAchievementsNotificationsComponent from './QuestsAchievementsNotificationsComponent.vue'
import QuestsDashboard from '../views/QuestsDashboard.vue'

export { QuestsAchievementsNotificationsComponent }

export const metadata = {
  key: 'quests-achievements-notifications',
  displayName: 'Achievements & Notifications',
  icon: 'quests',
  basePath: '/quests-and-achievements',
  requiresAuth: true,
}

export const routes = [{ path: '', name: 'quests-achievements-home', component: QuestsDashboard }]

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
}