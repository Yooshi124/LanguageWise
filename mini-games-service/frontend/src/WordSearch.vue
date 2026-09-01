<template>
	<main class="game-page">
		<a class="back-button" :href="gameHome" aria-label="Return to the main game page">Back</a>
		<header class="game-header">
			<p class="eyebrow">Find the connection</p>
			<h1>Word Search</h1>
			<p>Drag through every letter in a hidden theme.</p>
		</header>
		<section v-if="noVocabulary" class="board-shell empty-state" role="status">
			<p class="empty-state__message">{{ NO_VOCABULARY_MESSAGE }}</p>
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
						:aria-label="`Letter ${letter}, position ${index + 1}`"
						@pointerdown="startSelection(index, $event)"
						@pointerenter="continueSelection(index)"
					>
						{{ letter }}
					</button>
				</div>
				<p v-if="selection.length" class="current-word" aria-live="polite">{{ selection }}</p>
				<p v-if="message" class="game-message" :class="{ 'is-error': error }" role="status">{{ message }}</p>
				<div class="game-actions">
					<button type="button" :disabled="gameComplete || (!hintWord && hintsUsed >= maximumHints) || hintBusy" @click="useHint">
						{{ hintWord ? 'Show order' : `Hint (${maximumHints - hintsUsed} left)` }}
					</button>
					<button type="button" :disabled="gameComplete || hintBusy" @click="giveUp">Give up</button>
					<button v-if="gameComplete" type="button" @click="resetGameHandler">Play again</button>
				</div>
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
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { initializeGame, submitWordSearchWord, useWordSearchHint, giveUpWordSearch, resetGame, isNoVocabularyError, NO_VOCABULARY_MESSAGE } from './api.js';

// App base path ('/mini-games/' through the gateway, '/' in local dev).
const gameHome = `${import.meta.env.BASE_URL}game`;

const board = ref([]);
const rows = ref(8);
const columns = ref(6);
const totalWords = ref(0);
const themeHint = ref('');
const wordPaths = ref({});
const featuredWord = ref('');
const hintWord = ref('');
const hintPath = ref([]);
const hintsUsed = ref(0);
const maximumHints = ref(3);
const foundWords = ref([]);
const revealedWords = ref([]);
const isGivenUp = ref(false);
const gameComplete = ref(false);
const selectedIndexes = ref([]);
const selecting = ref(false);
const loading = ref(false);
const hintBusy = ref(false);
const hintPulseIndexes = ref([]);
const message = ref('');
const error = ref(false);
const noVocabulary = ref(false);

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
};

const loadGame = async () => {
	const state = await initializeGame('word-search');
	applyState(state);
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

const finishSelection = async () => {
	if (!selecting.value) return;
	selecting.value = false;
	const word = selection.value;
	const indices = [...selectedIndexes.value];
	selectedIndexes.value = [];
	if (word.length < 3) return;
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
		await new Promise((resolve) => window.setTimeout(resolve, 180));
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
		hintPulseIndexes.value = [];
		await loadGame();
	} catch (requestError) {
		if (isNoVocabularyError(requestError)) {
			noVocabulary.value = true;
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
		} else {
			message.value = requestError.message;
			error.value = true;
		}
	}
	window.addEventListener('pointerup', finishSelection);
});
onUnmounted(() => window.removeEventListener('pointerup', finishSelection));
</script>

