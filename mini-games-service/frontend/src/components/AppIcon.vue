<script setup>
import {
	mdiAccountCircleOutline,
	mdiClose,
	mdiControllerClassicOutline,
	mdiForumOutline,
	mdiChartBoxOutline,
	mdiBookOpenPageVariantOutline,
	mdiHelpCircleOutline,
	mdiHomeOutline,
	mdiLogoutVariant,
	mdiMenu,
	mdiTrophyOutline,
} from '@mdi/js';

defineProps({
	name: { type: String, required: true },
	size: { type: Number, default: 24 },
});

const icons = {
	analytics: mdiChartBoxOutline,
	close: mdiClose,
	courses: mdiBookOpenPageVariantOutline,
	discussion: mdiForumOutline,
	games: mdiControllerClassicOutline,
	help: mdiHelpCircleOutline,
	home: mdiHomeOutline,
	logout: mdiLogoutVariant,
	menu: mdiMenu,
	profile: mdiAccountCircleOutline,
	quests: mdiTrophyOutline,
};
</script>

<template>
	<svg
		class="app-icon"
		:width="size"
		:height="size"
		viewBox="0 0 24 24"
		aria-hidden="true"
		focusable="false"
	>
		<path :d="icons[name]" fill="currentColor" />
	</svg>
</template>
