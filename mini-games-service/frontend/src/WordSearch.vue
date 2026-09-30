<template>
	<main class="game-page word-search-game">
		<RouterLink class="back-button" :to="gameHome" aria-label="Return to the main game page">
			<AppIcon name="arrow-left" :size="18" />
			Back to games
		</RouterLink>
		<GameHelp :steps="howToPlay" />
		<header class="game-header">
			<p class="eyebrow">Find the connection</p>
			<h1>Word Search</h1>
			<p>Drag through every letter in a hidden theme.</p>
		</header>
		<section v-if="noVocabulary" class="board-shell empty-state" role="status">
			<p class="empty-state__message">{{ NO_VOCABULARY_MESSAGE }}</p>
		</section>
		<section v-else-if="aiUnavailable" class="board-shell empty-state" role="status">
			<p class="empty-state__message">{{ AI_UNAVAILABLE_MESSAGE }}</p>
		</section>
		<section v-else-if="starting" class="board-shell" aria-label="Generating game">
			<GeneratingState title="Preparing your Word Search…" />
		</section>
		<div v-else class="game-layout">
			<section class="board-shell" aria-label="Word Search game board">
				<div class="scoreline" aria-live="polite">
					<strong>{{ foundWords.length }} / {{ totalWords }}</strong>
					<span>words found</span>
				</div>
				<div class="game-grid" :style="{ '--columns': columns, '--rows': rows }" @pointermove="handlePointerMove">
					<svg class="path-layer" :viewBox="`0 0 ${columns} ${rows}`" aria-hidden="true">
						<polyline
							v-for="path in displayedPaths"
							:key="`${path.word}-${path.type}`"
							:points="pathPoints(path.indices)"
							class="word-path"
							:class="`path-${path.type}`"
						/>
					</svg>
					<button
						v-for="(letter, index) in board"
						:key="`${index}-${letter}`"
						type="button"
						class="grid-letter"
						:data-index="index"
						:class="cellClass(index)"
						:aria-pressed="selectedIndexes.includes(index)"
						:aria-label="`Letter ${letter}, position ${index + 1}${selectedIndexes.includes(index) ? ', selected' : ''}`"
						@pointerdown="startSelection(index, $event)"
						@pointerenter="continueSelection(index)"
						@keydown="handleGridKeydown(index, $event)"
					>
						{{ letter }}
					</button>
				</div>
				<p v-if="selection.length" class="current-word" aria-live="polite">{{ selection }}</p>
				<p v-if="message" class="game-message" :class="{ 'is-error': error, 'is-success': gameComplete && !isGivenUp }" role="status">{{ message }}</p>
				<div class="game-actions">
					<button type="button" :disabled="gameComplete || (!hintWord && hintsUsed >= maximumHints) || hintBusy" @click="useHint">
						{{ hintWord ? 'Show order' : `Hint (${maximumHints - hintsUsed} left)` }}
					</button>
					<button type="button" :disabled="gameComplete || hintBusy" @click="giveUp">Give up</button>
					<button v-if="gameComplete && definitions && Object.keys(definitions).length" type="button" @click="showDefinitions = true">Word definitions</button>
					<button v-if="gameComplete" type="button" :disabled="starting" @click="resetGameHandler">Play again</button>
				</div>
				<WordDefinitions :definitions="definitions" :visible="showDefinitions" @close="showDefinitions = false" />
				<GameSummary
					:visible="showSummary"
					:won="!isGivenUp"
					game-name="Word Search"
					:elapsed-seconds="elapsedSeconds"
					:stats="summaryStats"
					:share-text="shareText"
					@close="showSummary = false"
					@play-again="resetGameHandler"
				/>
			</section>

			<aside class="hint-box">
				<span class="hint-label">A little help</span>
				<p>{{ themeHint }}</p>
				<ul aria-label="Word list">
					<li v-for="word in visibleWords" :key="word" :class="wordStatus(word)">{{ word }}</li>
				</ul>
			</aside>
		</div>
	</main>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import { initializeGame, submitWordSearchWord, useWordSearchHint, giveUpWordSearch, resetGame, isNoVocabularyError, isAiUnavailableError, NO_VOCABULARY_MESSAGE, AI_UNAVAILABLE_MESSAGE } from './api.js';
import AppIcon from './components/AppIcon.vue';
import GameHelp from './components/GameHelp.vue';
import WordDefinitions from './components/WordDefinitions.vue';
import GameSummary from './components/GameSummary.vue';
import GeneratingState from './components/GeneratingState.vue';

// App base path ('/mini-games/' through the gateway, '/' in local dev).
const gameHome = { name: 'mini-games-home' };

const howToPlay = [
	'Words from your course vocabulary are hidden as a chain of connected letters in the grid.',
	'Press and drag from letter to letter to trace a word, then release to submit it.',
	'Keyboard: move between letters with the arrow keys, press Enter or Space to add a letter, then press Enter again on the last letter to submit (Escape cancels).',
	'Found words stay highlighted on the board and are ticked off in the list.',
	'Stuck? Spend a hint to reveal the start of a word — you only get a few.'
];

