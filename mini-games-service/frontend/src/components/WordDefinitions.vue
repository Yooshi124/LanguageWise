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
