import { createApp } from 'vue';
import { ChatDiscussionComponent } from './federation/feature.js';
import { router } from './router.js';

createApp(ChatDiscussionComponent).use(router).mount('#app');
