<template>
	<main class="game-page lw-shell">
		<section class="intro" aria-labelledby="games-title">
			<p class="eyebrow">Choose your challenge</p>
			<h1 id="games-title">Learn by playing.</h1>
			<p class="intro__copy">Short vocabulary games for a sharper memory and a little friendly competition.</p>
		</section>

		<ul class="game-list">
			<li v-for="game in games" :key="game.name" class="game-card">
				<a class="game-card__link" :href="game.link">
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
						<div class="game-card__heading">
							<span class="game-card__number">0{{ game.id }}</span>
							<h2>{{ game.name }}</h2>
						</div>
						<p>{{ game.description }}</p>
						<span class="play-link">Play now <span aria-hidden="true">&#8594;</span></span>
					</div>
				</a>
			</li>
		</ul>
	</main>
</template>

<script setup>
const games = [
	{
		id: 1,
		name: 'VocabVoyage',
		link: '/vocab-voyage',
		theme: 'vocab',
		description: 'Guess the word, build your streak, and sharpen your language instincts.'
	},
	{
		id: 2,
		name: 'Word Strings',
		link: '/word-strings',
		theme: 'strings',
		description: 'Follow the chain of letters and discover the words hiding in plain sight.'
	},
	{
		id: 3,
		name: 'Associations',
		link: '/associations',
		theme: 'associations',
		description: 'Connect ideas, spot patterns, and make vocabulary stick together.'
	}
];
</script>

<style scoped>
.game-page {
	--page-ink: #1c2b45;
	--page-muted: #5b6b85;
	--page-border: #d9e0ee;
	--page-surface: #fff;
	padding-top: 3.5rem;
	padding-bottom: 4rem;
}

.intro {
	max-width: 42rem;
	margin-bottom: 2.5rem;
}

.eyebrow {
	margin: 0 0 0.75rem;
	color: #10897a;
	font-size: 0.78rem;
	font-weight: 700;
	letter-spacing: 0.12em;
	text-transform: uppercase;
}

.intro h1 {
	margin: 0;
	color: var(--page-ink);
	font-size: clamp(2.5rem, 7vw, 4.5rem);
	font-weight: 800;
	letter-spacing: -0.04em;
	line-height: 0.98;
}

.intro__copy {
	max-width: 32rem;
	margin: 1.25rem 0 0;
	color: var(--page-muted);
	font-size: 1.1rem;
}

.game-list {
	display: grid;
	grid-template-columns: repeat(3, minmax(0, 1fr));
	gap: 1.25rem;
	margin: 0;
	padding: 0;
	list-style: none;
}

.game-card {
	min-width: 0;
}

.game-card__link {
	display: flex;
	height: 100%;
	flex-direction: column;
	color: inherit;
	text-decoration: none;
	background: var(--page-surface);
	border: 1px solid var(--page-border);
	border-radius: 10px;
	box-shadow: 0 1px 3px rgba(28, 43, 69, 0.08), 0 8px 24px rgba(28, 43, 69, 0.06);
	overflow: hidden;
	transition: transform 160ms ease, box-shadow 160ms ease, border-color 160ms ease;
}

.game-card__link:hover,
.game-card__link:focus-visible {
	border-color: #1f6feb;
	box-shadow: 0 4px 8px rgba(28, 43, 69, 0.1), 0 14px 30px rgba(28, 43, 69, 0.1);
	transform: translateY(-4px);
}

.game-card__link:focus-visible {
	outline: 3px solid rgba(31, 111, 235, 0.28);
	outline-offset: 3px;
}

.game-card__preview {
	display: grid;
	place-items: center;
	min-height: 14rem;
	padding: 1.5rem;
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

.vocab-preview__cell,
.strings-preview__cell {
	border: 2px solid #1c2b45;
	background: rgba(255, 255, 255, 0.7);
}

.vocab-preview__cell.is-filled,
.strings-preview__cell.is-filled {
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
	border-color: #9d6a28;
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
	padding: 1.35rem 1.5rem 1.5rem;
}

.game-card__heading {
	display: flex;
	align-items: baseline;
	gap: 0.7rem;
}

.game-card__number {
	color: #10897a;
	font-size: 0.75rem;
	font-weight: 800;
	letter-spacing: 0.08em;
}

.game-card h2 {
	margin: 0;
	color: var(--page-ink);
	font-size: 1.35rem;
}

.game-card__body p {
	margin: 0.75rem 0 1.5rem;
	color: var(--page-muted);
	font-size: 0.92rem;
}

.play-link {
	margin-top: auto;
	color: #1f6feb;
	font-size: 0.9rem;
	font-weight: 700;
}

.play-link span {
	display: inline-block;
	margin-left: 0.25rem;
	transition: transform 160ms ease;
}

.game-card__link:hover .play-link span,
.game-card__link:focus-visible .play-link span {
	transform: translateX(0.25rem);
}

@media (max-width: 52rem) {
	.game-list {
		grid-template-columns: 1fr;
	}

	.game-card__link {
		flex-direction: row;
	}

	.game-card__preview {
		width: 42%;
		min-height: 13rem;
	}
}

@media (max-width: 36rem) {
	.game-page {
		padding-top: 2rem;
	}

	.game-card__link {
		flex-direction: column;
	}

	.game-card__preview {
		width: 100%;
		min-height: 12rem;
	}
}
</style>