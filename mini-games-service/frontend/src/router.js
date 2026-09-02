import { createRouter, createWebHistory } from 'vue-router';
import MiniGamesComponent from './federation/MiniGamesComponent.vue';
import { routes } from './federation/feature.js';

export default createRouter({
	history: createWebHistory(import.meta.env.BASE_URL),
	routes: [
		{
			path: '/',
			component: MiniGamesComponent,
			props: {
				hostContext: {
					user: null,
					navigate: async () => {},
					signIn: () => {},
					signOut: async () => {}
				}
			},
			children: routes
		}
	]
});