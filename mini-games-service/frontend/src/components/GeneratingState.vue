<template>
	<div class="generating-state" role="status" aria-live="polite">
		<div class="generating-state__spinner" aria-hidden="true"></div>
		<p class="generating-state__title">{{ title }}</p>
		<p class="generating-state__hint">Generating fresh words for this round — this can take a moment. If this fails, please try again soon</p>
	</div>
</template>
<script setup>
defineProps({
	/** Heading shown under the spinner. */
	title: { type: String, default: 'Preparing your game…' }
});
</script>
