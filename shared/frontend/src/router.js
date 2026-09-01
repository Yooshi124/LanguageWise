import { createRouter, createWebHistory } from 'vue-router';
import HomeView from './views/HomeView.vue';
import LoginView from './views/LoginView.vue';
export default createRouter({
    history: createWebHistory(),
    routes: [
        { path: '/', name: 'home', component: HomeView },
        { path: '/index.html', redirect: '/' },
        { path: '/login.html', name: 'login', component: LoginView },
    ],
    scrollBehavior: () => ({ top: 0 }),
});
