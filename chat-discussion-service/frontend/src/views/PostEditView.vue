<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import PostForm from '../components/PostForm.vue';
import StateBlock from '../components/StateBlock.vue';
import { api } from '../api.js';
import { useAuth } from '../composables/useAuth.js';

const props = defineProps({ id: { type: [String, Number], required: true } });

const router = useRouter();
const { isOwnedByViewer } = useAuth();

const post = ref(null);
const loading = ref(true);
const loadError = ref(null);
const busy = ref(false);
const error = ref('');

const postId = computed(() => Number(props.id));

async function load() {
    loading.value = true;
    loadError.value = null;

    try {
        post.value = await api.post(postId.value);
    } catch (failure) {
        loadError.value = failure;
    } finally {
        loading.value = false;
    }
}

async function submit(update) {
    busy.value = true;
    error.value = '';

    try {
        await api.updatePost(postId.value, update);
        router.push({ name: 'post', params: { id: postId.value } });
    } catch (failure) {
        error.value = failure.firstValidationMessage
            || (failure.status === 403
                ? 'You can only edit your own posts.'
                : 'The post could not be saved. Please try again.');
        busy.value = false;
    }
}

onMounted(load);
</script>

<template>
    <StateBlock v-if="loading" title="Loading post…" />

    <StateBlock
        v-else-if="loadError && loadError.isNotFound"
        title="That post no longer exists"
        message="It may have been deleted."
        tone="error"
    >
        <p><RouterLink :to="{ name: 'forums' }">Back to all forums</RouterLink></p>
    </StateBlock>

    <StateBlock
        v-else-if="loadError"
        title="This post could not be loaded"
        message="The discussion service may be unavailable. Please try again."
        tone="error"
        retry-label="Try again"
        @retry="load"
    />

    <StateBlock
        v-else-if="!isOwnedByViewer(post)"
        title="You can only edit your own posts"
        message="This post belongs to somebody else."
        tone="error"
    >
        <p><RouterLink :to="{ name: 'post', params: { id: postId } }">Back to the post</RouterLink></p>
    </StateBlock>

    <template v-else>
        <h2 class="lw-section-heading">Edit post</h2>
        <PostForm
            :initial="post"
            submit-label="Save changes"
            :busy="busy"
            :error="error"
            @submit="submit"
            @cancel="router.push({ name: 'post', params: { id: postId } })"
        />
    </template>
</template>
