<script setup>
import { computed, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import CommentItem from '../components/CommentItem.vue';
import ImageGallery from '../components/ImageGallery.vue';
import ImagePicker from '../components/ImagePicker.vue';
import LikeButton from '../components/LikeButton.vue';
import StateBlock from '../components/StateBlock.vue';
import { api, PAGE_SIZE } from '../api.js';
import { formatDate } from '../format.js';
import { useAuth } from '../composables/useAuth.js';
import { uploadCommentImages } from '../composables/useImageUploads.js';

const props = defineProps({ id: { type: [String, Number], required: true } });

const router = useRouter();
const { isOwnedByViewer } = useAuth();

const post = ref(null);
const comments = ref([]);
const commentCount = ref(0);
const hasMoreComments = ref(false);

const loading = ref(true);
const error = ref(null);
const actionError = ref('');

const draft = ref('');
const draftImages = ref([]);
const posting = ref(false);
const loadingMore = ref(false);
const deleting = ref(false);

const postId = computed(() => Number(props.id));
const isMine = computed(() => isOwnedByViewer(post.value));

async function load() {
    loading.value = true;
    error.value = null;

    try {
        const detail = await api.post(postId.value);

        post.value = detail;
        comments.value = detail.comments ?? [];
        commentCount.value = detail.commentCount;
        hasMoreComments.value = detail.commentsHasMore;
    } catch (failure) {
        error.value = failure;
        post.value = null;
    } finally {
        loading.value = false;
    }
}

async function loadMoreComments() {
    if (loadingMore.value) {
        return;
    }

    loadingMore.value = true;

    try {
        const page = await api.comments(postId.value, { limit: PAGE_SIZE, offset: comments.value.length });
        comments.value = [...comments.value, ...(page ?? [])];
        hasMoreComments.value = comments.value.length < commentCount.value;
    } catch (failure) {
        reportAction(failure, 'Those comments could not be loaded.');
    } finally {
        loadingMore.value = false;
    }
}

async function addComment() {
    const content = draft.value.trim();

    if (!content || posting.value) {
        return;
    }

    posting.value = true;
    actionError.value = '';

    let created;

    try {
        created = await api.createComment(postId.value, content);
    } catch (failure) {
        reportAction(failure, 'Your comment could not be posted.');
        posting.value = false;
        return;
    }

    const chosen = draftImages.value;
    const imageError = await uploadCommentImages(created.id, chosen.map((entry) => entry.file));

    if (imageError) {
        actionError.value = `Your comment was posted, but ${imageError}`;
    }

    comments.value = [
        ...comments.value,
        {
            ...created,
            likeCount: 0,
            likedByViewer: false,
            images: await storedImages(created.id)
        }
    ];
    commentCount.value += 1;
    draft.value = '';
    draftImages.value = [];
    posting.value = false;
}

async function storedImages(commentId) {
    try {
        return await api.commentImages(commentId);
    } catch {
        return [];
    }
}

async function removePost() {
    const confirmed = window.confirm(
        'Delete this post? Its comments and likes are deleted with it, and this cannot be undone.'
    );

    if (!confirmed || deleting.value) {
        return;
    }

    deleting.value = true;

    try {
        await api.deletePost(postId.value);
        router.push({ name: 'forum', params: { code: post.value.forumCode } });
    } catch (failure) {
        reportAction(failure, 'The post could not be deleted.');
        deleting.value = false;
    }
}

function onCommentUpdated(updated) {
    const index = comments.value.findIndex((comment) => comment.id === updated.id);

    if (index !== -1) {
        comments.value[index] = updated;
    }
}

function onCommentDeleted(id) {
    comments.value = comments.value.filter((comment) => comment.id !== id);
    commentCount.value = Math.max(0, commentCount.value - 1);
}

function onPostLike({ liked, count }) {
    post.value = { ...post.value, likedByViewer: liked, likeCount: count };
}

function reportAction(failure, fallback) {
    actionError.value = failure?.firstValidationMessage
        || (failure?.isUnavailable ? 'The discussion service is unavailable. Please try again.' : fallback);
}

watch(postId, load, { immediate: true });
</script>

<template>
    <StateBlock v-if="loading" title="Loading post…" />

    <StateBlock
        v-else-if="error && error.isNotFound"
        title="That post no longer exists"
        message="It may have been deleted by its author."
        tone="error"
    >
        <p><RouterLink :to="{ name: 'forums' }">Back to all forums</RouterLink></p>
    </StateBlock>

    <StateBlock
        v-else-if="error && error.isUnavailable"
        title="This post is unavailable"
        message="The discussion service could not reach its database. Nothing has been lost."
        tone="error"
        retry-label="Try again"
        @retry="load"
    />

    <StateBlock
        v-else-if="error"
        title="Something went wrong loading this post"
        message="Please try again."
        tone="error"
        retry-label="Try again"
        @retry="load"
    />

    <template v-else-if="post">
        <p class="cd-breadcrumb">
            <RouterLink :to="{ name: 'forum', params: { code: post.forumCode } }">
                &larr; {{ post.forumName }}
            </RouterLink>
        </p>

        <article class="lw-card">
            <h2 class="cd-detail__title">{{ post.title }}</h2>

            <p class="cd-detail__meta">
                <span>{{ post.authorName || 'Unknown author' }}</span>
                <span aria-hidden="true">·</span>
                <span>{{ formatDate(post.createdAt) }}</span>
                <span v-if="post.updatedAt !== post.createdAt" class="cd-detail__edited">(edited)</span>
            </p>

            <p class="cd-detail__body">{{ post.content }}</p>

            <ImageGallery :images="post.images" />

            <footer class="cd-detail__actions">
                <LikeButton
                    kind="post"
                    :target-id="post.id"
                    :liked="post.likedByViewer"
                    :count="post.likeCount"
                    @update="onPostLike"
                    @error="reportAction($event, 'Your like could not be saved.')"
                />
                <template v-if="isMine">
                    <RouterLink class="lw-command" :to="{ name: 'post-edit', params: { id: post.id } }">
                        Edit
                    </RouterLink>
                    <button type="button" class="lw-command" :disabled="deleting" @click="removePost">
                        {{ deleting ? 'Deleting…' : 'Delete' }}
                    </button>
                </template>
            </footer>
        </article>

        <p v-if="actionError" class="cd-detail__error">{{ actionError }}</p>

        <section class="cd-comments">
            <h3 class="lw-section-heading">
                {{ commentCount }} {{ commentCount === 1 ? 'comment' : 'comments' }}
            </h3>

            <form class="lw-card cd-comment-form" @submit.prevent="addComment">
                <label class="cd-comment-form__label" for="new-comment">Add a comment</label>
                <textarea
                    id="new-comment"
                    v-model="draft"
                    class="cd-comment-form__input"
                    rows="3"
                    placeholder="Share what you think…"
                ></textarea>

                <ImagePicker v-model="draftImages" :busy="posting" />

                <div class="lw-form-actions">
                    <button type="submit" class="lw-command" :disabled="posting || !draft.trim()">
                        {{ posting ? 'Posting…' : 'Post comment' }}
                    </button>
                </div>
            </form>

            <p v-if="comments.length === 0" class="cd-comments__empty">
                No comments yet. Yours would be the first.
            </p>

            <ul v-else class="cd-comments__list">
                <CommentItem
                    v-for="comment in comments"
                    :key="comment.id"
                    :comment="comment"
                    :can-edit="isOwnedByViewer(comment)"
                    @update="onCommentUpdated"
                    @deleted="onCommentDeleted"
                    @error="reportAction($event, 'That change could not be saved.')"
                />
            </ul>

            <p v-if="hasMoreComments" class="cd-more">
                <button type="button" class="lw-command" :disabled="loadingMore" @click="loadMoreComments">
                    {{ loadingMore ? 'Loading…' : 'Load more comments' }}
                </button>
            </p>
        </section>
    </template>
</template>

<style scoped>
.cd-breadcrumb {
    margin: 0 0 1rem;
    font-size: 0.9rem;
}

.cd-detail__title {
    margin: 0 0 0.4rem;
    font-size: 1.5rem;
}

.cd-detail__meta {
    display: flex;
    flex-wrap: wrap;
    gap: 0.4rem;
    margin: 0 0 1rem;
    color: var(--lw-colour-ink-muted);
    font-size: 0.85rem;
}

.cd-detail__edited {
    font-style: italic;
}

.cd-detail__body {
    margin: 0 0 1.25rem;
    white-space: pre-wrap;
    line-height: 1.6;
}

.cd-detail__actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.cd-detail__error {
    margin: 1rem 0 0;
    padding: 0.6rem 0.75rem;
    border: 1px solid var(--lw-colour-danger);
    border-radius: var(--lw-radius-sm);
    background: rgba(179, 38, 30, 0.08);
    color: var(--lw-colour-danger);
    font-size: 0.9rem;
}

.cd-comments {
    margin-top: 2rem;
}

.cd-comment-form {
    margin-bottom: 1rem;
}

.cd-comment-form__label {
    display: block;
    margin-bottom: 0.3rem;
    font-weight: 600;
    font-size: 0.9rem;
}

.cd-comment-form__input {
    width: 100%;
    padding: 0.6rem 0.75rem;
    border: 1px solid var(--lw-colour-border);
    border-radius: var(--lw-radius-sm);
    font-family: var(--lw-font);
    font-size: 1rem;
    background: var(--lw-colour-surface);
    color: var(--lw-colour-ink);
}

.cd-comments__list {
    margin: 0;
    padding: 0;
}

.cd-comments__empty {
    color: var(--lw-colour-ink-muted);
}

.cd-more {
    text-align: center;
    margin-top: 1rem;
}
</style>
