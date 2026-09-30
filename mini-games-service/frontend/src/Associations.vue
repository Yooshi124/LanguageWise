<template>
	<main class="game-page associations-game">
		<RouterLink class="back-button" :to="gameHome" aria-label="Return to the main game page">
			<AppIcon name="arrow-left" :size="18" />
			Back to games
		</RouterLink>
		<GameHelp :steps="howToPlay" />
		<header class="game-header">
			<p class="eyebrow">Make the associations</p>
			<h1>Associations</h1>
			<p>Find the words that belong together.</p>
		</header>
		<section v-if="noVocabulary" class="board-shell empty-state" role="status">
			<p class="empty-state__message">{{ NO_VOCABULARY_MESSAGE }}</p>
		</section>
		<section v-else-if="aiUnavailable" class="board-shell empty-state" role="status">
			<p class="empty-state__message">{{ AI_UNAVAILABLE_MESSAGE }}</p>
		</section>
		<section v-else-if="starting" class="board-shell" aria-label="Generating game">
			<GeneratingState title="Preparing your Associations game…" />
		</section>
		<section v-else class="board-shell" aria-label="Associations game board">
			<div class="attempts" aria-label="Remaining mistakes">
				<span v-for="attempt in MAX_MISTAKES" :key="attempt" class="attempt-icon" :class="{ used: attempt <= failedAttempts }" :aria-label="attempt <= failedAttempts ? 'Mistake used' : 'Mistake remaining'"></span>
			</div>
			<div v-if="gameComplete && !isWon" class="failure-banner" role="alert">
				<strong>Game over</strong>
				<span>Here were the correct associations. Start a new game to try again.</span>
			</div>
			<div v-if="solvedGroups.length" class="solved-groups">
				<article v-for="(group, index) in solvedGroups" :key="group.summary" class="solved-group">
					<div class="group-bar" :class="`group-color-${index % GROUP_COLOUR_COUNT}`">
						<strong>{{ group.summary }}</strong>
						<span>{{ group.words.join(' / ') }}</span>
					</div>
				</article>
			</div>
			<div v-if="revealedGroups.length" class="revealed-groups">
				<p class="revealed-title">Correct associations</p>
				<article v-for="(group, index) in revealedGroups" :key="`revealed-${group.summary}`" class="revealed-group">
					<div class="group-bar" :class="`group-color-${(solvedGroups.length + index) % GROUP_COLOUR_COUNT}`">
						<strong>{{ group.summary }}</strong>
						<span>{{ group.words.join(' / ') }}</span>
					</div>
				</article>
			</div>

			<div v-if="!gameComplete" class="game-grid">
				<button
					v-for="word in words"
					:key="word"
					type="button"
					class="word-box"
					:class="{ selected: selectedWords.includes(word) }"
					:aria-pressed="selectedWords.includes(word)"
					:disabled="gameComplete"
					@click="toggleWord(word)"
				>
					{{ word }}
				</button>
			</div>

			<div class="controls">
				<p class="status" :class="{ error, 'is-success': isWon && gameComplete }" aria-live="polite">{{ message }}</p>
				<button class="submit-button" type="button" :disabled="selectedWords.length !== GROUP_SIZE || loading || gameComplete" @click="submitGuess">
					{{ loading ? 'Checking...' : 'Submit group' }}
				</button>
				<button v-if="gameComplete && definitions && Object.keys(definitions).length" class="reset-button" type="button" @click="showDefinitions = true">Word definitions</button>
				<button class="reset-button" type="button" :disabled="starting" @click="resetGameHandler">New game</button>
			</div>
			<WordDefinitions :definitions="definitions" :visible="showDefinitions" @close="showDefinitions = false" />
			<GameSummary
				:visible="showSummary"
				:won="isWon"
				game-name="Associations"
				:elapsed-seconds="elapsedSeconds"
				:stats="summaryStats"
				:share-text="shareText"
				@close="showSummary = false"
				@play-again="resetGameHandler"
			/>
		</section>
	</main>
 </template>

 <script setup>
 import { computed, onMounted, ref, watch } from 'vue';
 import { initializeGame, submitAssociationsGuess, resetGame, isNoVocabularyError, isAiUnavailableError, NO_VOCABULARY_MESSAGE, AI_UNAVAILABLE_MESSAGE } from './api.js';
 import AppIcon from './components/AppIcon.vue';
 import GameHelp from './components/GameHelp.vue';
 import WordDefinitions from './components/WordDefinitions.vue';
 import GameSummary from './components/GameSummary.vue';
 import GeneratingState from './components/GeneratingState.vue';

 // App base path ('/mini-games/' through the gateway, '/' in local dev).
