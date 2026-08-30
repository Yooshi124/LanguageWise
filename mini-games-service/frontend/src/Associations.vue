<template>
	<main class="game-page">
		<a class="back-button" href="/game" aria-label="Return to the main game page">Back</a>
		<header class="game-header">
			<p class="eyebrow">Make the associations</p>
			<h1>Associations</h1>
			<p>Find the words that belong together.</p>
		</header>
		<section class="board-shell" aria-label="Associations game board">
			<div class="attempts" aria-label="Remaining mistakes">
				<span v-for="attempt in 4" :key="attempt" class="attempt-icon" :class="{ used: attempt <= failedAttempts }" :aria-label="attempt <= failedAttempts ? 'Mistake used' : 'Mistake remaining'"></span>
			</div>
			<div v-if="gameComplete && !isWon" class="failure-banner" role="alert">
				<strong>Game over</strong>
				<span>Here were the correct associations. Start a new game to try again.</span>
			</div>
			<div v-if="solvedGroups.length" class="solved-groups">
				<article v-for="(group, index) in solvedGroups" :key="group.summary" class="solved-group">
					<div class="group-bar" :class="`group-color-${index % 4}`">
						<strong>{{ group.summary }}</strong>
						<span>{{ group.words.join(' / ') }}</span>
					</div>
				</article>
			</div>
			<div v-if="revealedGroups.length" class="revealed-groups">
				<p class="revealed-title">Correct associations</p>
				<article v-for="(group, index) in revealedGroups" :key="`revealed-${group.summary}`" class="revealed-group">
					<div class="group-bar" :class="`group-color-${(solvedGroups.length + index) % 4}`">
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
				<p class="status" :class="{ error }" aria-live="polite">{{ message }}</p>
				<button class="submit-button" type="button" :disabled="selectedWords.length !== 4 || loading || gameComplete" @click="submitGuess">
					{{ loading ? 'Checking...' : 'Submit group' }}
				</button>
				<button class="reset-button" type="button" @click="resetGame">New game</button>
			</div>
		</section>
	</main>
 </template>

 <script setup>
 import { onMounted, ref } from 'vue';

 const words = ref([]);
 const selectedWords = ref([]);
 const solvedGroups = ref([]);
const revealedGroups = ref([]);
 const failedAttempts = ref(0);
 const gameComplete = ref(false);