<style scoped>
.game-page { position: relative; min-height: 100vh; padding: 2rem; font-family: var(--lw-font, system-ui, sans-serif); color: #1c2b45; background: radial-gradient(circle at 50% 20%, #fffaf0 0, #fff1dc 45%, #f4e4cb 100%); }
.game-header { margin: 2rem auto; text-align: center; }
.eyebrow, .hint-label { color: #bd6a25; font-size: 0.75rem; font-weight: 800; letter-spacing: 0.12em; text-transform: uppercase; }
.eyebrow { margin: 0 0 0.5rem; }
h1 { margin: 0; font-size: clamp(2rem, 5vw, 3.25rem); }
.game-header p:last-child { margin: 0.65rem 0 0; color: #806f5d; }
.game-layout { display: flex; align-items: center; justify-content: center; gap: clamp(1.5rem, 5vw, 4rem); margin: 0 auto; }
.board-shell { padding: 1.25rem; background: rgba(255, 255, 255, 0.7); border: 1px solid rgba(189, 106, 37, 0.24); border-radius: 10px; box-shadow: 0 12px 32px rgba(118, 77, 31, 0.12); }
.empty-state { text-align: center; }
.empty-state__message { margin: 1rem 0; color: #65709d; font-weight: 600; line-height: 1.6; }
.scoreline { display: flex; align-items: baseline; justify-content: space-between; margin-bottom: 0.75rem; color: #806f5d; }
.scoreline strong { color: #1c2b45; font-size: 1.5rem; }
.game-grid { position: relative; display: grid; grid-template-columns: repeat(var(--columns), minmax(2rem, 1fr)); grid-template-rows: repeat(var(--rows), minmax(2rem, 1fr)); gap: 0.15rem; width: min(70vw, 30rem); aspect-ratio: 6 / 8; touch-action: none; user-select: none; }
.path-layer { position: absolute; inset: 0; z-index: 0; width: 100%; height: 100%; overflow: visible; pointer-events: none; }
.word-path { fill: none; stroke: #3d8b72; stroke-linecap: round; stroke-linejoin: round; stroke-width: 0.62; opacity: 0.62; }
.path-featured { stroke: #4c45a5; opacity: 0.95; }
.path-missed { stroke: #c85b70; opacity: 0.7; }
.grid-letter { position: relative; z-index: 1; display: grid; place-items: center; aspect-ratio: 1; padding: 0; border: 0; border-radius: 50%; color: #1c2b45; background: transparent; font: inherit; font-size: clamp(1rem, 3vw, 1.6rem); font-weight: 800; cursor: pointer; }
.grid-letter:hover, .grid-letter.selected { color: #fff; background: #bd6a25; }
.grid-letter.found { color: #fff; background: #3d8b72; }
.grid-letter.foundFeatured { background: #4c45a5; }
.grid-letter.hinted { box-shadow: inset 0 0 0 2px #bd6a25; }
.grid-letter.missed { color: #fff; background: #c85b70; }
.grid-letter.pulsing { color: #fff; background: #bd6a25; transform: translateY(-0.2rem); transition: transform 0.18s ease, background 0.18s ease; }
.current-word, .game-message { min-height: 1.5rem; margin: 0.75rem 0 0; text-align: center; font-weight: 700; }
.game-message.is-error { color: #a13d31; }
.game-actions { display: flex; justify-content: center; flex-wrap: wrap; gap: 0.5rem; margin-top: 0.75rem; }
.game-actions button { padding: 0.55rem 0.8rem; color: #1c2b45; border: 1px solid #d9b783; border-radius: 6px; background: #fff; font: inherit; font-weight: 700; cursor: pointer; }
.game-actions button:disabled { opacity: 0.5; cursor: not-allowed; }
.hint-box { width: min(22vw, 13rem); min-width: 9rem; padding: 1rem; border: 1px solid #d9b783; border-radius: 10px; background: rgba(255, 255, 255, 0.82); box-shadow: 0 8px 20px rgba(118, 77, 31, 0.1); }
.hint-label { font-size: 0.72rem; }
.hint-box p { margin: 0.6rem 0; line-height: 1.45; }
.hint-box ul { padding-left: 1.2rem; margin-bottom: 0; }
.hint-box li { margin: 0.2rem 0; }
.hint-box .foundWord { color: #3d8b72; font-weight: 800; }
.hint-box .missedWord { color: #c85b70; font-weight: 800; }
.back-button { position: absolute; top: 2rem; left: 2rem; padding: 0.5rem 0.75rem; color: #1c2b45; border: 1px solid #d9b783; border-radius: 6px; text-decoration: none; background: #fff; }
@media (max-width: 40rem) { .game-layout { flex-direction: column; } .game-grid { width: min(88vw, 30rem); } .hint-box { width: min(70vw, 16rem); } }
</style>
