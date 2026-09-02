import Associations from '../Associations.vue';
import GamePage from '../GamePage.vue';
import GuessTheWord from '../GuessTheWord.vue';
import WordSearch from '../WordSearch.vue';
import MiniGamesComponent from './MiniGamesComponent.vue';

export { MiniGamesComponent };

export const metadata = {
	key: 'mini-games',
	displayName: 'Mini Games',
	icon: 'games',
	basePath: '/mini-games',
	requiresAuth: true
};

export const routes = [
	{ path: '', name: 'mini-games-home', component: GamePage },
	{ path: 'game/guess-the-word', name: 'mini-games-guess-the-word', component: GuessTheWord },
	{ path: 'game/word-search', name: 'mini-games-word-search', component: WordSearch },
	{ path: 'game/associations', name: 'mini-games-associations', component: Associations }
];