const isWon = ref(false);
 const loading = ref(false);
 const message = ref('');
 const error = ref(false);

 const readResponse = async (response) => {
	 const text = await response.text();
	 return text ? JSON.parse(text) : null;
 };

 const applyState = (state) => {
	 words.value = state.words;
	 selectedWords.value = state.selectedWords;
	 solvedGroups.value = state.solvedGroups;
	revealedGroups.value = state.revealedGroups;
	 failedAttempts.value = state.failedAttempts;
	 gameComplete.value = state.isComplete;
	isWon.value = state.isWon;
 };

 const toggleWord = (word) => {
	 if (selectedWords.value.includes(word)) {
		 selectedWords.value = selectedWords.value.filter((selectedWord) => selectedWord !== word);
	 } else if (selectedWords.value.length < 4) {
		 selectedWords.value = [...selectedWords.value, word];
	 }
 };

 const submitGuess = async () => {
	 loading.value = true;
	 message.value = '';
	 error.value = false;
	 try {
		 const response = await fetch('/api/associations/guess', {
			 method: 'POST',
			 headers: { 'Content-Type': 'application/json' },
			 body: JSON.stringify({ words: selectedWords.value })
		 });
		 const result = await readResponse(response);
		 if (!response.ok) {
			 throw new Error(result?.errors?.words?.[0] ?? result?.error ?? `Unable to submit group (${response.status}).`);
		 }

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

 const resetGame = async () => {
	 try {
		 const response = await fetch('/api/associations/reset', { method: 'POST' });
		 if (!response.ok) throw new Error(`Unable to reset the game (${response.status}).`);
		 const stateResponse = await fetch('/api/associations');
		 applyState(await readResponse(stateResponse));
		 message.value = '';
		 error.value = false;
	 } catch (exception) {
		 message.value = exception.message;
		 error.value = true;
	 }
 };

 onMounted(async () => {
	 try {
		 const response = await fetch('/api/associations');
		 const state = await readResponse(response);
		 if (!response.ok) throw new Error(`Unable to load the game (${response.status}).`);
		 applyState(state);
	 } catch (exception) {
		 message.value = exception.message;
		 error.value = true;
	 }
 });
 </script>

 <style scoped>
 .game-page {
	 position: relative;
	 min-height: 100vh;
	 padding: 2.5rem 2rem 4rem;
	 font-family: var(--lw-font, system-ui, sans-serif);
	 color: #1c2b45;
	 background: radial-gradient(circle at 50% 20%, #f4f5ff 0, #e9edff 45%, #dfe4fb 100%);
 }

 .game-header { margin: 2rem auto; text-align: center; }
 .eyebrow { margin: 0 0 0.5rem; color: #4254a4; font-size: 0.75rem; font-weight: 800; letter-spacing: 0.12em; text-transform: uppercase; }
 h1 { margin: 0; font-size: clamp(2rem, 5vw, 3.25rem); letter-spacing: -0.03em; }
 .game-header p:last-child { margin: 0.65rem 0 0; color: #65709d; }
 .board-shell { width: min(92vw, 42rem); margin: 0 auto; padding: 1.5rem; background: rgba(255, 255, 255, 0.7); border: 1px solid rgba(66, 84, 164, 0.22); border-radius: 10px; box-shadow: 0 12px 32px rgba(57, 68, 132, 0.12); }
 .solved-groups { display: grid; gap: 0.6rem; margin-bottom: 0.75rem; }
.revealed-groups { display: grid; gap: 0.6rem; margin-top: 1rem; }
.revealed-title { margin: 0 0 0.1rem; color: #65709d; font-size: 0.8rem; font-weight: 800; text-transform: uppercase; }
.group-bar { padding: 0.75rem 1rem; color: #263557; border-radius: 0.5rem; }
.group-color-0 { background: #c7e6df; }
.group-color-1 { background: #f5d2a7; }
.group-color-2 { background: #d8d0ed; }
.group-color-3 { background: #f0c7c7; }
 .group-bar strong { display: block; font-size: 1rem; }
 .group-bar span { display: block; margin-top: 0.2rem; font-size: 0.78rem; opacity: 0.9; }
 .game-grid { display: grid; grid-template-columns: repeat(4, minmax(3rem, 1fr)); gap: 0.75rem; }
 .word-box { min-height: 4.5rem; padding: 0.5rem; color: #263557; border: 2px solid #aab4e3; border-radius: 0.75rem; background: rgba(255, 255, 255, 0.88); font: inherit; font-weight: 800; cursor: pointer; transition: 120ms ease; }
 .word-box:hover, .word-box:focus-visible { border-color: #4254a4; outline: 3px solid rgba(66, 84, 164, 0.18); }
 .word-box.selected { color: #fff; border-color: #293b82; background: #4254a4; }
 .word-box:disabled { cursor: default; }
 .controls { margin-top: 1.25rem; text-align: center; }
.status { min-height: 1.4rem; margin: 0 0 0.35rem; color: #38508f; font-weight: 700; }
 .status.error { color: #a33b50; }
.attempts { display: flex; justify-content: center; gap: 0.55rem; margin-bottom: 1rem; }
.attempt-icon { width: 0.8rem; height: 0.8rem; border: 2px solid #5265b5; border-radius: 50%; background: #d8d0ed; }
.attempt-icon.used { border-color: #aab6c6; background: transparent; opacity: 0.45; }
.failure-banner { display: grid; gap: 0.2rem; margin-bottom: 1rem; padding: 0.9rem 1rem; color: #713746; border: 1px solid #e4a1ad; border-left: 5px solid #c85b70; border-radius: 6px; background: #fff0f2; text-align: center; }
.failure-banner strong { font-size: 1rem; }
.failure-banner span { font-size: 0.85rem; }
 .submit-button, .reset-button { padding: 0.65rem 1rem; border: 1px solid #4254a4; border-radius: 6px; font: inherit; font-weight: 700; cursor: pointer; }
 .submit-button { color: #fff; background: #4254a4; }
 .submit-button:disabled { opacity: 0.45; cursor: not-allowed; }
 .reset-button { margin-left: 0.5rem; color: #1c2b45; background: #fff; }
 .back-button { position: absolute; top: 2rem; left: 2rem; padding: 0.5rem 0.75rem; color: #1c2b45; border: 1px solid #aab4e3; border-radius: 6px; text-decoration: none; background: #fff; }
 .back-button:hover, .back-button:focus-visible { border-color: #4254a4; outline: 3px solid rgba(66, 84, 164, 0.18); }
 @media (max-width: 520px) { .game-page { padding-inline: 1rem; } .back-button { position: static; display: inline-block; } .game-header { margin-top: 1.5rem; } .board-shell { padding: 1rem; } .game-grid { gap: 0.45rem; } .word-box { min-height: 4rem; font-size: 0.8rem; } }
 </style>
