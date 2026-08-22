import { createApp } from 'vue';
import GamePage from './GamePage.vue';
import VocabVoyage from './VocabVoyage.vue';
// Skeleton proof of concept, not part of the Mini Games feature. See src/skeleton/.
import SampleItems from './skeleton/SampleItems.vue';

const routes = {
    '/vocab-voyage': VocabVoyage,
    '/sample-items': SampleItems
};

const page = routes[window.location.pathname] ?? GamePage;

createApp(page).mount('#app');