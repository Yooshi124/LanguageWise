<template>
	<main class="guess-the-word">
		<a class="back-button" :href="gameHome" aria-label="Return to the main game page">Back</a>
		<GameHelp :steps="howToPlay" />
		<header class="game-header">
			<p class="eyebrow">Daily vocabulary challenge</p>
			<h1>Guess the word</h1>
			<p>Find the hidden five-letter word in six guesses.</p>
		</header>
		<section v-if="noVocabulary" class="board-shell empty-state" role="status">
			<p class="empty-state__message">{{ NO_VOCABULARY_MESSAGE }}</p>
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
		<button v-if="gameComplete" class="reset-button" type="button" @click="resetGameHandler">Play again</button>
		</section>
	</main>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import { submitGuessTheWordGuess, resetGame, initializeGame, getCourseCode, isNoVocabularyError, NO_VOCABULARY_MESSAGE } from './api.js';
import GameHelp from './components/GameHelp.vue';

// App base path ('/mini-games/' through the gateway, '/' in local dev).
const gameHome = `${import.meta.env.BASE_URL}game`;

// Letters in course languages that aren't accented variants of English
// letters (those — Ä, Ö, Ü, Ñ, Ł… — are matched by typing the plain letter).
// Only letters that genuinely can't be typed as an English letter get a button.
const specialLettersByCourse = {
	de: ['ß'],
};
const specialLetters = specialLettersByCourse[getCourseCode()] ?? [];

const howToPlay = [
	'Find the hidden five-letter word within six guesses.',
	'Type a guess and press Guess — every letter is checked against the answer.',
	'A tile shows when a letter is in the right spot, in the word but in the wrong spot, or not in the word at all.',
	'Use the alphabet row below the board to keep track of the letters you have already tried.'
];

const guess = ref('');
const guesses = ref([]);
const loading = ref(false);
const message = ref('');
const error = ref(false);
const noVocabulary = ref(false);
const gameComplete = ref(false);
const isWon = ref(false);
const correctAnswer = ref('');
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
		message.value = result.isCorrect ? 'Correct. You found the word!' : gameComplete.value ? '' : 'Keep going.';
	} catch (exception) {
		message.value = exception.message;
		error.value = true;
	} finally {
		loading.value = false;
	}
};

const resetGameHandler = async () => {
	try {
		await resetGame('guess-the-word');
		guesses.value = [];
		gameComplete.value = false;
		isWon.value = false;
		correctAnswer.value = '';
		message.value = '';
		error.value = false;
	} catch (exception) {
		message.value = exception.message;
		error.value = true;
	}
};

onMounted(async () => {
	try {
		const state = await initializeGame('guess-the-word');
		guesses.value = state?.guesses ?? [];
		gameComplete.value = state?.isComplete ?? false;
		isWon.value = state?.isWon ?? false;
		correctAnswer.value = state?.correctAnswer ?? '';
	} catch (exception) {
		if (isNoVocabularyError(exception)) {
			noVocabulary.value = true;
		} else {
			message.value = 'Could not start game: ' + exception.message;
			error.value = true;
		}
	}
});
</script>

