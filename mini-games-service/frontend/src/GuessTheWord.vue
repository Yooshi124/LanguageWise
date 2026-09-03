<template>
	<main class="guess-the-word">
		<RouterLink class="back-button" :to="gameHome" aria-label="Return to the main game page">
			<AppIcon name="arrow-left" :size="18" />
			Back to games
		</RouterLink>
		<GameHelp :steps="howToPlay" />
		<header class="game-header">
			<p class="eyebrow">Daily vocabulary challenge</p>
			<h1>Guess the word</h1>
			<p>Find the hidden five-letter word in six guesses.</p>
		</header>
		<section v-if="noVocabulary" class="board-shell empty-state" role="status">
			<p class="empty-state__message">{{ NO_VOCABULARY_MESSAGE }}</p>
		</section>
		<section v-else-if="aiUnavailable" class="board-shell empty-state" role="status">
			<p class="empty-state__message">{{ AI_UNAVAILABLE_MESSAGE }}</p>
		</section>
		<section v-else-if="starting" class="board-shell" aria-label="Generating game">
			<GeneratingState title="Preparing your word…" />
		</section>
		<section v-else class="board-shell" aria-label="GuessTheWord game board">
			<div class="game-grid">
				<div v-for="(box, index) in boxes" :key="index" class="grid-box" :class="colourClass(box.colour)">
					{{ box.letter }}
				</div>
			</div>
			<div class="alphabet" aria-label="Guessed letter status">
				<span
					v-for="letter in letterStatuses"
					:key="letter.value"
					class="alphabet-letter"
					:class="colourClass(letter.status)"
					:aria-label="`${letter.value}${letter.status ? `, ${letter.statusLabel}` : ', not guessed'}`"
				>
					{{ letter.value }}
				</span>
			</div>
			<form class="guess-form" @submit.prevent="submitGuess">
			<label for="guess">Your guess</label>
			<div class="guess-controls">
				<input id="guess" v-model="guess" maxlength="5" minlength="5" autocomplete="off" :disabled="gameComplete || loading" placeholder="5 letters" />
				<button type="submit" :disabled="gameComplete || loading">{{ loading ? 'Checking' : 'Guess' }}</button>
			</div>		<div v-if="specialLetters.length" class="special-letters" aria-label="Special letters for this language">
			<button
				v-for="letter in specialLetters"
				:key="letter"
				type="button"
				class="special-letter"
				:disabled="gameComplete || loading || guess.length >= 5"
				@click="insertLetter(letter)"
			>
				{{ letter }}
			</button>
		</div>		</form>
			<div v-if="gameComplete && !isWon" class="failure-banner" role="alert">
				<strong>Out of guesses</strong>
				<span>The answer was <b>{{ correctAnswer }}</b>. Start a new round to play again.</span>
			</div>
		<p v-if="message" class="game-message" :class="{ 'is-error': error }">{{ message }}</p>
		<button v-if="gameComplete && definitions && Object.keys(definitions).length" class="reset-button" type="button" @click="showDefinitions = true">Word definitions</button>
		<button v-if="gameComplete" class="reset-button" type="button" :disabled="starting" @click="resetGameHandler">Play again</button>
		<WordDefinitions :definitions="definitions" :visible="showDefinitions" @close="showDefinitions = false" />
		</section>
	</main>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import { submitGuessTheWordGuess, resetGame, initializeGame, isNoVocabularyError, isAiUnavailableError, NO_VOCABULARY_MESSAGE, AI_UNAVAILABLE_MESSAGE } from './api.js';
import AppIcon from './components/AppIcon.vue';
import GameHelp from './components/GameHelp.vue';
import WordDefinitions from './components/WordDefinitions.vue';
import GeneratingState from './components/GeneratingState.vue';

// App base path ('/mini-games/' through the gateway, '/' in local dev).
const gameHome = { name: 'mini-games-home' };

// Letters in the game's word pool that can't be typed as a plain English letter
// (ß — unlike Ä, Ö, Ü, Ñ, Ł… — has no ASCII stand-in), provided by the backend.
const specialLetters = ref([]);

