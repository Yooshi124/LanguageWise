<script setup>
import { ref, watch } from 'vue';
import ImagePicker from './ImagePicker.vue';
import { useForums } from '../composables/useForums.js';

const props = defineProps({
    initial: { type: Object, default: null },
    /** Images already stored against the post, shown so they can be removed while editing. */
    images: { type: Array, default: () => [] },
    submitLabel: { type: String, default: 'Publish' },
    busy: { type: Boolean, default: false },
    error: { type: String, default: '' }
});

const emit = defineEmits(['submit', 'cancel', 'remove-image']);

const { forums, displayName } = useForums();

const title = ref('');
const content = ref('');
const forumCode = ref('global');

const pendingImages = ref([]);

watch(
    () => props.initial,
    (post) => {
        title.value = post?.title ?? '';
        content.value = post?.content ?? '';
        forumCode.value = post?.forumCode ?? 'global';
    },
    { immediate: true }
);

function submit() {
    emit('submit', {
        title: title.value.trim(),
        content: content.value.trim(),
        forumCode: forumCode.value,
        images: pendingImages.value.map((entry) => entry.file)
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
            <select id="post-forum" v-model="forumCode" class="cd-form__input" required>
                <option v-for="forum in forums" :key="forum.code" :value="forum.code">
                    {{ displayName(forum) }}
                </option>
            </select>
        </div>

        <div class="lw-field cd-form__field">
            <label class="cd-form__label" for="post-content">Content</label>
            <textarea id="post-content" v-model="content" class="cd-form__input" rows="10" required></textarea>
        </div>

        <ImagePicker
            v-model="pendingImages"
            :existing="images"
            :busy="busy"
            @remove-existing="$emit('remove-image', $event)"
        />

        <div class="lw-form-actions">
            <button type="submit" class="lw-command" :disabled="busy || !title.trim() || !content.trim()">
                {{ busy ? 'Saving…' : submitLabel }}
            </button>
            <button type="button" class="lw-command" @click="$emit('cancel')">Cancel</button>
        </div>
    </form>
</template>
