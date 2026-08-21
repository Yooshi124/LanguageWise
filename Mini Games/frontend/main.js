import { createApp } from 'vue';
import GamePage from './GamePage.vue';
import VocabVoyage from './VocabVoyage.vue';

const page = window.location.pathname === '/vocab-voyage'
    ? VocabVoyage
    : GamePage;

createApp(page).mount('#app');