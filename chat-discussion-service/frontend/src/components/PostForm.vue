<script setup>
import { ref, watch } from 'vue';
import { useForums } from '../composables/useForums.js';

const props = defineProps({
    initial: { type: Object, default: null },
    submitLabel: { type: String, default: 'Publish' },
    busy: { type: Boolean, default: false },
    error: { type: String, default: '' }
});

const emit = defineEmits(['submit', 'cancel']);

const { forums } = useForums();

const title = ref('');
const content = ref('');
const category = ref('global');

watch(
    () => props.initial,
    (post) => {
        title.value = post?.title ?? '';
        content.value = post?.content ?? '';
        category.value = post?.category ?? 'global';
    },
    { immediate: true }
);

function submit() {
    emit('submit', {
        title: title.value.trim(),
        content: content.value.trim(),
        category: category.value
    });
}
</script>

<template>
    <form class="lw-card" @submit.prevent="submit">
        <p v-if="error" class="cd-form__error">{{ error }}</p>

        <div class="lw-field cd-form__field">
            <label class="cd-form__label" for="post-title">Title</label>
            <input
                id="post-title"
                v-model="title"
                class="cd-form__input"
                type="text"
                maxlength="200"
                required
                autofocus
            >
        </div>

        <div class="lw-field cd-form__field">
            <label class="cd-form__label" for="post-forum">Forum</label>
            <select id="post-forum" v-model="category" class="cd-form__input" required>
                <option v-for="forum in forums" :key="forum.code" :value="forum.code">
                    {{ forum.displayName }}
                </option>
            </select>
        </div>

        <div class="lw-field cd-form__field">
            <label class="cd-form__label" for="post-content">Content</label>
            <textarea id="post-content" v-model="content" class="cd-form__input" rows="10" required></textarea>
        </div>

        <div class="lw-form-actions">
            <button type="submit" class="lw-command" :disabled="busy || !title.trim() || !content.trim()">
                {{ busy ? 'Saving…' : submitLabel }}
            </button>
            <button type="button" class="lw-command" @click="$emit('cancel')">Cancel</button>
        </div>
    </form>
</template>

<style scoped>
.cd-form__field {
    margin-bottom: 1rem;
}

.cd-form__label {
    display: block;
    margin-bottom: 0.3rem;
    font-weight: 600;
    font-size: 0.9rem;
}

.cd-form__input {
    width: 100%;
    padding: 0.6rem 0.75rem;
    border: 1px solid var(--lw-colour-border);
    border-radius: var(--lw-radius-sm);
    font-family: var(--lw-font);
    font-size: 1rem;
    background: var(--lw-colour-surface);
    color: var(--lw-colour-ink);
}

.cd-form__input:focus {
    outline: 2px solid var(--lw-colour-primary);
    outline-offset: 1px;
}

.cd-form__error {
    margin: 0 0 1rem;
    padding: 0.6rem 0.75rem;
    border: 1px solid var(--lw-colour-danger);
    border-radius: var(--lw-radius-sm);
    background: rgba(179, 38, 30, 0.08);
    color: var(--lw-colour-danger);
    font-size: 0.9rem;
}
</style>
