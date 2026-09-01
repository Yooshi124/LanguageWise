import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { useAuth } from '../composables/useAuth';
import { serviceNavigation } from '../config/navigation';
import AppIcon from './AppIcon.vue';
import SidebarNavItem from './SidebarNavItem.vue';
const auth = useAuth();
const loggingOut = ref(false);
const logoutError = ref('');
const accountLabel = computed(() => {
    if (auth.status.value === 'authenticated') {
        return auth.username.value ?? 'Logged in';
    }
    if (auth.status.value === 'signed-out') {
        return 'Not logged in';
    }
    if (auth.status.value === 'error') {
        return 'Unable to verify login';
    }
    return 'Checking login';
});
const accountHref = computed(() => auth.status.value === 'signed-out' ? auth.loginUrl() : undefined);
const __VLS_props = defineProps();
const emit = defineEmits();
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
    const sidebar = event.currentTarget;
    if (!sidebar.contains(event.relatedTarget)) {
        setExpanded(false);
    }
}
async function handleLogout() {
    loggingOut.value = true;
    logoutError.value = '';
    try {
        await auth.logout();
    }
    catch (error) {
        logoutError.value = error instanceof Error ? error.message : 'Unable to log out';
    }
    finally {
        loggingOut.value = false;
    }
}
onMounted(() => auth.ensureAuthenticated().catch(() => undefined));
onBeforeUnmount(() => clearTimeout(hoverTimer));
const __VLS_ctx = {
    ...{},
    ...{},
    ...{},
    ...{},
    ...{},
};
let __VLS_components;
let __VLS_intrinsics;
let __VLS_directives;
__VLS_asFunctionalElement1(__VLS_intrinsics.aside, __VLS_intrinsics.aside)({
    ...{ onMouseenter: (...[$event]) => {
            return (__VLS_ctx.scheduleExpanded(true));
            // @ts-ignore
            [scheduleExpanded,];
        } },
    ...{ onMouseleave: (...[$event]) => {
            return (__VLS_ctx.scheduleExpanded(false));
            // @ts-ignore
            [scheduleExpanded,];
        } },
    ...{ onFocusin: (...[$event]) => {
            return (__VLS_ctx.setExpanded(true));
            // @ts-ignore
            [setExpanded,];
        } },
    ...{ onFocusout: (__VLS_ctx.handleFocusOut) },
    ...{ class: "app-sidebar" },
    ...{ class: ({ expanded: __VLS_ctx.expanded, 'mobile-open': __VLS_ctx.mobileOpen }) },
    'aria-label': "LanguageWise services",
});
/** @type {__VLS_StyleScopedClasses['app-sidebar']} */ ;
/** @type {__VLS_StyleScopedClasses['expanded']} */ ;
/** @type {__VLS_StyleScopedClasses['mobile-open']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "sidebar-header" },
});
/** @type {__VLS_StyleScopedClasses['sidebar-header']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.a, __VLS_intrinsics.a)({
    href: "/",
    ...{ class: "sidebar-brand" },
    'aria-label': (__VLS_ctx.expanded || __VLS_ctx.mobileOpen ? undefined : 'LanguageWise home'),
});
/** @type {__VLS_StyleScopedClasses['sidebar-brand']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.img)({
    ...{ class: "brand-mark" },
    src: "/languagewise-icon.png",
    alt: "",
});
/** @type {__VLS_StyleScopedClasses['brand-mark']} */ ;
if (__VLS_ctx.expanded || __VLS_ctx.mobileOpen) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({
        ...{ class: "brand-name" },
    });
    /** @type {__VLS_StyleScopedClasses['brand-name']} */ ;
}
__VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
    ...{ onClick: (...[$event]) => {
            return (__VLS_ctx.emit('update:mobileOpen', false));
            // @ts-ignore
            [handleFocusOut, expanded, expanded, expanded, mobileOpen, mobileOpen, mobileOpen, emit,];
        } },
    type: "button",
    ...{ class: "sidebar-toggle mobile-sidebar-close" },
    'aria-label': "Close service navigation",
});
/** @type {__VLS_StyleScopedClasses['sidebar-toggle']} */ ;
/** @type {__VLS_StyleScopedClasses['mobile-sidebar-close']} */ ;
const __VLS_0 = AppIcon;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent1(__VLS_0, new __VLS_0({
    name: "close",
}));
const __VLS_2 = __VLS_1({
    name: "close",
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
__VLS_asFunctionalElement1(__VLS_intrinsics.nav, __VLS_intrinsics.nav)({
    ...{ class: "sidebar-services" },
    'aria-label': "Services",
});
/** @type {__VLS_StyleScopedClasses['sidebar-services']} */ ;
for (const [item] of __VLS_vFor((__VLS_ctx.serviceNavigation))) {
    const __VLS_5 = SidebarNavItem;
    // @ts-ignore
    const __VLS_6 = __VLS_asFunctionalComponent1(__VLS_5, new __VLS_5({
        key: (item.label),
        label: (item.label),
        icon: (item.icon),
        href: (item.href),
        active: (item.current),
        showLabel: (__VLS_ctx.expanded || __VLS_ctx.mobileOpen),
    }));
    const __VLS_7 = __VLS_6({
        key: (item.label),
        label: (item.label),
        icon: (item.icon),
        href: (item.href),
        active: (item.current),
        showLabel: (__VLS_ctx.expanded || __VLS_ctx.mobileOpen),
    }, ...__VLS_functionalComponentArgsRest(__VLS_6));
    // @ts-ignore
    [expanded, mobileOpen, serviceNavigation,];
}
__VLS_asFunctionalElement1(__VLS_intrinsics.nav, __VLS_intrinsics.nav)({
    ...{ class: "sidebar-utilities" },
    'aria-label': "Account",
});
/** @type {__VLS_StyleScopedClasses['sidebar-utilities']} */ ;
const __VLS_10 = SidebarNavItem;
// @ts-ignore
const __VLS_11 = __VLS_asFunctionalComponent1(__VLS_10, new __VLS_10({
    label: (__VLS_ctx.accountLabel),
    icon: "profile",
    href: (__VLS_ctx.accountHref),
    static: (!__VLS_ctx.accountHref),
    showLabel: (__VLS_ctx.expanded || __VLS_ctx.mobileOpen),
}));
const __VLS_12 = __VLS_11({
    label: (__VLS_ctx.accountLabel),
    icon: "profile",
    href: (__VLS_ctx.accountHref),
    static: (!__VLS_ctx.accountHref),
    showLabel: (__VLS_ctx.expanded || __VLS_ctx.mobileOpen),
}, ...__VLS_functionalComponentArgsRest(__VLS_11));
if (__VLS_ctx.auth.isAuthenticated.value) {
    const __VLS_15 = SidebarNavItem;
    // @ts-ignore
    const __VLS_16 = __VLS_asFunctionalComponent1(__VLS_15, new __VLS_15({
        ...{ 'onClick': {} },
        label: (__VLS_ctx.loggingOut ? 'Logging out' : 'Logout'),
        icon: "logout",
        disabled: (__VLS_ctx.loggingOut),
        showLabel: (__VLS_ctx.expanded || __VLS_ctx.mobileOpen),
    }));
    const __VLS_17 = __VLS_16({
        ...{ 'onClick': {} },
        label: (__VLS_ctx.loggingOut ? 'Logging out' : 'Logout'),
        icon: "logout",
        disabled: (__VLS_ctx.loggingOut),
        showLabel: (__VLS_ctx.expanded || __VLS_ctx.mobileOpen),
    }, ...__VLS_functionalComponentArgsRest(__VLS_16));
    let __VLS_20;
    const __VLS_21 = {
        /** @type {typeof __VLS_20.click} */
        onClick: (__VLS_ctx.handleLogout),
    };
    var __VLS_18;
    var __VLS_19;
}
if (__VLS_ctx.logoutError) {
    __VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
        ...{ class: "sidebar-account-error" },
        role: "alert",
    });
    /** @type {__VLS_StyleScopedClasses['sidebar-account-error']} */ ;
    (__VLS_ctx.logoutError);
}
// @ts-ignore
[expanded, expanded, mobileOpen, mobileOpen, accountLabel, accountHref, accountHref, auth, loggingOut, loggingOut, handleLogout, logoutError, logoutError,];
const __VLS_export = (await import('vue')).defineComponent({
    __typeEmits: {},
    __typeProps: {},
});
export default {};
