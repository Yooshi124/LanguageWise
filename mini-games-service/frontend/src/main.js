import { createApp } from 'vue';
import MiniGamesComponent from './federation/MiniGamesComponent.vue';
import router from './router.js';

createApp(MiniGamesComponent, {
	hostContext: {
		user: null,
		navigate: async (path) => router.push(path),
		signIn: () => {},
		signOut: async () => {}
	}
}).use(router).mount('#app');