<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import PostForm from '../components/PostForm.vue';
import StateBlock from '../components/StateBlock.vue';
import { api } from '../api.js';
import { uploadPostImages } from '../composables/useImageUploads.js';
import { isOwnedByFeatureUser } from '../federation/featureHost.js';

const props = defineProps({ id: { type: [String, Number], required: true } });

const router = useRouter();

const post = ref(null);
const images = ref([]);
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
        images.value = post.value.images ?? [];
    } catch (failure) {
        loadError.value = failure;
    } finally {
        loading.value = false;
    }
}

async function submit({ images: chosen = [], ...update }) {
    busy.value = true;
    error.value = '';

    try {
        await api.updatePost(postId.value, update);
    } catch (failure) {
        error.value = failure.firstValidationMessage
            || (failure.status === 403
                ? 'You can only edit your own posts.'
                : 'The post could not be saved. Please try again.');
        busy.value = false;
        return;
    }

    const imageError = await uploadPostImages(postId.value, chosen);

    if (imageError) {
        // Staying here keeps the files that failed attached to the picker.
        error.value = `Your changes were saved, but ${imageError}`;
        images.value = await api.postImages(postId.value).catch(() => images.value);
        busy.value = false;
        return;
    }

    router.push({ name: 'post', params: { id: postId.value } });
}

// Removing a stored image takes effect at once: it is its own resource, not a field
// of the post that Save could carry with it.
async function removeImage(image) {
    const confirmed = window.confirm(`Remove ${image.fileName}? This cannot be undone.`);

    if (!confirmed || busy.value) {
        return;
    }

    busy.value = true;
    error.value = '';

    try {
        await api.deleteImage(image.id);
        images.value = images.value.filter((stored) => stored.id !== image.id);
    } catch (failure) {
        error.value = failure.isUnavailable
            ? 'The discussion service is unavailable, so the image was not removed.'
            : 'That image could not be removed.';
    } finally {
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
        v-else-if="!isOwnedByFeatureUser(post)"
        title="You can only edit your own posts"
        message="This post belongs to somebody else."
        tone="error"
    >
        <p><RouterLink :to="{ name: 'post', params: { id: postId } }">Back to the post</RouterLink></p>
    </StateBlock>

    <template v-else>
        <header class="cd-heading cd-heading--compact">
            <div class="cd-heading__intro">
                <v-chip color="primary" variant="tonal">Edit post</v-chip>
            </div>
            <h1>Refine your post.</h1>
            <p>Update the title, content or images, then save your changes.</p>
        </header>

        <PostForm
            :initial="post"
            :images="images"
            submit-label="Save changes"
            :busy="busy"
            :error="error"
            @submit="submit"
            @cancel="router.push({ name: 'post', params: { id: postId } })"
            @remove-image="removeImage"
        />
    </template>
</template>