const gameHome = { name: 'mini-games-home' };

const GROUP_SIZE = 4;
const MAX_MISTAKES = 4;
const GROUP_COLOUR_COUNT = 4;

 const howToPlay = [
 	'All sixteen words secretly belong to four groups of four.',
 	'Tap four words you think belong together, then press Submit group.',
 	'A wrong group costs one mistake — the dots above the board show how many you have left.',
 	'Find all four groups before running out of mistakes to win.'
 ];

 const words = ref([]);
 const selectedWords = ref([]);
 const solvedGroups = ref([]);
const revealedGroups = ref([]);
 const failedAttempts = ref(0);
 const gameComplete = ref(false);
const isWon = ref(false);
 const loading = ref(false);
 const starting = ref(false);
 const message = ref('');
 const error = ref(false);
 const noVocabulary = ref(false);
 const aiUnavailable = ref(false);
 const definitions = ref(null);
 const showDefinitions = ref(false);
const showSummary = ref(false);
const elapsedSeconds = ref(null);

// Open the results panel automatically the moment a round ends.
watch(gameComplete, (complete) => {
	if (complete) showSummary.value = true;
});

const summaryStats = computed(() => [
	{ label: 'Groups found', value: `${solvedGroups.value.length} / ${solvedGroups.value.length + revealedGroups.value.length}` },
	{ label: 'Mistakes', value: `${failedAttempts.value} / ${MAX_MISTAKES}` }
]);

const shareText = computed(() => {
	if (!gameComplete.value) return '';
	const totalGroups = solvedGroups.value.length + revealedGroups.value.length;
	return `Associations: found ${solvedGroups.value.length}/${totalGroups} groups with ${failedAttempts.value} mistake${failedAttempts.value === 1 ? '' : 's'}.`;
});

 const applyState = (state) => {
	 words.value = state.words;
	 selectedWords.value = state.selectedWords;
	 solvedGroups.value = state.solvedGroups;
	revealedGroups.value = state.revealedGroups;
	 failedAttempts.value = state.failedAttempts;
	 gameComplete.value = state.isComplete;
	isWon.value = state.isWon;
	elapsedSeconds.value = state.elapsedSeconds ?? null;
	if (state.definitions && Object.keys(state.definitions).length > 0) {
		definitions.value = state.definitions;
	}
 };

 const toggleWord = (word) => {
	 if (selectedWords.value.includes(word)) {
		 selectedWords.value = selectedWords.value.filter((selectedWord) => selectedWord !== word);
	 } else if (selectedWords.value.length < GROUP_SIZE) {
		 selectedWords.value = [...selectedWords.value, word];
	 }
 };

 const submitGuess = async () => {
	 loading.value = true;
	 message.value = '';
	 error.value = false;
	 try {
		 const result = await submitAssociationsGuess(selectedWords.value);
		 applyState(result.state);
		message.value = result.isAssociation
			 ? `Connected: ${result.group.summary}.`
			: gameComplete.value ? '' : 'Those words do not belong together.';
		 error.value = !result.isAssociation;
	 } catch (exception) {
		 message.value = exception.message;
		 error.value = true;
	 } finally {
		 loading.value = false;
	 }
 };

 const resetGameHandler = async () => {
	 starting.value = true;
	 showDefinitions.value = false;
	showSummary.value = false;
	 try {
		 await resetGame('associations');
		 const state = await initializeGame('associations');
		 applyState(state);
		 message.value = '';
		 error.value = false;
	 } catch (exception) {
		 if (isNoVocabularyError(exception)) {
			 noVocabulary.value = true;
		 } else if (isAiUnavailableError(exception)) {
			 aiUnavailable.value = true;
		 } else {
			 message.value = exception.message;
			 error.value = true;
		 }
	 } finally {
		 starting.value = false;
	 }
 };

 onMounted(async () => {
	 starting.value = true;
	 try {
		 const state = await initializeGame('associations');
		 applyState(state);
	 } catch (exception) {
		 if (isNoVocabularyError(exception)) {
			 noVocabulary.value = true;
		 } else if (isAiUnavailableError(exception)) {
			 aiUnavailable.value = true;
		 } else {
			 message.value = 'Could not start game: ' + exception.message;
			 error.value = true;
		 }
	 } finally {
		 starting.value = false;
	 }
 });
 </script>
