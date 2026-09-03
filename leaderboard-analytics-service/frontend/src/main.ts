import { createApp } from 'vue'
import { VueQueryPlugin } from '@tanstack/vue-query'
import LeaderboardAnalyticsComponent from './federation/LeaderboardAnalyticsComponent.vue'
import router from './router'

const app = createApp(LeaderboardAnalyticsComponent)
app.use(router)
app.use(VueQueryPlugin)
app.mount('#app')
