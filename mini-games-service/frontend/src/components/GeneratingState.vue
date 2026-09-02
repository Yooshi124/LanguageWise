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

<style scoped>
.generating-state {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 0.5rem;
	min-height: 16rem;
	padding: 2.5rem 1.5rem;
	text-align: center;
}

.generating-state__spinner {
	width: 2.5rem;
	height: 2.5rem;
	border: 4px solid rgba(79, 70, 229, 0.18);
	border-top-color: #4f46e5;
	border-radius: 50%;
	animation: generating-spin 0.8s linear infinite;
}

.generating-state__title {
	margin: 0.5rem 0 0;
	font-weight: 800;
	font-size: 1.05rem;
	color: #1c2b45;
}

.generating-state__hint {
	margin: 0;
	max-width: 26rem;
	color: #667085;
	font-size: 0.9rem;
	line-height: 1.55;
}

@keyframes generating-spin {
	to { transform: rotate(360deg); }
}
</style>