// Initial board dimensions and hint allowance before the server-generated state loads.
const DEFAULT_ROWS = 8;
const DEFAULT_COLUMNS = 6;
const DEFAULT_MAXIMUM_HINTS = 3;
const HINT_ANIMATION_DELAY_MS = 180;
const MINIMUM_SELECTION_LENGTH = 3;

const board = ref([]);
const rows = ref(DEFAULT_ROWS);
const columns = ref(DEFAULT_COLUMNS);
const totalWords = ref(0);
const themeHint = ref('');
const wordPaths = ref({});
const featuredWord = ref('');
const hintWord = ref('');
const hintPath = ref([]);
const hintsUsed = ref(0);
const maximumHints = ref(DEFAULT_MAXIMUM_HINTS);
const foundWords = ref([]);
const revealedWords = ref([]);
const isGivenUp = ref(false);
const gameComplete = ref(false);
const selectedIndexes = ref([]);
const selecting = ref(false);
const loading = ref(false);
const starting = ref(false);
const hintBusy = ref(false);
const hintPulseIndexes = ref([]);
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
	{ label: 'Words found', value: `${foundWords.value.length} / ${totalWords.value}` },
	{ label: 'Hints used', value: `${hintsUsed.value} / ${maximumHints.value}` }
]);

const shareText = computed(() => {
	if (!gameComplete.value) return '';
	const status = isGivenUp.value ? 'gave up after' : 'found';
	return `Word Search: ${status} ${foundWords.value.length}/${totalWords.value} words using ${hintsUsed.value} hint${hintsUsed.value === 1 ? '' : 's'}.`;
});

const selection = computed(() => selectedIndexes.value.map((index) => board.value[index]).join(''));
const foundIndexes = computed(() => foundWords.value.flatMap((word) => wordPaths.value[word] ?? []));
const missedIndexes = computed(() => revealedWords.value.flatMap((word) => wordPaths.value[word] ?? []));
const featuredIndexes = computed(() => foundWords.value.includes(featuredWord.value) ? wordPaths.value[featuredWord.value] ?? [] : []);
const hintedIndexes = computed(() => !isGivenUp.value && hintWord.value && !foundWords.value.includes(hintWord.value) ? hintPath.value : []);
const visibleWords = computed(() => isGivenUp.value ? Object.keys(wordPaths.value) : foundWords.value);
const displayedPaths = computed(() => {
	const paths = foundWords.value.map((word) => ({ word, indices: wordPaths.value[word], type: word === featuredWord.value ? 'featured' : 'found' }));
	if (isGivenUp.value) revealedWords.value.forEach((word) => paths.push({ word, indices: wordPaths.value[word], type: 'missed' }));
	return paths;
});

const cellClass = (index) => ({
	selected: selectedIndexes.value.includes(index),
	found: foundIndexes.value.includes(index),
	foundFeatured: featuredIndexes.value.includes(index),
	missed: isGivenUp.value && missedIndexes.value.includes(index) && !foundIndexes.value.includes(index),
	hinted: hintedIndexes.value.includes(index),
	pulsing: hintPulseIndexes.value.includes(index)
});
const wordStatus = (word) => ({ foundWord: foundWords.value.includes(word), missedWord: revealedWords.value.includes(word) });
const pathPoints = (indices) => (indices ?? []).map((index) => `${(index % columns.value) + 0.5},${Math.floor(index / columns.value) + 0.5}`).join(' ');

const applyState = (state) => {
	board.value = state.board;
	rows.value = state.rows;
	columns.value = state.columns;
	totalWords.value = state.totalWords;
	themeHint.value = state.themeHint;
	wordPaths.value = state.wordPaths;
	featuredWord.value = state.featuredWord;
	hintWord.value = state.hintWord;
	hintPath.value = state.hintPath;
	hintsUsed.value = state.hintsUsed;
	maximumHints.value = state.maximumHints;
	foundWords.value = state.words;
	revealedWords.value = state.revealedWords;
	isGivenUp.value = state.isGivenUp;
	gameComplete.value = state.isComplete;
	elapsedSeconds.value = state.elapsedSeconds ?? null;
	if (state.definitions) {
		definitions.value = state.definitions;
	}
};

const loadGame = async () => {
	starting.value = true;
	showDefinitions.value = false;
	showSummary.value = false;
	try {
		const state = await initializeGame('word-search');
		applyState(state);
	} finally {
		starting.value = false;
	}
};

const startSelection = (index, event) => {
	if (gameComplete.value || loading.value || hintBusy.value) return;
	event.currentTarget.setPointerCapture(event.pointerId);
	selecting.value = true;
	selectedIndexes.value = [index];
	message.value = '';
	error.value = false;
};

