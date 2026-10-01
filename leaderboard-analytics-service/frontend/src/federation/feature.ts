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

export const assistant = {
  apiBase: '/analytics/api',
  welcome: 'Ask me about your rankings, your last 30 days of lessons, or which course to focus on next.',
  placeholder: 'Ask Garry about your analytics…',
  context: () => ({ routeName: 'leaderboard-analytics-home' }),
  suggestions: () => [
    'How am I doing across my languages this month?',
    'Which course should I focus on next?',
    'Explain my current ranks.',
  ],
}