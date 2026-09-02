import { createRouter, createWebHistory } from 'vue-router';
import { useAuth } from './composables/useAuth.js';
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

router.beforeEach(async (to) => {
    const { ensureLoaded, redirectToSignIn } = useAuth();

    if (await ensureLoaded()) {
        return true;
    }

    redirectToSignIn();
    return false;
});
