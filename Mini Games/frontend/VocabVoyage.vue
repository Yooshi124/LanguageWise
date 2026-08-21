<template>
	<main class="vocab-voyage">
		<h1>VocabVoyage</h1>
		<section class="game-grid" aria-label="VocabVoyage game board">
			<div v-for="box in boxes" :key="box" class="grid-box"></div>
		</section>
	</main>
</template>

<script setup>
const boxes = Array.from({ length: 30 }, (_, index) => index + 1);
</script>

<style scoped>
.vocab-voyage {
	min-height: 100vh;
	padding: 2rem;
	font-family: Arial, sans-serif;
	background: #f4f4f4;
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
	border: 2px solid #333;
	background: #fff;
}
</style>