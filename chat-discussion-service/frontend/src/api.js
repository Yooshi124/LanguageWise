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

export const api = {
    forums: () => request('/forums'),

    posts: ({ mine, forumCode, q, limit, offset } = {}) =>
        request(`/posts${query({ mine, forumCode, q, limit, offset })}`),

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

    assistantTopics: () => request('/assistant/topics')
};

export const PAGE_SIZE = 20;

/** Matches ImageRules.MaxBytes. */
export const MAX_IMAGE_BYTES = 5 * 1024 * 1024;

/** Matches ImageRules.MaxPerPost, which a comment shares with a post. */
export const MAX_IMAGES_PER_POST = 6;

/** Matches ImageRules.AllowedContentTypes. */
export const ACCEPTED_IMAGE_TYPES = ['image/png', 'image/jpeg', 'image/gif', 'image/webp'];
