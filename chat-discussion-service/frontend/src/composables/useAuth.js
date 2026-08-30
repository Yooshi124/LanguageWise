import { readonly, ref } from 'vue';
import { api } from '../api.js';

const LOGIN_URL = 'http://localhost:3000/login.html';

const me = ref(null);
const resolved = ref(false);

async function ensureLoaded() {
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
    window.location.href = LOGIN_URL;
}

function isOwnedByViewer(item) {
    return me.value !== null && item != null && item.userId === me.value.id;
}

export function useAuth() {
    return {
        me: readonly(me),
        ensureLoaded,
        forget,
        redirectToSignIn,
        isOwnedByViewer
    };
}
