<script setup>
import DOMPurify from 'dompurify';
import MarkdownIt from 'markdown-it';
import { computed, nextTick, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { ASSISTANT_MAX_MESSAGE } from '../api.js';
import { useAssistant } from '../composables/useAssistant.js';

const {
    open, messages, streaming, error, suggestions, close, clear, cancel, ask, retry
} = useAssistant();

const route = useRoute();
const draft = ref('');
const transcript = ref(null);
const input = ref(null);

// The model writes markdown. Rendered with HTML disabled and then sanitised
// anyway, because an answer is untrusted text however it was produced.
const markdown = new MarkdownIt({ html: false, linkify: true, breaks: true });

function render(content) {
    return DOMPurify.sanitize(markdown.render(content));
}

/**
 * Where the question is being asked from. The backend validates this against its
 * own allowlist of route names, so it has to stay in step with router.js.
 */
const context = computed(() => {
    const name = String(route.name ?? 'forums');

    if (name === 'forum') {
        return { routeName: name, forumCode: String(route.params.code ?? '') };
    }

    if (name === 'post' || name === 'post-edit') {
        return { routeName: name, postId: Number(route.params.id) };
    }

    return { routeName: name };
});

async function scrollToLatest() {
    await nextTick();

    if (transcript.value) {
        transcript.value.scrollTop = transcript.value.scrollHeight;
    }
}

// Deep, because a streaming answer grows in place rather than adding a message.
watch(messages, scrollToLatest, { deep: true });

watch(open, async (isOpen) => {
    if (!isOpen) {
        return;
    }

    await scrollToLatest();
    input.value?.focus();
});

async function send(question = draft.value) {
    const text = question.trim();

    if (!text || streaming.value) {
        return;
    }

    draft.value = '';
    await ask(text, context.value);
    await nextTick();
    input.value?.focus();
}

// Enter sends, Shift+Enter starts a new line: answers are often about multi-step
// tasks, and the question that prompts one is sometimes just as long.
function onKeydown(event) {
    if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        send();
    }
}
</script>

<template>
    <aside
        v-if="open"
        class="cd-assistant"
        role="dialog"
        aria-label="AI mode assistant"
        @keydown.esc="close"
    >
        <header class="cd-assistant__head">
            <div>
                <h2 class="cd-assistant__title">AI mode</h2>
                <p class="cd-assistant__hint">Ask how the forum works</p>
            </div>
            <button
                type="button"
                class="cd-assistant__icon"
                title="Start over"
                :disabled="messages.length === 0"
                @click="clear"
            >Clear</button>
            <button type="button" class="cd-assistant__icon" title="Close AI mode" @click="close">
                Close
            </button>
        </header>

        <div ref="transcript" class="cd-assistant__transcript">
            <div v-if="messages.length === 0" class="cd-assistant__welcome">
                <p>
                    Hello! I can explain how this forum works — posting, editing, comments,
                    likes and search. What would you like to know?
                </p>
                <div class="cd-assistant__suggestions">
                    <button
                        v-for="question in suggestions"
                        :key="question"
                        type="button"
                        class="cd-assistant__suggestion"
                        :disabled="streaming"
                        @click="send(question)"
                    >{{ question }}</button>
                </div>
            </div>

            <div
                v-for="message in messages"
                :key="message.id"
                class="cd-assistant__turn"
                :class="`cd-assistant__turn--${message.role}`"
            >
                <span class="cd-assistant__role">
                    {{ message.role === 'assistant' ? 'Assistant' : 'You' }}:
                </span>
                <div
                    v-if="message.content"
                    class="cd-assistant__bubble"
                    v-html="render(message.content)"
                />
                <div v-else class="cd-assistant__bubble cd-assistant__typing" aria-hidden="true">
                    <span /><span /><span />
                </div>
                <p v-if="message.fromHelpPages" class="cd-assistant__note">
                    Answered from the help pages — the AI model is offline.
                </p>
            </div>
        </div>

        <p class="cd-assistant__role" role="status" aria-live="polite">
            {{ streaming ? 'The assistant is writing a response.' : '' }}
        </p>

        <p v-if="error" class="cd-assistant__error">
            {{ error }}
            <button type="button" class="cd-assistant__icon" @click="retry(context)">Retry</button>
        </p>

        <form class="cd-assistant__composer" @submit.prevent="send()">
            <label class="cd-assistant__role" for="assistant-message">Your question</label>
            <textarea
                id="assistant-message"
                ref="input"
                v-model="draft"
                class="cd-assistant__input"
                rows="1"
                :maxlength="ASSISTANT_MAX_MESSAGE"
                placeholder="How do I create a new post?"
                :disabled="streaming"
                @keydown="onKeydown"
            />
            <button
                v-if="streaming"
                type="button"
                class="lw-command"
                title="Stop the response"
                @click="cancel"
            >Stop</button>
            <button v-else type="submit" class="lw-command" :disabled="!draft.trim()">Ask</button>
        </form>
        <p class="cd-assistant__note">The assistant can make mistakes. Check important answers.</p>
    </aside>
</template>

<style scoped>
.cd-assistant {
    position: fixed;
    right: 1.25rem;
    bottom: 1.25rem;
    z-index: 20;
    display: flex;
    flex-direction: column;
    width: min(24rem, calc(100vw - 2.5rem));
    max-height: min(32rem, calc(100vh - 2.5rem));
    padding: 0;
    overflow: hidden;
    border: 1px solid rgba(79, 70, 229, .16);
    border-radius: 26px;
    background: var(--lw-colour-surface);
    box-shadow: var(--shadow-floating);
}

