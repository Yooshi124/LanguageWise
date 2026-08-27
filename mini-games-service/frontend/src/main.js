import { createApp } from 'vue';
import GamePage from './GamePage.vue';
import VocabVoyage from './VocabVoyage.vue';
import WordStrings from './WordStrings.vue';
import Associations from './Associations.vue';

const routes = {
    '/vocab-voyage': VocabVoyage,
    '/word-strings': WordStrings,
    '/associations': Associations,
};

const page = routes[window.location.pathname] ?? GamePage;

createApp(page).mount('#app');