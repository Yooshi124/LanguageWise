import { createApp } from 'vue';
import GamePage from './GamePage.vue';
import GuessTheWord from './GuessTheWord.vue';
import WordSearch from './WordSearch.vue';
import Associations from './Associations.vue';

const routes = {
    '/': GamePage,
    '/game': GamePage,
    '/game/guess-the-word': GuessTheWord,
    '/game/word-search': WordSearch,
    '/game/associations': Associations,
};

// Through the gateway the app is served under its base path (e.g. /mini-games/),
// so strip that prefix before resolving the route.
const path = window.location.pathname.replace(import.meta.env.BASE_URL, '/');
const page = routes[path] ?? GamePage;

createApp(page).mount('#app');