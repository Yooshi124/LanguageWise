import { createApp } from 'vue'
import QuestsAchievementsNotificationsComponent from './federation/QuestsAchievementsNotificationsComponent.vue'
import router from './router'

createApp(QuestsAchievementsNotificationsComponent, {
	hostContext: {
		user: { id: 1, name: 'Local user' },
		navigate: async () => {},
		signIn: () => {},
		signOut: async () => {},
	},
}).use(router).mount('#app')