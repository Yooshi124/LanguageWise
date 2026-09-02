const BASE = '/chat-discussion/api';

export class ApiError extends Error {
    constructor(status, body) {
        super(`Request failed with status ${status}`);
        this.name = 'ApiError';
        this.status = status;
        this.body = body;
    }

    get isUnavailable() {
        return this.status === 503 || this.status === 0;
    }

    get isNotFound() {
        return this.status === 404;
    }

    get isUnauthorised() {
        return this.status === 401;
    }

    get firstValidationMessage() {
        const errors = this.body?.errors;
        if (!errors) {
            return null;
        }

        for (const messages of Object.values(errors)) {
            if (Array.isArray(messages) && messages.length > 0) {
                return messages[0];
            }
        }

        return null;
    }
}

async function request(path, { method = 'GET', body } = {}) {
    let response;

    // A FormData body is sent as it is: the browser adds the multipart boundary to
    // the content type itself, so setting that header would break it.
    const isForm = typeof FormData !== 'undefined' && body instanceof FormData;

    try {
        response = await fetch(BASE + path, {
            method,
            credentials: 'same-origin',
            headers: body === undefined || isForm ? {} : { 'Content-Type': 'application/json' },
            body: body === undefined || isForm ? body : JSON.stringify(body)
        });
    } catch {
        throw new ApiError(0, null);
    }

    const text = response.status === 204 ? '' : await response.text();
    let payload = null;

    if (text) {
        try {
            payload = JSON.parse(text);
        } catch {
            payload = text;
        }
    }

    if (!response.ok) {
        throw new ApiError(response.status, payload);
    }

    return payload;
}

function query(params) {
    const search = new URLSearchParams();

    for (const [key, value] of Object.entries(params)) {
        if (value !== null && value !== undefined && value !== '') {
            search.set(key, String(value));
        }
    }

    const rendered = search.toString();
    return rendered ? `?${rendered}` : '';
}

function upload(path, file) {
    const form = new FormData();
    form.append('file', file);
    return request(path, { method: 'POST', body: form });
}

// ---------------------------------------------------------------------------
// AI mode. Its answer arrives as server-sent events rather than one JSON body,
// so it is read here by hand instead of through request() above.
//
// EventSource is not an option: it can only issue a GET, and the question, its
// history and the page it was asked from all travel in a POST body.
// ---------------------------------------------------------------------------

/**
 * Streams one answer, calling onDelta for each fragment as it arrives and onDone
 * once when the model finishes. Aborting the signal ends the stream quietly.
 *
 * onDone receives the reason the answer ended. 'fallback' means the model could
 * not be reached and the backend served the stored help text instead.
 *
 * Throws an ApiError before the stream starts, and a plain Error if it breaks
 * part-way through — the distinction matters because a broken stream has already
 * put some of an answer on screen.
 */
