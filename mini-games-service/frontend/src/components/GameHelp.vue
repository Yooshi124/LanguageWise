<script setup>
import AppIcon from './AppIcon.vue';

defineProps({
	// Short bullet points explaining how to play the game.
	steps: { type: Array, required: true },
});
</script>

<template>
	<!-- The popout is purely hover/focus driven: hidden until the user hovers
	     or keyboard-focuses the icon. -->
	<div class="game-help">
		<button type="button" class="game-help__trigger" aria-label="How to play">
			<AppIcon name="help" :size="22" />
		</button>
		<div class="game-help__popout" role="tooltip">
			<strong class="game-help__title">How to play</strong>
			<ul class="game-help__steps">
				<li v-for="(step, index) in steps" :key="index">{{ step }}</li>
			</ul>
		</div>
	</div>
</template>

<style scoped>
.game-help {
	position: absolute;
	top: 2rem;
	right: 2rem;
}

.game-help__trigger {
	display: grid;
	width: 2.25rem;
	height: 2.25rem;
	padding: 0;
	place-items: center;
	border: 1px solid rgba(28, 43, 69, 0.25);
	border-radius: 50%;
	color: #1c2b45;
	background: rgba(255, 255, 255, 0.85);
	cursor: help;
	transition: border-color 120ms ease, background 120ms ease;
}

.game-help:hover .game-help__trigger,
.game-help__trigger:focus-visible {
	border-color: currentColor;
	background: #ffffff;
	outline: none;
}

.game-help__popout {
	position: absolute;
	top: calc(100% + 0.6rem);
	right: 0;
	z-index: 20;
	width: min(19rem, calc(100vw - 3rem));
	padding: 0.9rem 1.1rem;
	border: 1px solid rgba(28, 43, 69, 0.18);
	border-radius: 10px;
	background: #ffffff;
	box-shadow: 0 12px 32px rgba(28, 43, 69, 0.18);
	text-align: left;
	opacity: 0;
	visibility: hidden;
	transform: translateY(-4px);
	transition: opacity 140ms ease, transform 140ms ease, visibility 140ms ease;
	pointer-events: none;
}

.game-help:hover .game-help__popout,
.game-help:focus-within .game-help__popout {
	opacity: 1;
	visibility: visible;
	transform: translateY(0);
}

.game-help__title {
	display: block;
	margin-bottom: 0.4rem;
	font-size: 0.8rem;
	letter-spacing: 0.08em;
	text-transform: uppercase;
}

.game-help__steps {
	display: grid;
	gap: 0.4rem;
	margin: 0;
	padding-left: 1.1rem;
	font-size: 0.85rem;
	font-weight: 400;
	line-height: 1.45;
}

@media (max-width: 520px) {
	.game-help {
		position: static;
		display: flex;
		justify-content: flex-end;
		margin-bottom: 0.5rem;
	}
}
</style>
