<script setup>
import { ref } from 'vue';
import { useAuth } from '../composables/useAuth.js';

const { me } = useAuth();
const signingOut = ref(false);

async function signOut() {
    signingOut.value = true;

    try {
        await fetch('/chat-discussion/shared-api/logout', { method: 'POST', credentials: 'same-origin' });
    } catch {
    }

    window.location.assign('/');
}
</script>

<template>
    <header class="lw-header">
        <div class="lw-header__inner">
            <h1 class="lw-header__title">Discussion Forum</h1>
            <p class="lw-header__subtitle">
                Lachlan &mdash; Where learners talk to each other about their progress.
            </p>
            <span v-if="me" class="lw-auth-status">
                <span>Logged in as {{ me.username }}</span>
                <button type="button" class="lw-command" :disabled="signingOut" @click="signOut">
                    {{ signingOut ? 'Signing out…' : 'Log out' }}
                </button>
            </span>
        </div>
    </header>
</template>
