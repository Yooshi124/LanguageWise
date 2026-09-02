import { createRouter, createWebHistory } from 'vue-router'
import QuestsAchievementsNotificationsComponent from './federation/QuestsAchievementsNotificationsComponent.vue'
import { routes } from './federation/feature'

export default createRouter({
  history: createWebHistory('/quests-and-achievements/'),
  routes: [{ path: '/', component: QuestsAchievementsNotificationsComponent, children: routes }],
})