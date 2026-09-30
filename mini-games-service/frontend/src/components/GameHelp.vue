<script setup>
import { useId } from 'vue';
import AppIcon from './AppIcon.vue';

defineProps({
	// Short bullet points explaining how to play the game.
	steps: { type: Array, required: true },
});

const tooltipId = useId();
</script>

<template>
	<!-- The popout is purely hover/focus driven: hidden until the user hovers
	     or keyboard-focuses the icon. -->
	<div class="game-help">
		<button type="button" class="game-help__trigger" aria-label="How to play" :aria-describedby="tooltipId">
			<AppIcon name="help" :size="22" />
		</button>
		<div :id="tooltipId" class="game-help__popout" role="tooltip">
			<strong class="game-help__title">How to play</strong>
			<ul class="game-help__steps">
				<li v-for="(step, index) in steps" :key="index">{{ step }}</li>
			</ul>
		</div>
	</div>
</template>