const continueSelection = (index) => {
	if (!selecting.value || index === undefined || selectedIndexes.value.includes(index)) return;
	selectedIndexes.value.push(index);
};

const handlePointerMove = (event) => {
	if (!selecting.value) return;
	const element = document.elementFromPoint(event.clientX, event.clientY);
	const index = element?.closest('.grid-letter')?.dataset.index;
	continueSelection(index === undefined ? undefined : Number(index));
};

// Move focus to the adjacent cell in the given direction, clamped to the board edges.
const focusCell = (index) => {
	document.querySelector(`.grid-letter[data-index="${index}"]`)?.focus();
};

const handleGridKeydown = (index, event) => {
	if (gameComplete.value || loading.value || hintBusy.value) return;

	const directions = { ArrowRight: [0, 1], ArrowLeft: [0, -1], ArrowDown: [1, 0], ArrowUp: [-1, 0] };
	if (directions[event.key]) {
		event.preventDefault();
		const [rowDelta, columnDelta] = directions[event.key];
		const row = Math.floor(index / columns.value);
		const column = index % columns.value;
		const targetRow = Math.min(Math.max(row + rowDelta, 0), rows.value - 1);
		const targetColumn = Math.min(Math.max(column + columnDelta, 0), columns.value - 1);
		focusCell(targetRow * columns.value + targetColumn);
		return;
	}

	if (event.key === 'Enter' || event.key === ' ') {
		event.preventDefault();
		if (!selecting.value) {
			selecting.value = true;
			selectedIndexes.value = [index];
			message.value = '';
			error.value = false;
			return;
		}
		const isLastSelected = selectedIndexes.value[selectedIndexes.value.length - 1] === index;
		if (isLastSelected && selectedIndexes.value.length >= MINIMUM_SELECTION_LENGTH) {
			finishSelection();
			return;
		}
		continueSelection(index);
		return;
	}

	if (event.key === 'Escape') {
		event.preventDefault();
		selecting.value = false;
		selectedIndexes.value = [];
	}
};

const finishSelection = async () => {
	if (!selecting.value) return;
	selecting.value = false;
	const word = selection.value;
	const indices = [...selectedIndexes.value];
	selectedIndexes.value = [];
	if (word.length < MINIMUM_SELECTION_LENGTH) return;
	loading.value = true;
	try {
		const result = await submitWordSearchWord(word, indices);
		applyState(result.state);
		message.value = result.isValid ? `${word} found` : `${word} is not one of the hidden words.`;
		error.value = !result.isValid;
	} catch (requestError) {
		message.value = requestError.message;
		error.value = true;
	} finally { loading.value = false; }
};

const animateHint = async () => {
	for (const index of hintPath.value) {
		hintPulseIndexes.value = [index];
		await new Promise((resolve) => window.setTimeout(resolve, HINT_ANIMATION_DELAY_MS));
	}
	hintPulseIndexes.value = [];
};

const useHint = async () => {
	if (hintBusy.value || gameComplete.value) return;
	hintBusy.value = true;
	try {
		if (hintWord.value) {
			await animateHint();
			message.value = `${hintWord.value} is shown in order.`;
			error.value = false;
			return;
		}
		if (hintsUsed.value >= maximumHints.value) return;
		const result = await useWordSearchHint();
		hintWord.value = result.word;
		hintPath.value = result.path;
		hintsUsed.value = result.state.hintsUsed;
		message.value = 'A word has been outlined. Trace it in order.';
		error.value = false;
	} catch (requestError) {
		message.value = requestError.message;
		error.value = true;
	} finally { hintBusy.value = false; }
};

const giveUp = async () => {
	try {
		const state = await giveUpWordSearch();
		applyState(state);
		message.value = 'All words are shown. Missed words are marked in red.';
		error.value = false;
	} catch (requestError) {
		message.value = 'Unable to reveal the remaining words.';
		error.value = true;
	}
};

const resetGameHandler = async () => {
	try {
		await resetGame('word-search');
		message.value = '';
		error.value = false;
		hintPulseIndexes.value = [];
		hintWord.value = '';
		hintPath.value = [];
		selectedIndexes.value = [];
		await loadGame();
	} catch (requestError) {
		if (isNoVocabularyError(requestError)) {
			noVocabulary.value = true;
		} else if (isAiUnavailableError(requestError)) {
			aiUnavailable.value = true;
		} else {
			message.value = requestError.message;
			error.value = true;
		}
	}
};

onMounted(async () => {
	try {
		await loadGame();
	} catch (requestError) {
		if (isNoVocabularyError(requestError)) {
			noVocabulary.value = true;
		} else if (isAiUnavailableError(requestError)) {
			aiUnavailable.value = true;
		} else {
			message.value = requestError.message;
			error.value = true;
		}
	}
	window.addEventListener('pointerup', finishSelection);
});
onUnmounted(() => window.removeEventListener('pointerup', finishSelection));
</script>
