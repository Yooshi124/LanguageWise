import { createRouter, createWebHistory } from 'vue-router';
import { routes as featureRoutes } from './federation/feature.js';

const routes = [
    ...featureRoutes.map((route) => ({ ...route, path: route.path ? `/${route.path}` : '/' })),
    { path: '/:pathMatch(.*)*', redirect: { name: 'forums' } }
];

const base = window.location.pathname.startsWith('/chat-discussion/') ? '/chat-discussion/' : '/';

export const router = createRouter({
    history: createWebHistory(base),
    routes,
    scrollBehavior: (to, from, saved) => saved ?? { top: 0 }
});