<style scoped>
.guess-the-word {
	position: relative;
	min-height: 100vh;
	padding: 2.5rem 2rem 4rem;
	font-family: var(--lw-font, system-ui, sans-serif);
	color: #1c2b45;
	background: radial-gradient(circle at 50% 20%, #f1fffc 0, #e6f3f2 42%, #dcebea 100%);
}

.game-header {
	margin: 2rem auto 2rem;
	text-align: center;
}

.eyebrow {
	margin: 0 0 0.5rem;
	color: #10897a;
	font-size: 0.75rem;
	font-weight: 800;
	letter-spacing: 0.12em;
	text-transform: uppercase;
}

h1 {
	margin: 0;
	font-size: clamp(2rem, 5vw, 3.25rem);
	letter-spacing: -0.03em;
}

.game-header p:last-child {
	margin: 0.65rem 0 0;
	color: #5b6b85;
}

.back-button {
	position: absolute;
	top: 2rem;
	left: 2rem;
	padding: 0.5rem 0.75rem;
	color: #1c2b45;
	border: 1px solid #9acbc4;
	border-radius: 6px;
	text-decoration: none;
	background: #fff;
}

.back-button:hover,
.back-button:focus-visible {
	border-color: #10897a;
	outline: 3px solid rgba(16, 137, 122, 0.18);
}

.board-shell {
	width: min(90vw, 34rem);
	margin: 0 auto;
	padding: 1.5rem;
	background: rgba(255, 255, 255, 0.72);
	border: 1px solid rgba(16, 137, 122, 0.2);
	border-radius: 10px;
	box-shadow: 0 12px 32px rgba(28, 85, 78, 0.12);
}

.empty-state { text-align: center; }
.empty-state__message { margin: 1rem 0; color: #5b6b85; font-weight: 600; line-height: 1.6; }

.attempts { display: flex; justify-content: center; gap: 0.55rem; margin-bottom: 1rem; }
.attempt-icon { width: 0.8rem; height: 0.8rem; border: 2px solid #10897a; border-radius: 50%; background: #b8e4dc; }
.attempt-icon.used { border-color: #aab6c6; background: transparent; opacity: 0.45; }

.game-grid {
	display: grid;
	grid-template-columns: repeat(5, minmax(3rem, 1fr));
	grid-template-rows: repeat(6, minmax(3rem, 1fr));
	gap: 0.5rem;
	width: min(90vw, 30rem);
	aspect-ratio: 5 / 6;
	margin: 0 auto;
}

.grid-box {
	display: grid;
	place-items: center;
	border: 2px solid #a7cbc7;
	border-radius: 4px;
	background: rgba(255, 255, 255, 0.9);
	color: #1c2b45;
	font-size: 1.25rem;
	font-weight: 800;
}

.grid-box.cell-correct {
	border-color: #10897a;
	background: #b8e4dc;
}

.grid-box.cell-present {
	border-color: #d87832;
	background: #f6c58f;
}

.grid-box.cell-absent {
	border-color: #8290a8;
	background: #dce2eb;
}

.alphabet {
	display: grid;
	grid-template-columns: repeat(13, minmax(1.55rem, 1fr));
	gap: 0.35rem;
	max-width: 30rem;
	margin: 1.25rem auto 0;
}

.alphabet-letter {
	display: grid;
	place-items: center;
	min-width: 0;
	aspect-ratio: 1;
	border: 1px solid #a7cbc7;
	border-radius: 4px;
	background: rgba(255, 255, 255, 0.9);
	color: #1c2b45;
	font-size: 0.78rem;
	font-weight: 800;
}

.alphabet-letter.cell-correct {
	border-color: #10897a;
	background: #b8e4dc;
}

.alphabet-letter.cell-present {
	border-color: #d87832;
	background: #f6c58f;
}

.alphabet-letter.cell-absent {
	border-color: #8290a8;
	background: #dce2eb;
	color: #5b6b85;
}

.guess-form {
	max-width: 20rem;
	margin: 1.25rem auto 0;
	text-align: left;
}

.guess-form label {
	display: block;
	margin-bottom: 0.4rem;
	color: #5b6b85;
	font-size: 0.85rem;
	font-weight: 700;
}

.guess-controls {
	display: flex;
	gap: 0.5rem;
}

.special-letters {
	display: flex;
	gap: 0.4rem;
	margin-top: 0.5rem;
}

.special-letter {
	min-width: 2.25rem;
	padding: 0.4rem 0.6rem;
	color: #1c2b45;
	border: 1px solid #9acbc4;
	border-radius: 6px;
	background: #fff;
	font: inherit;
	font-weight: 700;
	cursor: pointer;
	transition: border-color 120ms ease, background 120ms ease;
}

.special-letter:hover:not(:disabled),
.special-letter:focus-visible {
	border-color: #10897a;
	background: #e6f3f2;
}

.special-letter:disabled {
	opacity: 0.45;
	cursor: default;
}

.guess-controls input {
	min-width: 0;
	flex: 1;
	padding: 0.65rem 0.75rem;
	border: 1px solid #a7cbc7;
	border-radius: 6px;
	font: inherit;
	letter-spacing: 0.12em;
	text-transform: uppercase;
}

.guess-controls button,
.reset-button {
	padding: 0.65rem 0.9rem;
	color: #fff;
	border: 0;
	border-radius: 6px;
	background: #10897a;
	font: inherit;
	font-weight: 700;
	cursor: pointer;
}

.guess-controls button:disabled {
	opacity: 0.6;
	cursor: wait;
}

.game-message {
	margin: 1rem 0 0;
	color: #10897a;
	font-size: 0.9rem;
	text-align: center;
}

.failure-banner { display: grid; gap: 0.2rem; margin: 1.25rem -0.25rem 0; padding: 0.9rem 1rem; color: #713746; border: 1px solid #e4a1ad; border-left: 5px solid #c85b70; border-radius: 6px; background: #fff0f2; text-align: center; }
.failure-banner strong { font-size: 1rem; }
.failure-banner span { font-size: 0.85rem; }

.game-message.is-error {
	color: #b3261e;
}

.reset-button {
	display: block;
	margin: 1rem auto 0;
}
</style>