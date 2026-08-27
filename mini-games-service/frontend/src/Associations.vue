<template>
	<main class="game-page">
		<a class="back-button" href="/" aria-label="Return to the main game page">Back</a>
		<header class="game-header">
			<p class="eyebrow">Make the connection</p>
			<h1>Associations</h1>
			<p>Find the words that belong together.</p>
		</header>
		<section class="board-shell" aria-label="Associations game board">
			<div class="game-grid">
				<div v-for="box in boxes" :key="box" class="grid-box"></div>
			</div>
		</section>
	</main>
</template>

<script setup>
const boxes = Array.from({ length: 16 }, (_, index) => index + 1);
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

.game-header {
	margin: 2rem auto 2rem;
	text-align: center;
}

.eyebrow {
	margin: 0 0 0.5rem;
	color: #4254a4;
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
	color: #65709d;
}

.board-shell {
	width: min(80vw, 35rem);
	margin: 0 auto;
	padding: 1.5rem;
	background: rgba(255, 255, 255, 0.7);
	border: 1px solid rgba(66, 84, 164, 0.22);
	border-radius: 10px;
	box-shadow: 0 12px 32px rgba(57, 68, 132, 0.12);
}

.game-grid {
	display: grid;
	grid-template-columns: repeat(4, minmax(3rem, 1fr));
	grid-template-rows: repeat(4, minmax(3rem, 1fr));
	gap: 0.75rem;
	width: min(80vw, 32rem);
	aspect-ratio: 1;
	margin: 0 auto;
}

.grid-box {
	border: 2px solid #aab4e3;
	border-radius: 0.75rem;
	background: rgba(255, 255, 255, 0.88);
}

.back-button {
	position: absolute;
	top: 2rem;
	left: 2rem;
	padding: 0.5rem 0.75rem;
	color: #1c2b45;
	border: 1px solid #aab4e3;
	border-radius: 6px;
	text-decoration: none;
	background: #fff;
}

.back-button:hover,
.back-button:focus-visible {
	border-color: #4254a4;
	outline: 3px solid rgba(66, 84, 164, 0.18);
}
</style>
