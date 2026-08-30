import { createApp } from 'vue';
import GamePage from './GamePage.vue';
import GuessTheWord from './GuessTheWord.vue';
import WordSearch from './WordSearch.vue';
import Associations from './Associations.vue';

const routes = {
    '/game': GamePage,
    '/game/guess-the-word': GuessTheWord,
    '/game/word-search': WordSearch,
    '/game/associations': Associations,
};

const page = routes[window.location.pathname] ?? GamePage;

createApp(page).mount('#app');