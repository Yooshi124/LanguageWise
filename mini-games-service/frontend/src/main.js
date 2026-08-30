import { createApp } from 'vue';
import GamePage from './GamePage.vue';
import VocabVoyage from './VocabVoyage.vue';
import WordStrings from './WordStrings.vue';
import Associations from './Associations.vue';

const routes = {
    '/game': GamePage,
    '/game/guess-the-word': VocabVoyage,
    '/game/word-search': WordStrings,
    '/game/associations': Associations,
};

const page = routes[window.location.pathname] ?? GamePage;

createApp(page).mount('#app');