import { readonly, ref } from 'vue';
import {
    api,
    ApiError,
    ASSISTANT_MAX_CONVERSATION,
    ASSISTANT_MAX_HISTORY
} from '../api.js';
import { useAuth } from './useAuth.js';

// Module level, like useAuth, so the panel keeps its transcript when you follow
// Garry's advice and navigate to another page mid-conversation.

const SUGGESTIONS = [
    'How do I create a new post?',
    'How do I edit my post?',
    'How do likes work?',
    'How do I find the posts I wrote?'
];

const open = ref(false);
const messages = ref([]);
const streaming = ref(false);
const error = ref('');

// Which user the transcript on screen belongs to. Signing in as somebody else
// has to start a fresh conversation rather than inherit the previous one.
let loadedUserId = null;
let controller = null;

function storageKey(userId) {
    return `languagewise:chat-discussion:assistant:v1:user:${userId}`;
}

/**
 * Points the panel at the signed-in user's transcript, restoring it from this
 * tab's session storage. Repeat calls for the same user do nothing, so the
 * component can call it on every render.
 */
function initialise() {
    const { me } = useAuth();
    const userId = me.value?.id ?? null;

    if (userId === loadedUserId) {
        return;
    }

    controller?.abort();
    controller = null;
    loadedUserId = userId;
    streaming.value = false;
    error.value = '';
    messages.value = userId === null ? [] : load(userId);
}

function expand() {
    open.value = true;
}

function close() {
    open.value = false;
}

function clear() {
    controller?.abort();
    messages.value = [];
    error.value = '';
    persist();
}

/** Ends the current answer where it stands. What has arrived so far is kept. */
function cancel() {
    controller?.abort();
}

/**
 * Asks a question and appends the answer as it streams in.
 *
 * The empty assistant message is pushed up front so the panel has something to
 * render the typing indicator into, and is removed again if the request fails
 * before any of it arrives.
 */
async function ask(question, context) {
    const text = question.trim();

    if (!text || streaming.value || loadedUserId === null) {
        return;
    }

    error.value = '';

    const carried = history(text.length);
    const answer = { id: crypto.randomUUID(), role: 'assistant', content: '' };

    messages.value = [
        ...messages.value,
        { id: crypto.randomUUID(), role: 'user', content: text },
        answer
    ];

    streaming.value = true;
    controller = new AbortController();
    const request = controller;

    try {
        await api.streamAssistantMessage(
            { message: text, history: carried, context },
            {
                onDelta: (delta) => {
                    messages.value = messages.value.map((message) =>
                        message.id === answer.id
                            ? { ...message, content: message.content + delta }
                            : message);
                },
                onDone: (reason) => {
                    if (reason === 'fallback') {
                        messages.value = messages.value.map((message) =>
                            message.id === answer.id
                                ? { ...message, fromHelpPages: true }
                                : message);
                    }

                    persist();
                }
            },
            request.signal
        );
    } catch (failure) {
        // An abort is the user pressing Stop, and a stream that broke part-way
        // has already put a partial answer on screen: neither is worth an error
        // message, and neither should throw the partial answer away.
        if (!(failure instanceof DOMException && failure.name === 'AbortError')) {
            error.value = describe(failure);
        }

        messages.value = messages.value.filter(
            (message) => message.id !== answer.id || message.content);
        persist();
    } finally {
        if (controller === request) {
            controller = null;
            streaming.value = false;
        }
    }
}

/**
 * Re-sends the last question, after an answer that failed or was stopped.
 *
 * Rewinds to the question rather than assuming it is the last message: a stream
 * that broke part-way leaves a partial answer after it, and that half-written
 * reply is what is being replaced.
 */
async function retry(context) {
    if (streaming.value) {
        return;
    }

    const asked = messages.value.findLastIndex((message) => message.role === 'user');

    if (asked < 0) {
        return;
    }

    const question = messages.value[asked].content;
    messages.value = messages.value.slice(0, asked);
    await ask(question, context);
}

function describe(failure) {
    // A stream that broke part-way throws a plain Error carrying its own wording,
    // so only a genuine ApiError is worth translating into one of ours.
    if (!(failure instanceof ApiError)) {
        return failure instanceof Error
            ? failure.message
            : 'Garry could not answer that. Please try again.';
    }

    if (failure.firstValidationMessage) {
        return failure.firstValidationMessage;
    }

    if (failure.isUnauthorised) {
        return 'Please sign in again to talk to Garry.';
    }

    if (failure.status === 429) {
        return failure.body?.detail
            || 'Garry is busy right now. Please wait a moment and try again.';
    }

    return failure.isUnavailable || failure.status === 502
        ? 'Garry is unavailable right now. Please try again shortly.'
        : 'Garry could not answer that. Please try again.';
}

/**
 * The transcript in the shape the backend expects: empty turns dropped, and
 * capped to the same number of turns and characters the backend accepts so a
 * long conversation is trimmed here rather than rejected outright.
 */
function history(questionLength) {
    return bounded(messages.value, ASSISTANT_MAX_CONVERSATION - questionLength)
        .map(({ role, content }) => ({ role, content }));
}

/**
 * The newest messages that fit inside both caps, oldest first. Counted from the
 * end because the recent turns are the ones a follow-up question depends on.
 */
function bounded(source, characterLimit = ASSISTANT_MAX_CONVERSATION) {
    const kept = [];
    let characters = 0;

    for (let index = source.length - 1; index >= 0; index--) {
        const message = source[index];

        if (!message.content || kept.length >= ASSISTANT_MAX_HISTORY) {
            break;
        }

        if (characters + message.content.length > characterLimit) {
            break;
        }

        kept.unshift(message);
        characters += message.content.length;
    }

    return kept;
}

/**
 * Session storage rather than local storage: the transcript should outlive a
 * page navigation, but not the tab. Trimmed to what would be sent anyway, so a
 * restored conversation is never larger than the backend would accept.
 */
function persist() {
    if (loadedUserId === null) {
        return;
    }

    messages.value = bounded(messages.value.filter((message) => message.content.trim()));

    try {
        sessionStorage.setItem(storageKey(loadedUserId), JSON.stringify(messages.value));
    } catch {
        // A full or unavailable session storage costs the transcript on reload,
        // which is not worth interrupting the conversation over.
    }
}

function load(userId) {
    let stored;

    try {
        stored = sessionStorage.getItem(storageKey(userId));
    } catch {
        return [];
    }

    if (!stored) {
        return [];
    }

    try {
        const value = JSON.parse(stored);
        return Array.isArray(value) ? bounded(value.filter(isMessage)) : [];
    } catch {
        sessionStorage.removeItem(storageKey(userId));
        return [];
    }
}

function isMessage(value) {
    return value !== null
        && typeof value === 'object'
        && typeof value.id === 'string'
        && (value.role === 'user' || value.role === 'assistant')
        && typeof value.content === 'string'
        && value.content.trim().length > 0;
}

export function useAssistant() {
    initialise();

    return {
        open: readonly(open),
        messages: readonly(messages),
        streaming: readonly(streaming),
        error: readonly(error),
        suggestions: SUGGESTIONS,
        expand,
        close,
        clear,
        cancel,
        ask,
        retry
    };
}
