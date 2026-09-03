<script setup>
import { onBeforeUnmount } from 'vue';
import ForumNav from '../components/ForumNav.vue';
import GarryAssistant from '../components/GarryAssistant.vue';
import { useForums } from '../composables/useForums.js';
import { setFeatureHostContext } from './featureHost.js';

const props = defineProps({
    hostContext: {
        type: Object,
        default: null
    }
});

if (props.hostContext) {
    setFeatureHostContext(props.hostContext);
}
useForums().ensureLoaded().catch(() => {});

onBeforeUnmount(() => {
    if (props.hostContext) {
        setFeatureHostContext(null);
    }
});
</script>

<template>
    <section class="feature-chat-discussion">
        <ForumNav />
        <RouterView />
        <GarryAssistant />
    </section>
</template>