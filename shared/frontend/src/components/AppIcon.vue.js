import { mdiAccountCircleOutline, mdiBookOpenPageVariantOutline, mdiChartBoxOutline, mdiClose, mdiControllerClassicOutline, mdiForumOutline, mdiHomeOutline, mdiLogoutVariant, mdiMenu, mdiTrophyOutline, } from '@mdi/js';
const __VLS_props = defineProps();
const icons = {
    analytics: mdiChartBoxOutline,
    close: mdiClose,
    courses: mdiBookOpenPageVariantOutline,
    discussion: mdiForumOutline,
    games: mdiControllerClassicOutline,
    home: mdiHomeOutline,
    logout: mdiLogoutVariant,
    menu: mdiMenu,
    profile: mdiAccountCircleOutline,
    quests: mdiTrophyOutline,
};
const __VLS_ctx = {
    ...{},
    ...{},
    ...{},
    ...{},
};
let __VLS_components;
let __VLS_intrinsics;
let __VLS_directives;
let __VLS_0;
/** @ts-ignore @type { | typeof __VLS_components.vIcon | typeof __VLS_components.VIcon | typeof __VLS_components['v-icon']} */
vIcon;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent1(__VLS_0, new __VLS_0({
    ...{ class: "app-icon" },
    icon: (__VLS_ctx.icons[__VLS_ctx.name]),
    size: (__VLS_ctx.size ?? 24),
    'aria-hidden': "true",
}));
const __VLS_2 = __VLS_1({
    ...{ class: "app-icon" },
    icon: (__VLS_ctx.icons[__VLS_ctx.name]),
    size: (__VLS_ctx.size ?? 24),
    'aria-hidden': "true",
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
var __VLS_5;
/** @type {__VLS_StyleScopedClasses['app-icon']} */ ;
var __VLS_3;
// @ts-ignore
[icons, name, size,];
const __VLS_export = (await import('vue')).defineComponent({
    __typeProps: {},
});
export default {};
