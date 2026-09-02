<script setup>
import LikeButton from './LikeButton.vue';
import { excerpt, formatDate } from '../format.js';

const props = defineProps({
    post: { type: Object, required: true },
    showForum: { type: Boolean, default: true }
});

const emit = defineEmits(['update', 'error']);

function onLike({ liked, count }) {
    emit('update', { ...props.post, likedByViewer: liked, likeCount: count });
}
</script>

<template>
    <article class="lw-card cd-post">
        <h3 class="cd-post__title">
            <RouterLink :to="{ name: 'post', params: { id: post.id } }">{{ post.title }}</RouterLink>
        </h3>

        <p class="cd-post__meta">
            <span>{{ post.authorName || 'Unknown author' }}</span>
            <span aria-hidden="true">·</span>
            <span>{{ formatDate(post.createdAt) }}</span>
            <template v-if="showForum">
                <span aria-hidden="true">·</span>
                <RouterLink class="lw-badge" :to="{ name: 'forum', params: { code: post.forumCode } }">
                    {{ post.forumName }}
                </RouterLink>
            </template>
        </p>

        <p class="cd-post__body">{{ excerpt(post.content, 220) }}</p>

        <p v-if="post.matchedCommentExcerpt" class="cd-post__match">
            <strong>Matched in a comment:</strong> {{ excerpt(post.matchedCommentExcerpt) }}
        </p>

        <footer class="cd-post__footer">
            <LikeButton
                kind="post"
                :target-id="post.id"
                :liked="post.likedByViewer"
                :count="post.likeCount"
                @update="onLike"
                @error="$emit('error', $event)"
            />
            <RouterLink class="cd-post__comments" :to="{ name: 'post', params: { id: post.id } }">
                {{ post.commentCount }} {{ post.commentCount === 1 ? 'comment' : 'comments' }}
            </RouterLink>
        </footer>
    </article>
</template>

<style scoped>
.cd-post {
    margin-bottom: 20px;
    padding: 26px;
    transition: transform .2s ease, box-shadow .2s ease, border-color .2s ease;
}

.cd-post:hover {
    transform: translateY(-4px);
    border-color: #d5d9e6;
    box-shadow: 0 16px 36px rgba(31, 41, 55, .09);
}

.cd-post__title {
    margin: 0 0 0.5rem;
    font-size: 1.5rem;
    letter-spacing: -.02em;
}

.cd-post__title a {
    color: var(--lw-colour-ink);
    text-decoration: none;
}

.cd-post__title a:hover {
    color: var(--lw-colour-primary);
}

.cd-post__meta {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.4rem;
    margin: 0 0 0.6rem;
    color: var(--lw-colour-ink-muted);
    font-size: 0.85rem;
}

.cd-post__meta a.lw-badge {
    text-decoration: none;
}

.cd-post__body {
    margin: 0 0 0.75rem;
}

.cd-post__match {
    margin: 0 0 0.75rem;
    padding: 0.5rem 0.75rem;
    border-left: 3px solid var(--lw-colour-accent, var(--lw-colour-primary));
    background: var(--lw-colour-bg);
    color: var(--lw-colour-ink-muted);
    font-size: 0.9rem;
}

.cd-post__footer {
    display: flex;
    align-items: center;
    gap: 1rem;
}

.cd-post__comments {
    color: var(--lw-colour-ink-muted);
    font-size: 0.9rem;
    text-decoration: none;
}

.cd-post__comments:hover {
    color: var(--lw-colour-primary);
}
</style>
