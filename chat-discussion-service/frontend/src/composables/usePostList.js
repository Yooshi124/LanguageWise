import { ref } from 'vue';
import { api, PAGE_SIZE } from '../api.js';

export function usePostList() {
    const posts = ref([]);
    const loading = ref(false);
    const loadingMore = ref(false);
    const error = ref(null);
    const hasMore = ref(false);

    let latestRequest = 0;

    async function load(params) {
        const request = ++latestRequest;

        loading.value = true;
        error.value = null;

        try {
            const page = await api.posts({ ...params, limit: PAGE_SIZE, offset: 0 });

            if (request !== latestRequest) {
                return;
            }

            posts.value = page ?? [];
            hasMore.value = (page?.length ?? 0) === PAGE_SIZE;
        } catch (failure) {
            if (request !== latestRequest) {
                return;
            }

            error.value = failure;
            posts.value = [];
            hasMore.value = false;
        } finally {
            if (request === latestRequest) {
                loading.value = false;
            }
        }
    }

    async function loadMore(params) {
        if (loadingMore.value || !hasMore.value) {
            return;
        }

        const request = latestRequest;
        loadingMore.value = true;

        try {
            const page = await api.posts({ ...params, limit: PAGE_SIZE, offset: posts.value.length });

            if (request !== latestRequest) {
                return;
            }

            posts.value = [...posts.value, ...(page ?? [])];
            hasMore.value = (page?.length ?? 0) === PAGE_SIZE;
        } catch (failure) {
            error.value = failure;
        } finally {
            loadingMore.value = false;
        }
    }

    function replace(updated) {
        const index = posts.value.findIndex((post) => post.id === updated.id);

        if (index !== -1) {
            posts.value[index] = updated;
        }
    }

    return { posts, loading, loadingMore, error, hasMore, load, loadMore, replace };
}
