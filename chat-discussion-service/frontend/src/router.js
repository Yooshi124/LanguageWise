import { createRouter, createWebHistory } from 'vue-router';
import { useAuth } from './composables/useAuth.js';

import ForumIndexView from './views/ForumIndexView.vue';
import ForumView from './views/ForumView.vue';
import PostView from './views/PostView.vue';
import MyPostsView from './views/MyPostsView.vue';
import PostCreateView from './views/PostCreateView.vue';
import PostEditView from './views/PostEditView.vue';

const routes = [
    { path: '/', name: 'forums', component: ForumIndexView },
    { path: '/forums/:code', name: 'forum', component: ForumView, props: true },
    { path: '/my-posts', name: 'my-posts', component: MyPostsView },
    { path: '/new', name: 'post-create', component: PostCreateView },
    { path: '/posts/:id', name: 'post', component: PostView, props: true },
    { path: '/posts/:id/edit', name: 'post-edit', component: PostEditView, props: true },
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
