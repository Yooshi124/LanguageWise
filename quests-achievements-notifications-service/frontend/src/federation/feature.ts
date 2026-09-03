import QuestsAchievementsNotificationsComponent from './QuestsAchievementsNotificationsComponent.vue'
import QuestsDashboard from '../views/QuestsDashboard.vue'

export { QuestsAchievementsNotificationsComponent }

export const metadata = {
  key: 'quests-achievements-notifications',
  displayName: 'Quests & Achievements',
  icon: 'quests',
  basePath: '/quests-and-achievements',
  requiresAuth: true,
}

export const routes = [{ path: '', name: 'quests-achievements-home', component: QuestsDashboard }]