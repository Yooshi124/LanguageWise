<script setup>
import { ref } from 'vue';
import { api } from '../api.js';

const props = defineProps({
    targetId: { type: Number, required: true },
    kind: { type: String, required: true, validator: (value) => ['post', 'comment'].includes(value) },
    liked: { type: Boolean, required: true },
    count: { type: Number, required: true }
});

const emit = defineEmits(['update', 'error']);

const busy = ref(false);

async function toggle() {
    if (busy.value) {
        return;
    }

    const wasLiked = props.liked;
    const wasCount = props.count;

    busy.value = true;
    emit('update', { liked: !wasLiked, count: Math.max(0, wasCount + (wasLiked ? -1 : 1)) });

    try {
        if (wasLiked) {
            await unlike();
        } else {
            await like();
        }
    } catch (error) {
        const alreadyAgreed = wasLiked ? error.isNotFound : error.status === 409;

        if (!alreadyAgreed) {
            emit('update', { liked: wasLiked, count: wasCount });
            emit('error', error);
        }
    } finally {
        busy.value = false;
    }
}

function like() {
    return props.kind === 'post' ? api.likePost(props.targetId) : api.likeComment(props.targetId);
}

function unlike() {
    return props.kind === 'post' ? api.unlikePost(props.targetId) : api.unlikeComment(props.targetId);
}
</script>

<template>
    <button
        type="button"
        class="cd-like"
        :class="{ 'cd-like--on': liked }"
        :aria-pressed="liked"
        :title="liked ? 'Remove your like' : 'Like this'"
        @click="toggle"
    >
        <span aria-hidden="true">{{ liked ? '♥' : '♡' }}</span>
        <span>{{ count }}</span>
        <span class="cd-like__label">{{ count === 1 ? 'like' : 'likes' }}</span>
    </button>
</template>

<style scoped>
.cd-like {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    padding: 0.3rem 0.7rem;
    border: 1px solid var(--lw-colour-border);
    border-radius: 999px;
    background: var(--lw-colour-surface);
    color: var(--lw-colour-ink-muted);
    font-family: var(--lw-font);
    font-size: 0.9rem;
    cursor: pointer;
}

.cd-like:hover {
    border-color: var(--lw-colour-primary);
    color: var(--lw-colour-primary);
}

.cd-like--on {
    border-color: var(--lw-colour-danger);
    color: var(--lw-colour-danger);
}

.cd-like__label {
    color: inherit;
}
</style>
