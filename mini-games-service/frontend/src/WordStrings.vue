<template>
	<main class="game-page">
		<a class="back-button" href="/" aria-label="Return to the main game page">Back</a>
		<header class="game-header">
			<p class="eyebrow">Build the chain</p>
			<h1>Word Strings</h1>
			<p>Follow the letters and uncover the next word.</p>
		</header>
		<div class="game-layout">
			<section class="board-shell" aria-label="Word Strings game board">
				<div class="game-grid">
					<div v-for="box in boxes" :key="box" class="grid-box"></div>
				</div>
			</section>

			<aside class="hint-box">
				<span class="hint-label">A little help</span>
				<h2>Hint</h2>
				<p>Your hint will appear here.</p>
			</aside>
		</div>
	</main>
</template>

<script setup>
const boxes = Array.from({ length: 48 }, (_, index) => index + 1);
</script>

<style scoped>
.game-page {
	position: relative;
	min-height: 100vh;
	padding: 2rem;
	font-family: var(--lw-font, system-ui, sans-serif);
	color: #1c2b45;
	background: radial-gradient(circle at 50% 20%, #fffaf0 0, #fff1dc 45%, #f4e4cb 100%);
}

.game-header {
	margin: 2rem auto;
	text-align: center;
}

.eyebrow {
	margin: 0 0 0.5rem;
	color: #bd6a25;
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
	color: #806f5d;
}

.game-layout {
	display: flex;
	align-items: center;
	justify-content: center;
	gap: clamp(1.5rem, 5vw, 4rem);
	margin: 0 auto;
}

.board-shell {
	padding: 1.25rem;
	background: rgba(255, 255, 255, 0.7);
	border: 1px solid rgba(189, 106, 37, 0.24);
	border-radius: 10px;
	box-shadow: 0 12px 32px rgba(118, 77, 31, 0.12);
}

.game-grid {
	display: grid;
	grid-template-columns: repeat(6, minmax(2rem, 1fr));
	grid-template-rows: repeat(8, minmax(2rem, 1fr));
	gap: 0.5rem;
	width: min(58vw, 28rem);
	aspect-ratio: 6 / 8;
}

.grid-box {
	border: 2px solid #d5b88f;
	border-radius: 4px;
	background: rgba(255, 255, 255, 0.88);
}

.hint-box {
	width: min(22vw, 10rem);
	min-width: 8rem;
	padding: 1rem;
	border: 1px solid #d9b783;
	border-radius: 10px;
	background: rgba(255, 255, 255, 0.82);
	box-shadow: 0 8px 20px rgba(118, 77, 31, 0.1);
}

.hint-label {
	color: #bd6a25;
	font-size: 0.72rem;
	font-weight: 800;
	letter-spacing: 0.1em;
	text-transform: uppercase;
}

.hint-box h2 {
	margin: 0 0 0.75rem;
	font-size: 1.1rem;
}

.hint-box p {
	margin: 0;
}

.back-button {
	position: absolute;
	top: 2rem;
	left: 2rem;
	padding: 0.5rem 0.75rem;
	color: #1c2b45;
	border: 1px solid #d9b783;
	border-radius: 6px;
	text-decoration: none;
	background: #fff;
}

.back-button:hover,
.back-button:focus-visible {
	border-color: #d87832;
	outline: 3px solid rgba(216, 120, 50, 0.18);
}

@media (max-width: 40rem) {
	.game-layout {
		flex-direction: column;
	}

	.game-grid {
		width: min(82vw, 28rem);
	}

	.hint-box {
		width: min(70vw, 16rem);
	}
}
</style>
