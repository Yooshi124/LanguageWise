<script setup>
import { ref } from 'vue';
import ImageGallery from './ImageGallery.vue';
import ImagePicker from './ImagePicker.vue';
import LikeButton from './LikeButton.vue';
import { api } from '../api.js';
import { formatDate } from '../format.js';
import { uploadCommentImages } from '../composables/useImageUploads.js';

const props = defineProps({
    comment: { type: Object, required: true },
    canEdit: { type: Boolean, default: false }
});

const emit = defineEmits(['update', 'deleted', 'error']);

const editing = ref(false);
const draft = ref('');
const saving = ref(false);
const pendingImages = ref([]);
const imageError = ref('');

function startEditing() {
    draft.value = props.comment.content;
    pendingImages.value = [];
    imageError.value = '';
    editing.value = true;
}

async function save() {
    const content = draft.value.trim();

    if (!content || saving.value) {
        return;
    }

    saving.value = true;
    imageError.value = '';

    try {
        const updated = await api.updateComment(props.comment.id, content);

        // The text is saved; the images are their own resources and go up after it.
        const failure = await uploadCommentImages(props.comment.id, pendingImages.value.map((entry) => entry.file));

        if (failure) {
            imageError.value = `Your comment was saved, but ${failure}`;
        }

        emit('update', { ...props.comment, ...updated, images: await currentImages() });

        if (!failure) {
            editing.value = false;
        }

        pendingImages.value = [];
    } catch (error) {
        emit('error', error);
    } finally {
        saving.value = false;
    }
}

// Removing an image takes effect at once: it is its own resource, not a field of
// the comment that Save could carry with it.
async function removeImage(image) {
    if (!window.confirm(`Remove ${image.fileName}? This cannot be undone.`) || saving.value) {
        return;
    }

    saving.value = true;

    try {
        await api.deleteImage(image.id);
        emit('update', {
            ...props.comment,
            images: props.comment.images.filter((stored) => stored.id !== image.id)
        });
    } catch (error) {
        emit('error', error);
    } finally {
        saving.value = false;
    }
}

/** Re-read rather than guess, so a partial upload is reflected exactly. */
async function currentImages() {
    try {
        return await api.commentImages(props.comment.id);
    } catch {
        return props.comment.images ?? [];
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
        <p class="cd-comment__author">{{ comment.authorName || 'Unknown author' }}</p>
        <p class="cd-comment__meta">
            <span>{{ formatDate(comment.createdAt) }}</span>
            <span v-if="comment.updatedAt !== comment.createdAt" class="cd-comment__edited">(edited)</span>
        </p>

        <form v-if="editing" class="cd-comment__edit" @submit.prevent="save">
            <textarea v-model="draft" class="cd-comment__input" rows="3" required></textarea>

            <p v-if="imageError" class="cd-comment__error">{{ imageError }}</p>

            <ImagePicker
                v-model="pendingImages"
                :existing="comment.images ?? []"
                :busy="saving"
                @remove-existing="removeImage"
            />

            <div class="lw-form-actions">
                <button type="submit" class="lw-command" :disabled="saving || !draft.trim()">
                    {{ saving ? 'Saving…' : 'Save' }}
                </button>
                <button type="button" class="lw-command" @click="editing = false">Cancel</button>
            </div>
        </form>

        <template v-else>
            <p class="cd-comment__body">{{ comment.content }}</p>

            <ImageGallery :images="comment.images ?? []" />

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
                    <button type="button" class="lw-command" @click="startEditing">Edit</button>
                    <button type="button" class="lw-command" @click="remove">Delete</button>
                </template>
            </div>
        </template>
    </li>
</template>
