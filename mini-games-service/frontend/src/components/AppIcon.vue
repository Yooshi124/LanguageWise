<script setup>
import {
	mdiAccountCircleOutline,
	mdiArrowLeft,
	mdiBookSearchOutline,
	mdiClose,
	mdiContentCopy,
	mdiControllerClassicOutline,
	mdiForumOutline,
	mdiChartBoxOutline,
	mdiBookOpenPageVariantOutline,
	mdiHelpCircleOutline,
	mdiHomeOutline,
	mdiLogoutVariant,
	mdiMenu,
	mdiShareVariantOutline,
	mdiTrophyOutline,
} from '@mdi/js';

defineProps({
	name: { type: String, required: true },
	size: { type: Number, default: 24 },
});

const icons = {
	analytics: mdiChartBoxOutline,
	'arrow-left': mdiArrowLeft,
	close: mdiClose,
	copy: mdiContentCopy,
	courses: mdiBookOpenPageVariantOutline,
	discussion: mdiForumOutline,
	'docs-search': mdiBookSearchOutline,
	games: mdiControllerClassicOutline,
	help: mdiHelpCircleOutline,
	home: mdiHomeOutline,
	logout: mdiLogoutVariant,
	menu: mdiMenu,
	profile: mdiAccountCircleOutline,
	quests: mdiTrophyOutline,
	share: mdiShareVariantOutline,
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
