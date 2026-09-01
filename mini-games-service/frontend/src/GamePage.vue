<template>
	<main class="games-home">
		<div class="games-home__container">
			<div class="games-hero">
				<span class="games-chip">Train a little every day</span>
				<h1>Pick a game.<br /><span>Make it stick.</span></h1>
				<p>Three quick vocabulary workouts built from the words you have already unlocked in your courses.</p>
			</div>

			<ul class="game-list">
				<li v-for="game in games" :key="game.name">
					<a class="game-card" :class="`game-card--${game.theme}`" :href="game.link">
						<div class="game-card__preview" :class="`game-card__preview--${game.theme}`" aria-hidden="true">
							<div v-if="game.theme === 'vocab'" class="vocab-preview">
								<span v-for="cell in 10" :key="cell" class="vocab-preview__cell" :class="{ 'is-filled': cell === 2 || cell === 7 }"></span>
							</div>
							<div v-else-if="game.theme === 'strings'" class="strings-preview">
								<span v-for="cell in 48" :key="cell" class="strings-preview__cell" :class="{ 'is-filled': cell === 5 || cell === 18 || cell === 31 || cell === 44 }"></span>
							</div>
							<div v-else class="associations-preview">
								<span v-for="cell in 16" :key="cell" class="associations-preview__cell" :class="{ 'is-filled': cell === 6 || cell === 11 }"></span>
							</div>
						</div>

						<div class="game-card__body">
							<span class="game-card__meta">Game {{ String(game.id).padStart(2, '0') }}</span>
							<h2>{{ game.name }}</h2>
							<p>{{ game.description }}</p>
							<span class="game-card__action">
								<span class="game-card__action-content">
									Play now <span aria-hidden="true">&#8594;</span>
								</span>
							</span>
						</div>
					</a>
				</li>
			</ul>
		</div>
	</main>
</template>

<script setup>
// Relative links resolve correctly both under the gateway base (/mini-games/)
// and when served at the root in local dev.
const games = [
	{
		id: 1,
		name: 'Guess the word',
		link: 'game/guess-the-word',
		theme: 'vocab',
		description: 'Guess the word, build your streak, and sharpen your language instincts.'
	},
	{
		id: 2,
		name: 'Word Search',
		link: 'game/word-search',
		theme: 'strings',
		description: 'Follow the chain of letters and discover the words hiding in plain sight.'
	},
	{
		id: 3,
		name: 'Associations',
		link: 'game/associations',
		theme: 'associations',
		description: 'Connect ideas, spot patterns, and make vocabulary stick together.'
	}
];
</script>

