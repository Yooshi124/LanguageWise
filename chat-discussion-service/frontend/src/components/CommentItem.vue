<script setup>
import { ref } from 'vue';
import LikeButton from './LikeButton.vue';
import { api } from '../api.js';
import { formatDate } from '../format.js';

const props = defineProps({
    comment: { type: Object, required: true },
    canEdit: { type: Boolean, default: false }
});

const emit = defineEmits(['update', 'deleted', 'error']);

const editing = ref(false);
const draft = ref('');
const saving = ref(false);

function startEditing() {
    draft.value = props.comment.content;
    editing.value = true;
}

async function save() {
    const content = draft.value.trim();

    if (!content || saving.value) {
        return;
    }

    saving.value = true;

    try {
        const updated = await api.updateComment(props.comment.id, content);
        emit('update', { ...props.comment, ...updated });
        editing.value = false;
    } catch (error) {
        emit('error', error);
    } finally {
        saving.value = false;
    }
}

async function remove() {
    if (!window.confirm('Delete this comment? This cannot be undone.')) {
        return;
    }

    try {
        await api.deleteComment(props.comment.id);
        emit('deleted', props.comment.id);
    } catch (error) {
        emit('error', error);
    }
}

function onLike({ liked, count }) {
    emit('update', { ...props.comment, likedByViewer: liked, likeCount: count });
}
</script>

<template>
    <li class="cd-comment">
        <p class="cd-comment__meta">
            <strong>{{ comment.authorName || 'Unknown author' }}</strong>
            <span aria-hidden="true">·</span>
            <span>{{ formatDate(comment.createdAt) }}</span>
            <span v-if="comment.updatedAt !== comment.createdAt" class="cd-comment__edited">(edited)</span>
        </p>

        <form v-if="editing" class="cd-comment__edit" @submit.prevent="save">
            <textarea v-model="draft" class="cd-comment__input" rows="3" required></textarea>
            <div class="lw-form-actions">
                <button type="submit" class="lw-command" :disabled="saving || !draft.trim()">
                    {{ saving ? 'Saving…' : 'Save' }}
                </button>
                <button type="button" class="lw-command" @click="editing = false">Cancel</button>
            </div>
        </form>

        <template v-else>
            <p class="cd-comment__body">{{ comment.content }}</p>

            <div class="cd-comment__actions">
                <LikeButton
                    kind="comment"
                    :target-id="comment.id"
                    :liked="comment.likedByViewer"
                    :count="comment.likeCount"
                    @update="onLike"
                    @error="$emit('error', $event)"
                />
                <template v-if="canEdit">
                    <button type="button" class="cd-comment__link" @click="startEditing">Edit</button>
                    <button type="button" class="cd-comment__link cd-comment__link--danger" @click="remove">
                        Delete
                    </button>
                </template>
            </div>
        </template>
    </li>
</template>

<style scoped>
.cd-comment {
    padding: 1rem 0;
    border-top: 1px solid var(--lw-colour-border);
    list-style: none;
}

.cd-comment__meta {
    display: flex;
    flex-wrap: wrap;
    gap: 0.4rem;
    margin: 0 0 0.4rem;
    color: var(--lw-colour-ink-muted);
    font-size: 0.85rem;
}

.cd-comment__meta strong {
    color: var(--lw-colour-ink);
}

.cd-comment__edited {
    font-style: italic;
}

.cd-comment__body {
    margin: 0 0 0.6rem;
    white-space: pre-wrap;
}

.cd-comment__actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.cd-comment__input {
    width: 100%;
    padding: 0.6rem 0.75rem;
    border: 1px solid var(--lw-colour-border);
    border-radius: var(--lw-radius-sm);
    font-family: var(--lw-font);
    font-size: 1rem;
    background: var(--lw-colour-surface);
    color: var(--lw-colour-ink);
}

.cd-comment__link {
    padding: 0;
    border: none;
    background: none;
    color: var(--lw-colour-ink-muted);
    font-family: var(--lw-font);
    font-size: 0.9rem;
    text-decoration: underline;
    cursor: pointer;
}

.cd-comment__link:hover {
    color: var(--lw-colour-primary);
}

.cd-comment__link--danger:hover {
    color: var(--lw-colour-danger);
}
</style>
