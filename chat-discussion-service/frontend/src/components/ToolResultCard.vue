<script setup>
import { computed } from 'vue';
import { toolLabel } from '../config/assistantTools.js';

const maximumRows = 12;

const props = defineProps({
    result: { type: Object, required: true }
});

const label = computed(() => toolLabel(props.result.tool));
const raw = computed(() => JSON.stringify(props.result.result, null, 2));

const view = computed(() => {
    const value = asRecord(props.result.result);

    if (props.result.isError) {
        return {
            summary: text(value.text).replace(/^An error occurred invoking '[^']+': /, '')
                || 'The tool could not complete the request.',
            rows: []
        };
    }

    switch (props.result.tool) {
        case 'chat_list_forums':
            return {
                rows: list(value.forums).map((forum) => ({
                    primary: text(forum.name),
                    secondary: text(forum.code)
                }))
            };
        case 'chat_search_posts':
            return {
                rows: list(value.posts).map((post) => ({
                    primary: text(post.title),
                    secondary: `${text(post.forumName)} · ${number(post.commentCount)} comments`
                }))
            };
        case 'chat_get_post':
            return {
                summary: `${text(value.title)} — by ${text(value.authorName)} in ${text(value.forumName)}`,
                rows: list(value.comments).map((comment) => ({
                    primary: text(comment.content),
                    secondary: text(comment.authorName)
                }))
            };
        default:
            return { rows: [] };
    }
});

const visibleRows = computed(() => view.value.rows.slice(0, maximumRows));
const hiddenRows = computed(() => Math.max(0, view.value.rows.length - maximumRows));

function asRecord(value) {
    return typeof value === 'object' && value !== null && !Array.isArray(value) ? value : {};
}

function list(value) {
    return Array.isArray(value) ? value.map(asRecord) : [];
}

function text(value) {
    return typeof value === 'string' ? value : '';
}

function number(value) {
    return typeof value === 'number' ? value : 0;
}
</script>

<template>
    <section
        class="assistant-tool-card"
        :class="{ 'assistant-tool-card-error': result.isError }"
        :aria-label="`${label} result`"
    >
        <header>
            <span class="assistant-tool-card-badge">Tool</span>
            <strong>{{ label }}</strong>
        </header>
        <p v-if="view.summary" class="assistant-tool-card-summary">{{ view.summary }}</p>
        <ul v-if="visibleRows.length" class="assistant-tool-card-rows">
            <li v-for="(row, index) in visibleRows" :key="index">
                <span>{{ row.primary }}</span>
                <small v-if="row.secondary">{{ row.secondary }}</small>
            </li>
        </ul>
        <p v-if="hiddenRows" class="assistant-tool-card-more">+{{ hiddenRows }} more</p>
        <p
            v-if="!result.isError && !view.summary && visibleRows.length === 0"
            class="assistant-tool-card-summary"
        >
            Nothing found.
        </p>
        <details class="assistant-tool-card-raw">
            <summary>Raw result</summary>
            <pre>{{ raw }}</pre>
        </details>
    </section>
</template>
