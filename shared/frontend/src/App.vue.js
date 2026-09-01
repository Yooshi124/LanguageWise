import { computed, ref } from 'vue';
import { useRoute } from 'vue-router';
import AppIcon from './components/AppIcon.vue';
import AppSidebar from './components/AppSidebar.vue';
const route = useRoute();
const sidebarExpanded = ref(false);
const mobileSidebarOpen = ref(false);
const showShell = computed(() => route.name !== 'login');
const __VLS_ctx = {
    ...{},
    ...{},
};
let __VLS_components;
let __VLS_intrinsics;
let __VLS_directives;
if (__VLS_ctx.showShell) {
    let __VLS_0;
    /** @ts-ignore @type { | typeof __VLS_components.vApp | typeof __VLS_components.VApp | typeof __VLS_components['v-app'] | typeof __VLS_components.vApp | typeof __VLS_components.VApp | typeof __VLS_components['v-app']} */
    vApp;
    // @ts-ignore
    const __VLS_1 = __VLS_asFunctionalComponent1(__VLS_0, new __VLS_0({
        ...{ class: ({ 'sidebar-expanded': __VLS_ctx.sidebarExpanded }) },
    }));
    const __VLS_2 = __VLS_1({
        ...{ class: ({ 'sidebar-expanded': __VLS_ctx.sidebarExpanded }) },
    }, ...__VLS_functionalComponentArgsRest(__VLS_1));
    var __VLS_5;
    /** @type {__VLS_StyleScopedClasses['sidebar-expanded']} */ ;
    const { default: __VLS_6 } = __VLS_3.slots;
    const __VLS_7 = AppSidebar;
    // @ts-ignore
    const __VLS_8 = __VLS_asFunctionalComponent1(__VLS_7, new __VLS_7({
        expanded: (__VLS_ctx.sidebarExpanded),
        mobileOpen: (__VLS_ctx.mobileSidebarOpen),
    }));
    const __VLS_9 = __VLS_8({
        expanded: (__VLS_ctx.sidebarExpanded),
        mobileOpen: (__VLS_ctx.mobileSidebarOpen),
    }, ...__VLS_functionalComponentArgsRest(__VLS_8));
    __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
        ...{ onClick: (...[$event]) => {
                if (!(__VLS_ctx.showShell))
                    throw 0;
                return (__VLS_ctx.mobileSidebarOpen = true);
                // @ts-ignore
                [showShell, sidebarExpanded, sidebarExpanded, mobileSidebarOpen, mobileSidebarOpen,];
            } },
        type: "button",
        ...{ class: "mobile-nav-trigger" },
        'aria-label': "Open service navigation",
    });
    /** @type {__VLS_StyleScopedClasses['mobile-nav-trigger']} */ ;
    const __VLS_12 = AppIcon;
    // @ts-ignore
    const __VLS_13 = __VLS_asFunctionalComponent1(__VLS_12, new __VLS_12({
        name: "menu",
    }));
    const __VLS_14 = __VLS_13({
        name: "menu",
    }, ...__VLS_functionalComponentArgsRest(__VLS_13));
    if (__VLS_ctx.mobileSidebarOpen) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.button)({
            ...{ onClick: (...[$event]) => {
                    if (!(__VLS_ctx.showShell))
                        throw 0;
                    if (!(__VLS_ctx.mobileSidebarOpen))
                        throw 0;
                    return (__VLS_ctx.mobileSidebarOpen = false);
                    // @ts-ignore
                    [mobileSidebarOpen, mobileSidebarOpen,];
                } },
            type: "button",
            ...{ class: "sidebar-scrim" },
            'aria-label': "Close service navigation",
        });
        /** @type {__VLS_StyleScopedClasses['sidebar-scrim']} */ ;
    }
    __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
        ...{ class: "app-shell-content" },
    });
    /** @type {__VLS_StyleScopedClasses['app-shell-content']} */ ;
    let __VLS_17;
    /** @ts-ignore @type { | typeof __VLS_components.vMain | typeof __VLS_components.VMain | typeof __VLS_components['v-main'] | typeof __VLS_components.vMain | typeof __VLS_components.VMain | typeof __VLS_components['v-main']} */
    vMain;
    // @ts-ignore
    const __VLS_18 = __VLS_asFunctionalComponent1(__VLS_17, new __VLS_17({}));
    const __VLS_19 = __VLS_18({}, ...__VLS_functionalComponentArgsRest(__VLS_18));
    const { default: __VLS_22 } = __VLS_20.slots;
    let __VLS_23;
    /** @ts-ignore @type { | typeof __VLS_components.routerView | typeof __VLS_components.RouterView | typeof __VLS_components['router-view']} */
    routerView;
    // @ts-ignore
    const __VLS_24 = __VLS_asFunctionalComponent1(__VLS_23, new __VLS_23({}));
    const __VLS_25 = __VLS_24({}, ...__VLS_functionalComponentArgsRest(__VLS_24));
    // @ts-ignore
    [];
    var __VLS_20;
    // @ts-ignore
    [];
    var __VLS_3;
}
else {
    let __VLS_28;
    /** @ts-ignore @type { | typeof __VLS_components.vApp | typeof __VLS_components.VApp | typeof __VLS_components['v-app'] | typeof __VLS_components.vApp | typeof __VLS_components.VApp | typeof __VLS_components['v-app']} */
    vApp;
    // @ts-ignore
    const __VLS_29 = __VLS_asFunctionalComponent1(__VLS_28, new __VLS_28({}));
    const __VLS_30 = __VLS_29({}, ...__VLS_functionalComponentArgsRest(__VLS_29));
    var __VLS_33;
    const { default: __VLS_34 } = __VLS_31.slots;
    let __VLS_35;
    /** @ts-ignore @type { | typeof __VLS_components.routerView | typeof __VLS_components.RouterView | typeof __VLS_components['router-view']} */
    routerView;
    // @ts-ignore
    const __VLS_36 = __VLS_asFunctionalComponent1(__VLS_35, new __VLS_35({}));
    const __VLS_37 = __VLS_36({}, ...__VLS_functionalComponentArgsRest(__VLS_36));
    // @ts-ignore
    [];
    var __VLS_31;
}
// @ts-ignore
[];
const __VLS_export = (await import('vue')).defineComponent({});
export default {};
