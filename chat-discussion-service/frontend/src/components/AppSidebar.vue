<script setup>
import { computed, onBeforeUnmount, ref } from 'vue';
import { useAuth } from '../composables/useAuth.js';
import { serviceNavigation, sharedHomeHref } from '../config/navigation.js';
import AppIcon from './AppIcon.vue';
import SidebarNavItem from './SidebarNavItem.vue';

const brandIconUrl = `${import.meta.env.BASE_URL}languagewise-icon.png`;
const { me } = useAuth();

const loggingOut = ref(false);
const logoutError = ref('');

const accountLabel = computed(() => (me.value ? me.value.username : 'Not logged in'));

defineProps({
    expanded: { type: Boolean, required: true },
    mobileOpen: { type: Boolean, required: true }
});

const emit = defineEmits(['update:expanded', 'update:mobileOpen']);

let hoverTimer;

function setExpanded(value) {
    clearTimeout(hoverTimer);
    emit('update:expanded', value);
}

function scheduleExpanded(value) {
    clearTimeout(hoverTimer);
    hoverTimer = setTimeout(() => setExpanded(value), value ? 140 : 220);
}

function handleFocusOut(event) {
    if (!event.currentTarget.contains(event.relatedTarget)) {
        setExpanded(false);
    }
}

async function handleLogout() {
    loggingOut.value = true;
    logoutError.value = '';

    try {
        await fetch('/chat-discussion/shared-api/logout', {
            method: 'POST',
            credentials: 'same-origin'
        });
    } catch {
        // Signing out locally still matters even if the shared service is down.
    }

    window.location.assign('/');
}

onBeforeUnmount(() => clearTimeout(hoverTimer));
</script>

<template>
    <aside
        class="app-sidebar"
        :class="{ expanded, 'mobile-open': mobileOpen }"
        aria-label="LanguageWise services"
        @mouseenter="scheduleExpanded(true)"
        @mouseleave="scheduleExpanded(false)"
        @focusin="setExpanded(true)"
        @focusout="handleFocusOut"
    >
        <div class="sidebar-header">
            <a
                :href="sharedHomeHref"
                class="sidebar-brand"
                :aria-label="expanded || mobileOpen ? undefined : 'LanguageWise home'"
            >
                <img class="brand-mark" :src="brandIconUrl" alt="">
                <span v-if="expanded || mobileOpen" class="brand-name">LanguageWise</span>
            </a>
            <button
                type="button"
                class="sidebar-toggle mobile-sidebar-close"
                aria-label="Close service navigation"
                @click="emit('update:mobileOpen', false)"
            >
                <AppIcon name="close" />
            </button>
        </div>

        <nav class="sidebar-services" aria-label="Services">
            <SidebarNavItem
                v-for="item in serviceNavigation"
                :key="item.label"
                :label="item.label"
                :icon="item.icon"
                :href="item.href"
                :active="item.current"
                :disabled="item.disabled"
                :show-label="expanded || mobileOpen"
            />
        </nav>

        <nav class="sidebar-utilities" aria-label="Account">
            <SidebarNavItem
                :label="accountLabel"
                icon="profile"
                :static="true"
                :show-label="expanded || mobileOpen"
            />
            <SidebarNavItem
                v-if="me"
                :label="loggingOut ? 'Logging out' : 'Logout'"
                icon="logout"
                :disabled="loggingOut"
                :show-label="expanded || mobileOpen"
                @click="handleLogout"
            />
            <p v-if="logoutError" class="sidebar-account-error" role="alert">{{ logoutError }}</p>
        </nav>
    </aside>
</template>
