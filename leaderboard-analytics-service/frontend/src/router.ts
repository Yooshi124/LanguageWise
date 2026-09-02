import { createRouter, createWebHistory } from 'vue-router'
import LeaderboardAnalyticsComponent from './federation/LeaderboardAnalyticsComponent.vue'
import { routes } from './federation/feature'

export default createRouter({
  history: createWebHistory('/analytics/'),
  routes: [{ path: '/', component: LeaderboardAnalyticsComponent, children: routes }],
})