<style scoped>
.games-home {
	min-height: 100vh;
	background: radial-gradient(circle at 85% 8%, #e0e7ff 0, transparent 30%), #f6f7fb;
}

.games-home__container {
	padding: 38px clamp(24px, 3.5vw, 50px) 42px;
}

.games-hero {
	max-width: 780px;
}

.games-chip {
	display: inline-block;
	margin-bottom: 20px;
	padding: 6px 14px;
	border-radius: 999px;
	color: #4338ca;
	background: #eef2ff;
	font-size: 0.82rem;
	font-weight: 650;
}

.games-hero h1 {
	margin: 0;
	font-size: clamp(2.8rem, 6vw, 5rem);
	font-weight: 800;
	line-height: 1.03;
	letter-spacing: -0.055em;
}

.games-hero h1 span {
	color: #4f46e5;
}

.games-hero p {
	max-width: 630px;
	margin: 24px 0 0;
	color: #667085;
	font-size: 1.2rem;
	line-height: 1.7;
}

.game-list {
	display: grid;
	grid-template-columns: repeat(3, minmax(0, 1fr));
	gap: 20px;
	margin: 42px 0 0;
	padding: 0;
	list-style: none;
}

.game-list li {
	min-width: 0;
}

.game-card {
	position: relative;
	display: flex;
	height: 100%;
	flex-direction: column;
	overflow: hidden;
	border: 1px solid #e7e9f0;
	border-radius: 24px;
	color: inherit;
	background: white;
	text-decoration: none;
	transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.game-card:hover,
.game-card:focus-visible {
	box-shadow: 0 18px 40px rgba(31, 41, 55, 0.1);
	transform: translateY(-6px);
}

.game-card:focus-visible {
	outline: 3px solid rgba(79, 70, 229, 0.28);
	outline-offset: 3px;
}

.game-card__preview {
	display: grid;
	min-height: 13rem;
	padding: 1.5rem;
	place-items: center;
}

.game-card__preview--vocab {
	background: #e6f3f2;
}

.game-card__preview--strings {
	background: #fff1dc;
}

.game-card__preview--associations {
	background: #e9edff;
}

.vocab-preview {
	display: grid;
	grid-template-columns: repeat(5, 2.25rem);
	grid-template-rows: repeat(2, 2.25rem);
	gap: 0.4rem;
}

.vocab-preview__cell {
	border: 2px solid #1c2b45;
	background: rgba(255, 255, 255, 0.7);
}

.vocab-preview__cell.is-filled {
	border-color: #10897a;
	background: #10897a;
}

.strings-preview {
	display: grid;
	grid-template-columns: repeat(6, 1.55rem);
	grid-template-rows: repeat(8, 1.55rem);
	gap: 0.35rem;
	transform: rotate(-3deg);
}

.strings-preview__cell {
	border: 2px solid #9d6a28;
	background: rgba(255, 255, 255, 0.75);
}

.strings-preview__cell.is-filled {
	border-color: #d87832;
	background: #d87832;
}

.associations-preview {
	display: grid;
	grid-template-columns: repeat(4, 3.1rem);
	grid-template-rows: repeat(4, 3.1rem);
	gap: 0.45rem;
}

.associations-preview__cell {
	border: 2px solid #4254a4;
	border-radius: 0.7rem;
	background: rgba(255, 255, 255, 0.72);
}

.associations-preview__cell.is-filled {
	border-color: #4254a4;
	background: #4254a4;
}

.game-card__body {
	display: flex;
	flex: 1;
	flex-direction: column;
	padding: 24px;
}

.game-card__meta {
	color: #6366f1;
	font-size: 0.78rem;
	font-weight: 800;
	letter-spacing: 0.08em;
	text-transform: uppercase;
}

/* Each card picks up the accent colour of the game page it links to. */
.game-card--vocab .game-card__meta,
.game-card--vocab .game-card__action-content {
	color: #10897a;
}

.game-card--strings .game-card__meta,
.game-card--strings .game-card__action-content {
	color: #b45309;
}

.game-card--associations .game-card__meta,
.game-card--associations .game-card__action-content {
	color: #4254a4;
}

.game-card h2 {
	margin: 14px 0 8px;
	font-size: 1.5rem;
}

.game-card__body p {
	margin: 0;
	min-height: 48px;
	color: #667085;
	line-height: 1.5;
}

.game-card__action {
	display: flex;
	margin-top: 26px;
	align-items: center;
	justify-content: space-between;
	font-weight: 700;
}

.game-card__action-content {
	display: inline-flex;
	width: 100%;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
}

.game-card__action-content span {
	display: inline-block;
	transition: transform 0.16s ease;
}

.game-card:hover .game-card__action-content span,
.game-card:focus-visible .game-card__action-content span {
	transform: translateX(0.25rem);
}

@media (max-width: 1100px) {
	.game-list {
		grid-template-columns: repeat(2, minmax(0, 1fr));
	}
}

@media (max-width: 600px) {
	.games-hero h1 {
		font-size: 2.7rem;
	}

	.game-list {
		grid-template-columns: minmax(0, 1fr);
	}

	.game-card__preview {
		min-height: 11rem;
	}
}
</style>