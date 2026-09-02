<script setup>
import {
    mdiAccountCircleOutline,
    mdiArrowLeft,
    mdiChartBoxOutline,
    mdiClose,
    mdiBookOpenPageVariantOutline,
    mdiControllerClassicOutline,
    mdiForumOutline,
    mdiHomeOutline,
    mdiLogoutVariant,
    mdiMenu,
    mdiRobotHappyOutline,
    mdiPencilOutline,
    mdiTrophyOutline
} from '@mdi/js';

defineProps({
    name: { type: String, required: true },
    size: { type: Number, default: 24 }
});

const icons = {
    analytics: mdiChartBoxOutline,
    'arrow-left': mdiArrowLeft,
    assistant: mdiRobotHappyOutline,
    close: mdiClose,
    compose: mdiPencilOutline,
    courses: mdiBookOpenPageVariantOutline,
    discussion: mdiForumOutline,
    games: mdiControllerClassicOutline,
    home: mdiHomeOutline,
    logout: mdiLogoutVariant,
    menu: mdiMenu,
    profile: mdiAccountCircleOutline,
    quests: mdiTrophyOutline
};
</script>

<template>
    <v-icon class="app-icon" :icon="icons[name]" :size="size" aria-hidden="true" />
</template>