async function streamAssistantMessage({ message, history, context }, { onDelta, onDone }, signal) {
    let response;

    try {
        response = await fetch(`${BASE}/assistant/messages`, {
            method: 'POST',
            signal,
            credentials: 'same-origin',
            headers: { Accept: 'text/event-stream', 'Content-Type': 'application/json' },
            body: JSON.stringify({ message, history, context })
        });
    } catch (failure) {
        if (failure instanceof DOMException && failure.name === 'AbortError') {
            throw failure;
        }

        throw new ApiError(0, null);
    }

    if (!response.ok) {
        let payload = null;

        try {
            payload = await response.json();
        } catch {
            payload = null;
        }

        throw new ApiError(response.status, payload);
    }

    if (!response.body) {
        throw new Error('The assistant could not start a response. Please try again.');
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let completed = false;

    for (;;) {
        const { value, done } = await reader.read();

        // Normalising the line endings first means a frame is always separated by
        // exactly one blank line, whatever the proxy in between decided to send.
        buffer += decoder.decode(value, { stream: !done }).replace(/\r\n/g, '\n');

        let boundary = buffer.indexOf('\n\n');

        while (boundary >= 0) {
            completed = readFrame(buffer.slice(0, boundary), onDelta, onDone) || completed;
            buffer = buffer.slice(boundary + 2);
            boundary = buffer.indexOf('\n\n');
        }

        if (done) {
            break;
        }
    }

    if (buffer.trim()) {
        completed = readFrame(buffer, onDelta, onDone) || completed;
    }

    // No terminating event means the connection dropped mid-answer. Saying so is
    // better than leaving a half-written reply looking finished.
    if (!completed) {
        throw new Error('The assistant’s response ended unexpectedly. Please try again.');
    }
}

/** One server-sent event. Returns true when it was the one that ends the stream. */
function readFrame(frame, onDelta, onDone) {
    let name = 'message';
    const data = [];

    for (const line of frame.split('\n')) {
        if (line.startsWith('event:')) {
            name = line.slice(6).trim();
        } else if (line.startsWith('data:')) {
            data.push(line.slice(5).replace(/^ /, ''));
        }
    }

    if (data.length === 0) {
        return false;
    }

    let payload;

    try {
        payload = JSON.parse(data.join('\n'));
    } catch {
        throw new Error('The assistant returned an invalid response. Please try again.');
    }

    if (name === 'delta') {
        if (typeof payload?.content === 'string' && payload.content) {
            onDelta(payload.content);
        }

        return false;
    }

    if (name === 'done') {
        onDone(typeof payload?.reason === 'string' ? payload.reason : 'stop');
        return true;
    }

    if (name === 'error') {
        throw new Error(
            typeof payload?.message === 'string' && payload.message
                ? payload.message
                : 'The assistant’s response was interrupted. Please try again.'
        );
    }

    return false;
}

export const api = {
    me: () => request('/me'),
    forums: () => request('/forums'),

    posts: ({ userId, forumCode, q, limit, offset } = {}) =>
        request(`/posts${query({ userId, forumCode, q, limit, offset })}`),

    post: (id) => request(`/posts/${id}`),

    comments: (postId, { limit, offset } = {}) =>
        request(`/posts/${postId}/comments${query({ limit, offset })}`),

    createPost: (post) => request('/posts', { method: 'POST', body: post }),
    updatePost: (id, patch) => request(`/posts/${id}`, { method: 'PATCH', body: patch }),
    deletePost: (id) => request(`/posts/${id}`, { method: 'DELETE' }),

    createComment: (postId, content) =>
        request(`/posts/${postId}/comments`, { method: 'POST', body: { content } }),
    updateComment: (id, content) => request(`/comments/${id}`, { method: 'PATCH', body: { content } }),
    deleteComment: (id) => request(`/comments/${id}`, { method: 'DELETE' }),

    postImages: (postId) => request(`/posts/${postId}/images`),
    commentImages: (commentId) => request(`/comments/${commentId}/images`),

    uploadPostImage: (postId, file) => upload(`/posts/${postId}/images`, file),
    uploadCommentImage: (commentId, file) => upload(`/comments/${commentId}/images`, file),

    deleteImage: (id) => request(`/images/${id}`, { method: 'DELETE' }),

    /** The src of a stored image. The bytes are proxied by the backend, never fetched here. */
    imageUrl: (id) => `${BASE}/images/${id}/content`,

    likePost: (id) => request(`/posts/${id}/likes`, { method: 'POST' }),
    unlikePost: (id) => request(`/posts/${id}/likes`, { method: 'DELETE' }),
    likeComment: (id) => request(`/comments/${id}/likes`, { method: 'POST' }),
    unlikeComment: (id) => request(`/comments/${id}/likes`, { method: 'DELETE' }),

    assistantTopics: () => request('/assistant/topics'),

    streamAssistantMessage
};

export const PAGE_SIZE = 20;

/** Matches ImageRules.MaxBytes. */
export const MAX_IMAGE_BYTES = 5 * 1024 * 1024;

/** Matches ImageRules.MaxPerPost, which a comment shares with a post. */
export const MAX_IMAGES_PER_POST = 6;

/** Matches ImageRules.AllowedContentTypes. */
export const ACCEPTED_IMAGE_TYPES = ['image/png', 'image/jpeg', 'image/gif', 'image/webp'];

/** Matches AssistantRequestValidator.MaximumMessageCharacters. */
export const ASSISTANT_MAX_MESSAGE = 4000;

/** Matches AssistantRequestValidator.MaximumHistoryTurns; older turns are dropped before sending. */
export const ASSISTANT_MAX_HISTORY = 12;

/** Matches AssistantRequestValidator.MaximumConversationCharacters, message included. */
export const ASSISTANT_MAX_CONVERSATION = 12000;