.cd-assistant__head {
    display: flex;
    align-items: flex-start;
    gap: 0.4rem;
    padding: 16px 18px;
    color: white;
    background: linear-gradient(135deg, #3730a3, #4f46e5);
}

.cd-assistant__title {
    margin: 0;
    font-size: 1rem;
}

.cd-assistant__hint {
    margin: 0.1rem 0 0;
    color: #e0e7ff;
    font-size: 0.78rem;
}

.cd-assistant__head div {
    flex: 1;
}

.cd-assistant__icon {
    padding: 0.2rem 0.5rem;
    border: 1px solid rgba(255, 255, 255, .3);
    border-radius: 10px;
    background: rgba(255, 255, 255, .12);
    color: white;
    font-size: 0.8rem;
    cursor: pointer;
}

.cd-assistant__icon:hover:not(:disabled) {
    background: rgba(255, 255, 255, .22);
}

.cd-assistant__icon:disabled {
    opacity: 0.5;
    cursor: default;
}

.cd-assistant__transcript {
    flex: 1;
    overflow-y: auto;
    padding: 0.9rem 1.1rem;
}

.cd-assistant__welcome p {
    margin: 0 0 0.6rem;
    font-size: 0.9rem;
    line-height: 1.45;
}

.cd-assistant__turn {
    margin-bottom: 0.7rem;
}

.cd-assistant__turn--user {
    text-align: right;
}

.cd-assistant__bubble {
    display: inline-block;
    max-width: 90%;
    padding: 0.5rem 0.7rem;
    border-radius: var(--lw-radius-sm);
    border: 1px solid var(--lw-colour-border);
    font-size: 0.9rem;
    line-height: 1.45;
    text-align: left;
}

.cd-assistant__turn--user .cd-assistant__bubble {
    background: var(--lw-colour-primary);
    border-color: var(--lw-colour-primary);
    color: #fff;
}

/* Rendered markdown, so the bubble's own margins have to be tamed. */
.cd-assistant__bubble :deep(> :first-child) {
    margin-top: 0;
}

.cd-assistant__bubble :deep(> :last-child) {
    margin-bottom: 0;
}

.cd-assistant__bubble :deep(p) {
    margin: 0 0 0.5rem;
}

.cd-assistant__bubble :deep(ol),
.cd-assistant__bubble :deep(ul) {
    margin: 0 0 0.5rem;
    padding-left: 1.2rem;
}

.cd-assistant__bubble :deep(code) {
    font-size: 0.85em;
}

.cd-assistant__typing {
    display: inline-flex;
    gap: 0.25rem;
    align-items: center;
}

.cd-assistant__typing span {
    width: 0.35rem;
    height: 0.35rem;
    border-radius: 50%;
    background: var(--lw-colour-ink);
    opacity: 0.35;
    animation: cd-assistant-blink 1.2s infinite;
}

.cd-assistant__typing span:nth-child(2) {
    animation-delay: 0.2s;
}

.cd-assistant__typing span:nth-child(3) {
    animation-delay: 0.4s;
}

@keyframes cd-assistant-blink {
    0%, 60%, 100% { opacity: 0.25; }
    30% { opacity: 0.8; }
}

@media (prefers-reduced-motion: reduce) {
    .cd-assistant__typing span {
        animation: none;
    }
}

.cd-assistant__suggestions {
    display: flex;
    flex-wrap: wrap;
    gap: 0.35rem;
    padding: 0 1.1rem;
}

.cd-assistant__suggestion {
    padding: 0.3rem 0.55rem;
    border: 1px solid var(--lw-colour-border);
    border-radius: var(--lw-radius-sm);
    background: none;
    color: var(--lw-colour-ink);
    font-size: 0.78rem;
    cursor: pointer;
}

.cd-assistant__suggestion:hover:not(:disabled) {
    border-color: #a5b4fc;
    background: #eef2ff;
    color: #3730a3;
}

.cd-assistant__error {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
    margin: 0.6rem 1.1rem;
    padding: 0.45rem 0.6rem;
    border: 1px solid var(--lw-colour-danger);
    border-radius: var(--lw-radius-sm);
    background: rgba(180, 35, 24, 0.08);
    color: var(--lw-colour-danger);
    font-size: 0.82rem;
}

.cd-assistant__composer {
    display: flex;
    gap: 0.4rem;
    align-items: flex-end;
    margin-top: 0.6rem;
    padding: 0.75rem 1.1rem;
    border-top: 1px solid var(--lw-colour-border);
}

/* Present for screen readers; the placeholder and the bubbles carry the sighted user. */
.cd-assistant__role {
    position: absolute;
    width: 1px;
    height: 1px;
    overflow: hidden;
    clip-path: inset(50%);
    white-space: nowrap;
}

.cd-assistant__input {
    flex: 1;
    min-width: 0;
    max-height: 6rem;
    padding: 0.5rem 0.6rem;
    border: 1px solid var(--lw-colour-border);
    border-radius: var(--lw-radius-sm);
    font-family: var(--lw-font);
    font-size: 0.9rem;
    line-height: 1.4;
    resize: vertical;
    background: var(--lw-colour-surface);
    color: var(--lw-colour-ink);
}

.cd-assistant__input:focus {
    outline: 2px solid var(--lw-colour-primary);
    outline-offset: 1px;
}

.cd-assistant__note {
    margin: 0 0 0.7rem;
    padding: 0 1.1rem;
    color: #98a2b3;
    font-size: 0.7rem;
    text-align: center;
}
</style>
