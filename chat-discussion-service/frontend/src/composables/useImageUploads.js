import { api } from '../api.js';

/**
 * Uploads the chosen files against a post that already exists, one at a time: the
 * backend counts the images already there before accepting another, and concurrent
 * uploads would each pass that check against the same stale count.
 *
 * Returns a message describing the first failure, or an empty string.
 */
export function uploadPostImages(postId, files) {
    return uploadEach((file) => api.uploadPostImage(postId, file), files);
}

/** The same, for the images attached to a comment. */
export function uploadCommentImages(commentId, files) {
    return uploadEach((file) => api.uploadCommentImage(commentId, file), files);
}

async function uploadEach(send, files) {
    for (const file of files) {
        try {
            await send(file);
        } catch (failure) {
            return failure.firstValidationMessage
                || (failure.isUnavailable
                    ? 'the discussion service is unavailable, so the images were not uploaded.'
                    : `${file.name} could not be uploaded.`);
        }
    }

    return '';
}
