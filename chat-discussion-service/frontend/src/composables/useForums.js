import { ref } from 'vue';
import { api } from '../api.js';

const forums = ref([]);
let inFlight = null;

async function ensureLoaded() {
    if (forums.value.length > 0) {
        return forums.value;
    }

    inFlight ??= api.forums()
        .then((loaded) => {
            forums.value = loaded ?? [];
            return forums.value;
        })
        .finally(() => {
            inFlight = null;
        });

    return inFlight;
}

function forumName(code) {
    return forums.value.find((forum) => forum.code === code)?.name ?? code;
}

function exists(code) {
    return forums.value.some((forum) => forum.code === code);
}

export function useForums() {
    return { forums, ensureLoaded, forumName, exists };
}
