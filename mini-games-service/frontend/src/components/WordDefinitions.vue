<template>
	<Teleport to="body">
		<div v-if="visible" class="definitions-overlay" role="dialog" aria-modal="true" aria-label="Word definitions" @click.self="close">
			<div class="definitions-card">
				<header class="definitions-card__header">
					<h2>Words from this round</h2>
					<button type="button" class="definitions-card__close" aria-label="Close definitions" @click="close">&times;</button>
				</header>
				<ul class="definitions-list">
					<li v-for="(definition, word) in definitions" :key="word" class="definitions-list__item">
						<strong class="definitions-list__word">{{ word }}</strong>
						<span class="definitions-list__definition">{{ definition }}</span>
					</li>
				</ul>
				<p v-if="empty" class="definitions-empty">No definitions were saved for this round.</p>
			</div>
		</div>
	</Teleport>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
	/** Map of word -> definition; only the entries shown. */
	definitions: { type: Object, default: null },
	/** Whether the popup is shown. */
	visible: { type: Boolean, default: false }
});

const emit = defineEmits(['close']);

const empty = computed(() => !props.definitions || Object.keys(props.definitions).length === 0);

function close() {
	emit('close');
}
</script>

<style scoped>
.definitions-overlay {
	position: fixed;
	inset: 0;
	z-index: 60;
	display: grid;
	place-items: center;
	padding: 1.5rem;
	background: rgba(15, 23, 42, 0.5);
	backdrop-filter: blur(3px);
}

.definitions-card {
	width: min(92vw, 30rem);
	max-height: min(80vh, 34rem);
	display: flex;
	flex-direction: column;
	background: #fff;
	border-radius: 14px;
	box-shadow: 0 24px 60px rgba(15, 23, 42, 0.28);
	overflow: hidden;
}

.definitions-card__header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 1rem 1.25rem;
	border-bottom: 1px solid #ececf1;
}

.definitions-card__header h2 {
	margin: 0;
	font-size: 1.05rem;
	font-weight: 800;
	color: #1c2b45;
}

.definitions-card__close {
	border: 0;
	background: transparent;
	font-size: 1.5rem;
	line-height: 1;
	color: #667085;
	cursor: pointer;
}

.definitions-list {
	margin: 0;
	padding: 0.5rem 1.25rem 1.25rem;
	list-style: none;
	overflow-y: auto;
}

.definitions-list__item {
	display: flex;
	gap: 0.75rem;
	align-items: baseline;
	padding: 0.55rem 0;
	border-bottom: 1px solid #f1f2f6;
}

.definitions-list__item:last-child {
	border-bottom: 0;
}

.definitions-list__word {
	min-width: 6.5rem;
	font-weight: 800;
	color: #4338ca;
	text-transform: uppercase;
	letter-spacing: 0.02em;
}

.definitions-list__definition {
	color: #475467;
	font-size: 0.92rem;
	line-height: 1.45;
}

.definitions-empty {
	margin: 0;
	padding: 1rem 1.25rem 1.5rem;
	color: #98a2b3;
}
</style>