const howToPlay = [
	'Find the hidden five-letter word within six guesses.',
	'Type a guess and press Guess — every letter is checked against the answer.',
	'A tile shows when a letter is in the right spot, in the word but in the wrong spot, or not in the word at all.',
	'Use the alphabet row below the board to keep track of the letters you have already tried.'
];

const guess = ref('');
const guesses = ref([]);
const loading = ref(false);
const starting = ref(false);
const message = ref('');
const error = ref(false);
const noVocabulary = ref(false);
const aiUnavailable = ref(false);
const gameComplete = ref(false);
const isWon = ref(false);
const correctAnswer = ref('');
const definitions = ref(null);
const showDefinitions = ref(false);
const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.split('');

const boxes = computed(() => Array.from({ length: 30 }, (_, index) => {
		const row = Math.floor(index / 5);
		const letter = guesses.value[row]?.guess[index % 5] ?? '';
		const colour = guesses.value[row]?.colours[index % 5] ?? '';
		return { letter, colour };
	}));

const colourClass = (colour) => ({
		'cell-correct': colour === 'G',
		'cell-present': colour === 'O',
		'cell-absent': colour === 'R'
});

const letterStatuses = computed(() => {
	const statusPriority = { R: 1, O: 2, G: 3 };
	const statusLabels = { R: 'incorrect', O: 'in the word', G: 'correct position' };
	const statuses = {};

	guesses.value.forEach((submittedGuess) => {
		const submittedLetters = submittedGuess.guess?.toUpperCase() ?? '';
		submittedLetters.split('').forEach((letter, index) => {
			const status = submittedGuess.colours?.[index];
			if (status && (!statuses[letter] || statusPriority[status] > statusPriority[statuses[letter]])) {
				statuses[letter] = status;
			}
		});
	});

	return alphabet.map((value) => ({
		value,
		status: statuses[value] ?? '',
		statusLabel: statusLabels[statuses[value]] ?? ''
	}));
});

const insertLetter = (letter) => {
	if (guess.value.length < 5) {
		guess.value += letter;
	}
	document.getElementById('guess')?.focus();
};

const submitGuess = async () => {
	if (guess.value.trim().length !== 5) {
		message.value = 'Enter a five-letter guess.';
		error.value = true;
		return;
	}

	loading.value = true;
	message.value = '';
	error.value = false;
	try {
		const result = await submitGuessTheWordGuess(guess.value);
		guesses.value.push(result);
		guess.value = '';
		gameComplete.value = result.isCorrect || guesses.value.length >= 6;
		isWon.value = result.isCorrect;
		correctAnswer.value = result.correctAnswer ?? '';
		if (result.definitions && Object.keys(result.definitions).length > 0) {
			definitions.value = result.definitions;
		}
		message.value = result.isCorrect ? 'Correct. You found the word!' : gameComplete.value ? '' : 'Keep going.';
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
	try {
		await resetGame('guess-the-word');
		const state = await initializeGame('guess-the-word');
		guesses.value = state?.guesses ?? [];
		gameComplete.value = state?.isComplete ?? false;
		isWon.value = state?.isWon ?? false;
		correctAnswer.value = state?.correctAnswer ?? '';
		specialLetters.value = state?.specialLetters ?? specialLetters.value;
		definitions.value = state?.definitions ?? null;
		guess.value = '';
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
	try {
		const state = await initializeGame('guess-the-word');
		guesses.value = state?.guesses ?? [];
		gameComplete.value = state?.isComplete ?? false;
		isWon.value = state?.isWon ?? false;
		correctAnswer.value = state?.correctAnswer ?? '';
		specialLetters.value = state?.specialLetters ?? [];
		definitions.value = state?.definitions ?? null;
	} catch (exception) {
		if (isNoVocabularyError(exception)) {
			noVocabulary.value = true;
		} else if (isAiUnavailableError(exception)) {
			aiUnavailable.value = true;
		} else {
			message.value = 'Could not start game: ' + exception.message;
			error.value = true;
		}
	}
});
</script>
