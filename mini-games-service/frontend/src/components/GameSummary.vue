<template>
	<Teleport to="body">
		<div v-if="visible" class="summary-overlay" role="dialog" aria-modal="true" :aria-label="`${gameName} results`" @click.self="close" @keydown="trapFocus">
			<div ref="cardRef" class="summary-card">
				<header class="summary-card__header" :class="{ 'is-won': won }">
					<h2>{{ won ? 'You won!' : 'Game over' }}</h2>
					<button ref="closeButtonRef" type="button" class="summary-card__close" aria-label="Close results" @click="close">&times;</button>
				</header>
				<p class="summary-card__subtitle">{{ gameName }}</p>
				<dl v-if="displayedStats.length" class="summary-stats">
					<div v-for="stat in displayedStats" :key="stat.label" class="summary-stats__item">
						<dt>{{ stat.label }}</dt>
						<dd>{{ stat.value }}</dd>
					</div>
				</dl>
				<div class="summary-actions">
					<button type="button" class="summary-actions__share" :disabled="!shareText" @click="share">
						<AppIcon name="share" :size="16" />
						{{ shareStatusLabel }}
					</button>
					<button type="button" class="summary-actions__again" @click="playAgain">Play again</button>
				</div>
			</div>
		</div>
	</Teleport>
</template>

<script setup>
import { computed, ref, watch } from 'vue';
import AppIcon from './AppIcon.vue';
import { useModalFocusTrap } from '../composables/useModalFocusTrap.js';

const props = defineProps({
	/** Whether the summary popup is shown. */
	visible: { type: Boolean, default: false },
	/** Whether the round ended in a win. */
	won: { type: Boolean, default: false },
	/** Display name of the game, e.g. "Guess the Word". */
	gameName: { type: String, required: true },
	/** Seconds the round took; omit to hide the time stat. */
	elapsedSeconds: { type: Number, default: null },
	/** Extra stat rows to show, e.g. [{ label: 'Guesses', value: '4 / 6' }]. */
	stats: { type: Array, default: () => [] },
	/** Plain-text result summary offered for sharing/copying; omit to disable the share button. */
	shareText: { type: String, default: '' }
});

const emit = defineEmits(['close', 'play-again']);

const closeButtonRef = ref(null);
const shareState = ref('idle'); // idle | copied | shared | error

function close() {
	emit('close');
}

function playAgain() {
	emit('play-again');
}

const visible = computed(() => props.visible);
const { containerRef: cardRef, trapFocus } = useModalFocusTrap(visible, close, closeButtonRef);

watch(visible, (isVisible) => {
	if (isVisible) {
		shareState.value = 'idle';
	}
});

const formattedTime = computed(() => {
	if (props.elapsedSeconds === null || props.elapsedSeconds === undefined) return null;
	const minutes = Math.floor(props.elapsedSeconds / 60);
	const seconds = props.elapsedSeconds % 60;
	return minutes > 0 ? `${minutes}:${String(seconds).padStart(2, '0')}` : `${seconds}s`;
});

const displayedStats = computed(() => {
	const items = [...props.stats];
	if (formattedTime.value) {
		items.push({ label: 'Time', value: formattedTime.value });
	}
	return items;
});

const shareStatusLabel = computed(() => {
	switch (shareState.value) {
		case 'copied': return 'Copied!';
		case 'shared': return 'Shared';
		case 'error': return 'Could not share';
		default: return 'Share result';
	}
});

// Uses the native share sheet when available (mobile browsers), otherwise falls back to
// copying the summary text to the clipboard.
async function share() {
	if (!props.shareText) return;
	try {
		if (navigator.share) {
			await navigator.share({ text: props.shareText });
			shareState.value = 'shared';
		} else if (navigator.clipboard?.writeText) {
			await navigator.clipboard.writeText(props.shareText);
			shareState.value = 'copied';
		} else {
			shareState.value = 'error';
		}
	} catch (error) {
		// The user cancelling the native share sheet is not a failure worth reporting.
		if (error?.name === 'AbortError') return;
		shareState.value = 'error';
		return;
	}
	window.setTimeout(() => {
		shareState.value = 'idle';
	}, 2500);
}
</script>
