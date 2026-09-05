<script setup>
import { computed, onBeforeUnmount } from 'vue';
import AssistantChat from '../components/AssistantChat.vue';
import { setFeatureHostContext } from './featureHost.js';

const props = defineProps({
	hostContext: {
		type: Object,
		required: true
	}
});

const showAssistant = computed(() => props.hostContext?.user != null);

setFeatureHostContext(props.hostContext);
onBeforeUnmount(() => setFeatureHostContext(undefined));
</script>

<template>
	<section class="feature-mini-games">
		<RouterView />
		<AssistantChat
			v-if="showAssistant"
			:key="hostContext.user.id"
			:user-id="hostContext.user.id"
		/>
	</section>
</template>