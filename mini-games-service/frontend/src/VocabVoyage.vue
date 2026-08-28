<template>
	<main class="vocab-voyage">
		<a class="back-button" href="/" aria-label="Return to the main game page">Back</a>
		<header class="game-header">
			<p class="eyebrow">Daily vocabulary challenge</p>
			<h1>VocabVoyage</h1>
			<p>Find the hidden five-letter word in six guesses.</p>
		</header>
		<section class="board-shell" aria-label="VocabVoyage game board">
			<div class="game-grid">
				<div v-for="(box, index) in boxes" :key="index" class="grid-box" :class="colourClass(box.colour)">
					{{ box.letter }}
				</div>
			</div>
			<form class="guess-form" @submit.prevent="submitGuess">
			<label for="guess">Your guess</label>
			<div class="guess-controls">
				<input id="guess" v-model="guess" maxlength="5" minlength="5" autocomplete="off" :disabled="gameComplete || loading" placeholder="5 letters" />
				<button type="submit" :disabled="gameComplete || loading">{{ loading ? 'Checking' : 'Guess' }}</button>
			</div>
		</form>
		<p v-if="message" class="game-message" :class="{ 'is-error': error }">{{ message }}</p>
		<button v-if="gameComplete" class="reset-button" type="button" @click="resetGame">Play again</button>
		</section>
	</main>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';

const guess = ref('');
const guesses = ref([]);
const loading = ref(false);
const message = ref('');
const error = ref(false);
const gameComplete = ref(false);

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

const readResponse = async (response) => {
		const body = await response.text();
		if (!body) {
			return null;
		}

		try {
			return JSON.parse(body);
		} catch {
			throw new Error(`The game backend returned an unexpected response (${response.status}).`);
		}
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
		const response = await fetch('/api/vocab-voyage/guess', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ guess: guess.value })
		});
		const result = await readResponse(response);
		if (!response.ok) {
			throw new Error(result?.errors?.guess?.[0] ?? result?.error ?? `Unable to submit guess (${response.status}).`);
		}

		guesses.value.push(result);
		guess.value = '';
		gameComplete.value = result.isCorrect || guesses.value.length >= 6;
		message.value = result.isCorrect ? 'Correct. You found the word!' : gameComplete.value ? 'Game complete. Try again!' : 'Keep going.';
	} catch (exception) {
		message.value = exception.message;
		error.value = true;
	} finally {
		loading.value = false;
	}
};

const resetGame = async () => {
	try {
		const response = await fetch('/api/vocab-voyage/reset', { method: 'POST' });
		if (!response.ok) {
			throw new Error(`Unable to reset the game (${response.status}).`);
		}

		guesses.value = [];
		gameComplete.value = false;
		message.value = '';
		error.value = false;
	} catch (exception) {
		message.value = exception.message;
		error.value = true;
	}
};

onMounted(async () => {
	try {
		const response = await fetch('/api/vocab-voyage');
		const state = await readResponse(response);
		if (!response.ok) {
			throw new Error(`Unable to load the game (${response.status}).`);
		}

		guesses.value = state?.guesses ?? [];
		gameComplete.value = state?.isComplete ?? false;
	} catch (exception) {
		message.value = exception.message;
		error.value = true;
	}
});
</script>

<style scoped>
.vocab-voyage {
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

.game-message.is-error {
	color: #b3261e;
}

.reset-button {
	display: block;
	margin: 1rem auto 0;
}
</style>