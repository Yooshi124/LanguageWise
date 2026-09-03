<script setup>
import { onBeforeUnmount } from 'vue';
import { setFeatureHostContext } from './featureHost.js';

const props = defineProps({
	hostContext: {
		type: Object,
		required: true
	}
});

setFeatureHostContext(props.hostContext);
onBeforeUnmount(() => setFeatureHostContext(undefined));
</script>

<template>
	<section class="feature-mini-games">
		<RouterView />
	</section>
</template>