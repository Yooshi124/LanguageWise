<script setup>
import DOMPurify from 'dompurify';
import MarkdownIt from 'markdown-it';
import { computed, nextTick, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import {
    mdiDeleteOutline,
    mdiMinus,
    mdiRefresh,
    mdiSend,
    mdiStopCircleOutline,
    mdiToolboxOutline
} from '@mdi/js';
import { ASSISTANT_MAX_MESSAGE } from '../api.js';
import { useAssistant } from '../composables/useAssistant.js';
import { assistantToolChips, missingRequirement } from '../config/assistantTools.js';
import ToolResultCard from './ToolResultCard.vue';

const {
    open, messages, streaming, error, suggestions, expand, close, clear, cancel, ask, retry,
    tools, toolsEnabled, toolsLoading, toolsError, toolRunning, setToolsEnabled, loadTools, runTool
} = useAssistant();

const route = useRoute();
const draft = ref('');
const transcript = ref(null);
const input = ref(null);
const garryImage = '/remotes/chat-discussion/images/garry.png';

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

const toolContext = computed(() => ({
    forumCode: context.value.forumCode,
    postId: context.value.postId,
    query: draft.value
}));

const toolChips = computed(() =>
    assistantToolChips
        .map((chip) => ({
            chip,
            tool: tools.value.find((tool) => tool.name === chip.tool),
            unavailable: missingRequirement(chip, toolContext.value)
        }))
        .filter((entry) => entry.tool !== undefined));

const toolsBusy = computed(() => streaming.value || toolRunning.value !== null);

async function runToolChip(chip) {
    const tool = tools.value.find((item) => item.name === chip.tool);

    if (!tool || missingRequirement(chip, toolContext.value)) {
        return;
    }

    await runTool(tool, chip.arguments(toolContext.value));
}

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
    <aside class="garry-assistant" aria-label="Garry the LanguageWise assistant">
        <Transition name="garry-panel">
            <section v-if="open" class="garry-panel" @keydown.esc="close">
                <header class="garry-header">
                    <img :src="garryImage" alt="" class="garry-header-image" />
                    <div>
                        <strong>Garry</strong>
                        <span>Hi, I’m Garry and I’m here to help you learn!</span>
                    </div>
                    <v-btn
                        :icon="mdiToolboxOutline"
                        :variant="toolsEnabled ? 'tonal' : 'text'"
                        size="small"
                        :aria-pressed="toolsEnabled"
                        :aria-label="toolsEnabled ? 'Hide Garry’s tools' : 'Show Garry’s tools'"
                        :title="toolsEnabled ? 'Hide tools' : 'Show tools'"
                        @click="setToolsEnabled(!toolsEnabled)"
                    />
                    <v-btn
                        :icon="mdiDeleteOutline"
                        variant="text"
                        size="small"
                        aria-label="Clear conversation"
                        :disabled="messages.length === 0"
                        @click="clear"
                    />
                    <v-btn
                        :icon="mdiMinus"
                        variant="text"
                        size="small"
                        aria-label="Minimize Garry"
                        @click="close"
                    />
                </header>

                <div ref="transcript" class="garry-messages">
                    <div v-if="messages.length === 0" class="garry-welcome">
                        <img :src="garryImage" alt="Garry the LanguageWise assistant" />
                        <h2>Hi, I’m Garry!</h2>
                        <p>
                            Ask me how this forum works — posting, editing, comments,
                            likes and search.
                        </p>
                        <div class="garry-suggestions" aria-label="Suggested questions">
                            <button
                                v-for="question in suggestions"
                                :key="question"
                                type="button"
                                :disabled="streaming"
                                @click="send(question)"
                            >{{ question }}</button>
                        </div>
                    </div>

                    <div
                        v-for="message in messages"
                        :key="message.id"
                        class="garry-message"
                        :class="`garry-message-${message.role}`"
                    >
                        <span class="sr-only">
                            {{ message.role === 'assistant' ? 'Garry' : 'You' }}:
                        </span>
                        <ToolResultCard
                            v-for="(toolResult, index) in message.toolResults ?? []"
                            :key="`${message.id}-tool-${index}`"
                            :result="toolResult"
                        />
                        <div
                            v-if="message.content"
                            class="garry-message-content"
                            v-html="render(message.content)"
                        />
                        <div v-else class="garry-typing" aria-hidden="true">
                            <span /><span /><span />
                        </div>
                        <p v-if="message.fromHelpPages" class="garry-fallback-note">
                            Answered from the help pages — the AI model is offline.
                        </p>
                    </div>
                </div>

                <p class="sr-only" role="status" aria-live="polite">
                    {{ streaming ? 'Garry is writing a response.' : '' }}
                </p>

                <div class="assistant-tools">
                    <template v-if="toolsEnabled">
                        <p v-if="toolsLoading" class="assistant-tools-status">Loading tools…</p>
                        <p v-else-if="toolsError" class="assistant-tools-status">
                            {{ toolsError }}
                            <button type="button" @click="loadTools()">Retry</button>
                        </p>
                        <div v-else class="assistant-tool-chips" aria-label="Garry's tools">
                            <button
                                v-for="entry in toolChips"
                                :key="entry.chip.tool"
                                type="button"
                                :disabled="toolsBusy || entry.unavailable !== null"
                                :title="entry.unavailable ?? entry.tool?.description"
                                :aria-busy="toolRunning === entry.chip.tool"
                                @click="runToolChip(entry.chip)"
                            >{{ entry.chip.label }}</button>
                        </div>
                    </template>
                </div>

                <v-alert
                    v-if="error"
                    type="error"
                    variant="tonal"
                    density="compact"
                    class="garry-error"
                >
                    {{ error }}
                    <template #append>
                        <v-btn
                            :icon="mdiRefresh"
                            variant="text"
                            size="small"
                            aria-label="Retry last message"
                            @click="retry(context)"
                        />
                    </template>
                </v-alert>

                <form class="garry-composer" @submit.prevent="send()">
                    <label class="sr-only" for="assistant-message">Message Garry</label>
                    <textarea
                        id="assistant-message"
                        ref="input"
                        v-model="draft"
                        rows="1"
                        :maxlength="ASSISTANT_MAX_MESSAGE"
                        placeholder="Ask Garry about the forum…"
                        :disabled="streaming"
                        @keydown="onKeydown"
                    />
                    <v-btn
                        v-if="streaming"
                        :icon="mdiStopCircleOutline"
                        color="primary"
                        variant="text"
                        aria-label="Stop Garry’s response"
                        @click="cancel"
                    />
                    <v-btn
                        v-else
                        :icon="mdiSend"
                        color="primary"
                        variant="flat"
                        aria-label="Send message"
                        type="submit"
                        :disabled="!draft.trim()"
                    />
                </form>
                <p class="garry-disclaimer">Garry can make mistakes. Check important answers.</p>
            </section>
        </Transition>

        <button
            v-if="!open"
            type="button"
            class="garry-launcher"
            aria-label="Open Garry the LanguageWise assistant"
            @click="expand"
        >
            <img :src="garryImage" alt="" />
            <span>Ask Garry</span>
        </button>
    </aside>
</template>
