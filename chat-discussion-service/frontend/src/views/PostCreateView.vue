<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import PostForm from '../components/PostForm.vue';
import { api } from '../api.js';
import { uploadPostImages } from '../composables/useImageUploads.js';

const router = useRouter();

const busy = ref(false);
const error = ref('');

// Set when the post was saved but an image was not, so the message can offer the post.
const publishedId = ref(null);

async function submit({ images = [], ...fields }) {
    busy.value = true;
    error.value = '';
    publishedId.value = null;

    let created;

    try {
        created = await api.createPost(fields);
    } catch (failure) {
        error.value = failure.firstValidationMessage
            || (failure.isUnavailable
                ? 'The discussion service is unavailable. Your post has not been saved.'
                : 'The post could not be created. Please try again.');
        busy.value = false;
        return;
    }

    const imageError = await uploadPostImages(created.id, images);

    if (imageError) {
        error.value = `Your post was published, but ${imageError}`;
        publishedId.value = created.id;
        busy.value = false;
        return;
    }

    router.push({ name: 'post', params: { id: created.id } });
}
</script>

<template>
    <h2 class="lw-section-heading">New post</h2>
    <PostForm
        submit-label="Publish"
        :busy="busy"
        :error="error"
        @submit="submit"
        @cancel="router.back()"
    />

    <p v-if="publishedId" class="cd-create__published">
        <RouterLink :to="{ name: 'post', params: { id: publishedId } }">Open the published post</RouterLink>
    </p>
</template>
