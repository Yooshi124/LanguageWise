import LeaderboardAnalyticsComponent from './LeaderboardAnalyticsComponent.vue'
import HomeView from '../views/HomeView.vue'

export { LeaderboardAnalyticsComponent }

export const metadata = {
  key: 'leaderboard-analytics',
  displayName: 'Leaderboard & Analytics',
  icon: 'analytics',
  basePath: '/analytics',
  requiresAuth: true,
}

export const routes = [{ path: '', name: 'leaderboard-analytics-home', component: HomeView }]