import AppIcon from './AppIcon.vue';
const __VLS_props = defineProps();
const emit = defineEmits();
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
let __VLS_0;
/** @ts-ignore @type { | typeof __VLS_components.vTooltip | typeof __VLS_components.VTooltip | typeof __VLS_components['v-tooltip'] | typeof __VLS_components.vTooltip | typeof __VLS_components.VTooltip | typeof __VLS_components['v-tooltip']} */
vTooltip;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent1(__VLS_0, new __VLS_0({
    text: (__VLS_ctx.label),
    location: "end",
    disabled: (__VLS_ctx.showLabel),
}));
const __VLS_2 = __VLS_1({
    text: (__VLS_ctx.label),
    location: "end",
    disabled: (__VLS_ctx.showLabel),
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
var __VLS_5;
const { default: __VLS_6 } = __VLS_3.slots;
{
    const { activator: __VLS_7 } = __VLS_3.slots;
    const [{ props }] = __VLS_vSlot(__VLS_7);
    if (__VLS_ctx.href) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.a, __VLS_intrinsics.a)({
            ...(props),
            href: (__VLS_ctx.href),
            ...{ class: "sidebar-nav-item" },
            ...{ class: ({ active: __VLS_ctx.active }) },
            'aria-current': (__VLS_ctx.active ? 'page' : undefined),
            'aria-label': (__VLS_ctx.showLabel ? undefined : __VLS_ctx.label),
        });
        /** @type {__VLS_StyleScopedClasses['sidebar-nav-item']} */ ;
        /** @type {__VLS_StyleScopedClasses['active']} */ ;
        const __VLS_8 = AppIcon;
        // @ts-ignore
        const __VLS_9 = __VLS_asFunctionalComponent1(__VLS_8, new __VLS_8({
            name: (__VLS_ctx.icon),
        }));
        const __VLS_10 = __VLS_9({
            name: (__VLS_ctx.icon),
        }, ...__VLS_functionalComponentArgsRest(__VLS_9));
        if (__VLS_ctx.showLabel) {
            __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({});
            (__VLS_ctx.label);
        }
    }
    else if (!__VLS_ctx.static) {
        __VLS_asFunctionalElement1(__VLS_intrinsics.button, __VLS_intrinsics.button)({
            ...{ onClick: (...[$event]) => {
                    if (!!(__VLS_ctx.href))
                        throw 0;
                    if (!(!__VLS_ctx.static))
                        throw 0;
                    return (__VLS_ctx.emit('click'));
                    // @ts-ignore
                    [label, label, label, showLabel, showLabel, showLabel, href, href, active, active, icon, static, emit,];
                } },
            ...(props),
            type: "button",
            ...{ class: "sidebar-nav-item" },
            disabled: (__VLS_ctx.disabled),
            'aria-label': (__VLS_ctx.showLabel ? undefined : __VLS_ctx.label),
        });
        /** @type {__VLS_StyleScopedClasses['sidebar-nav-item']} */ ;
        const __VLS_13 = AppIcon;
        // @ts-ignore
        const __VLS_14 = __VLS_asFunctionalComponent1(__VLS_13, new __VLS_13({
            name: (__VLS_ctx.icon),
        }));
        const __VLS_15 = __VLS_14({
            name: (__VLS_ctx.icon),
        }, ...__VLS_functionalComponentArgsRest(__VLS_14));
        if (__VLS_ctx.showLabel) {
            __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({});
            (__VLS_ctx.label);
        }
    }
    else {
        __VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
            ...(props),
            ...{ class: "sidebar-nav-item sidebar-nav-item-static" },
            'aria-label': (__VLS_ctx.showLabel ? undefined : __VLS_ctx.label),
        });
        /** @type {__VLS_StyleScopedClasses['sidebar-nav-item']} */ ;
        /** @type {__VLS_StyleScopedClasses['sidebar-nav-item-static']} */ ;
        const __VLS_18 = AppIcon;
        // @ts-ignore
        const __VLS_19 = __VLS_asFunctionalComponent1(__VLS_18, new __VLS_18({
            name: (__VLS_ctx.icon),
        }));
        const __VLS_20 = __VLS_19({
            name: (__VLS_ctx.icon),
        }, ...__VLS_functionalComponentArgsRest(__VLS_19));
        if (__VLS_ctx.showLabel) {
            __VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({});
            (__VLS_ctx.label);
        }
    }
    // @ts-ignore
    [label, label, label, label, showLabel, showLabel, showLabel, showLabel, icon, icon, disabled,];
}
// @ts-ignore
[];
var __VLS_3;
// @ts-ignore
[];
const __VLS_export = (await import('vue')).defineComponent({
    __typeEmits: {},
    __typeProps: {},
});
export default {};
