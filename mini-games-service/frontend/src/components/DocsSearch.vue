<template>
	<Teleport to="body">
		<div v-if="visible" class="docs-search-overlay" role="dialog" aria-modal="true" aria-label="Ask the docs" @click.self="close" @keydown="trapFocus">
			<div ref="cardRef" class="docs-search-card">
				<header class="docs-search-card__header">
					<h2>Ask the docs</h2>
					<button ref="closeButtonRef" type="button" class="docs-search-card__close" aria-label="Close docs search" @click="close">&times;</button>
				</header>

				<form class="docs-search-form" @submit.prevent="submit">
					<input
						v-model="query"
						type="search"
						class="docs-search-form__input"
						placeholder="How does the leaderboard work?"
						aria-label="Search the documentation"
					/>
					<button type="submit" class="docs-search-form__submit" :disabled="loading || !query.trim()">
						{{ loading ? 'Searching…' : 'Search' }}
					</button>
				</form>

				<p v-if="error" class="docs-search-error">{{ error }}</p>

				<ul v-else-if="results.length" class="docs-search-results">
					<li v-for="(result, index) in results" :key="`${result.source}-${index}`" class="docs-search-results__item">
						<span class="docs-search-results__source">{{ result.source }} — {{ result.heading }}</span>
						<p class="docs-search-results__text">{{ result.text }}</p>
					</li>
				</ul>

				<p v-else-if="searched && !loading" class="docs-search-empty">No matching documentation found.</p>
			</div>
		</div>
	</Teleport>
</template>

<script setup>
import { computed, ref, watch } from 'vue';
import { useModalFocusTrap } from '../composables/useModalFocusTrap.js';
import { queryRagDocs } from '../api.js';

const props = defineProps({
	/** Whether the popup is shown. */
	visible: { type: Boolean, default: false }
});

const emit = defineEmits(['close']);

const closeButtonRef = ref(null);
const query = ref('');
const results = ref([]);
const loading = ref(false);
const searched = ref(false);
const error = ref('');

function close() {
	emit('close');
}

const visible = computed(() => props.visible);
const { containerRef: cardRef, trapFocus } = useModalFocusTrap(visible, close, closeButtonRef);

watch(visible, (isVisible) => {
	if (isVisible) {
		query.value = '';
		results.value = [];
		searched.value = false;
		error.value = '';
	}
});

async function submit() {
	const trimmed = query.value.trim();
	if (!trimmed || loading.value) return;

	loading.value = true;
	error.value = '';
	try {
		const response = await queryRagDocs(trimmed);
		results.value = response?.results ?? [];
	} catch (caught) {
		results.value = [];
		error.value = caught?.message || 'Could not search the docs right now.';
	} finally {
		loading.value = false;
		searched.value = true;
	}
}
</script>
