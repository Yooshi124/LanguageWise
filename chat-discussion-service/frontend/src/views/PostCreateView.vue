<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import PostForm from '../components/PostForm.vue';
import { api } from '../api.js';

const router = useRouter();

const busy = ref(false);
const error = ref('');

async function submit(post) {
    busy.value = true;
    error.value = '';

    try {
        const created = await api.createPost(post);
        router.push({ name: 'post', params: { id: created.id } });
    } catch (failure) {
        error.value = failure.firstValidationMessage
            || (failure.isUnavailable
                ? 'The discussion service is unavailable. Your post has not been saved.'
                : 'The post could not be created. Please try again.');
        busy.value = false;
    }
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
</template>
