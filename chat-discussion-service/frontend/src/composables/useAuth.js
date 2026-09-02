import { computed, ref } from 'vue';
import { api } from '../api.js';
import { getFeatureUser, hasFeatureHostContext, redirectToSignIn as hostSignIn } from '../federation/featureHost.js';

const LOGIN_URL = 'http://localhost:3000/login.html';

const me = ref(null);
const resolved = ref(false);
const currentUser = computed(() => getFeatureUser() ?? me.value);

async function ensureLoaded() {
    if (hasFeatureHostContext()) {
        return getFeatureUser();
    }

    if (resolved.value) {
        return me.value;
    }

    try {
        me.value = await api.me();
    } catch {
        me.value = null;
    } finally {
        resolved.value = true;
    }

    return me.value;
}

function forget() {
    me.value = null;
    resolved.value = false;
}

function redirectToSignIn() {
    if (hasFeatureHostContext()) {
        hostSignIn(window.location.pathname + window.location.search + window.location.hash);
        return;
    }

    window.location.href = LOGIN_URL;
}

function isOwnedByViewer(item) {
    return currentUser.value !== null && item != null && item.userId === currentUser.value.id;
}

export function useAuth() {
    return {
        me: currentUser,
        ensureLoaded,
        forget,
        redirectToSignIn,
        isOwnedByViewer
    };
}
