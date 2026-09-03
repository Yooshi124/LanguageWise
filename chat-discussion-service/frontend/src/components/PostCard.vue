<script setup>
import AppIcon from './AppIcon.vue';
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

        <p class="cd-post__author">{{ post.authorName || 'Unknown author' }}</p>
        <p class="cd-post__meta">
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
                <AppIcon name="arrow-right" :size="18" />
            </RouterLink>
        </footer>
    </article>
</template>
