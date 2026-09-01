import { computed, ref } from 'vue';
import { useAuth } from '../composables/useAuth';
const auth = useAuth();
const username = ref('');
const password = ref('');
const submitting = ref(false);
const errorMessage = ref('');
const canSubmit = computed(() => username.value.trim() !== '' && password.value !== '');
function safeReturnUrl() {
    const requested = new URLSearchParams(window.location.search).get('returnUrl');
    const fallback = document.referrer && new URL(document.referrer).origin === window.location.origin
        ? new URL(document.referrer).pathname
        : '/';
    const resolved = new URL(requested || fallback, window.location.origin);
    return resolved.origin === window.location.origin
        ? `${resolved.pathname}${resolved.search}${resolved.hash}`
        : '/';
}
async function submit() {
    if (!canSubmit.value || submitting.value) {
        return;
    }
    submitting.value = true;
    errorMessage.value = '';
    try {
        await auth.login(username.value.trim(), password.value);
        window.location.assign(safeReturnUrl());
    }
    catch (error) {
        errorMessage.value =
            error instanceof Error ? error.message : 'Unable to reach the server. Please try again later.';
    }
    finally {
        submitting.value = false;
    }
}
const __VLS_ctx = {
    ...{},
    ...{},
};
let __VLS_components;
let __VLS_intrinsics;
let __VLS_directives;
__VLS_asFunctionalElement1(__VLS_intrinsics.main, __VLS_intrinsics.main)({
    ...{ class: "login-page" },
});
/** @type {__VLS_StyleScopedClasses['login-page']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.section, __VLS_intrinsics.section)({
    ...{ class: "login-shell" },
    'aria-labelledby': "login-title",
});
/** @type {__VLS_StyleScopedClasses['login-shell']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "login-brand-panel" },
});
/** @type {__VLS_StyleScopedClasses['login-brand-panel']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.a, __VLS_intrinsics.a)({
    ...{ class: "login-brand" },
    href: "/",
    'aria-label': "LanguageWise home",
});
/** @type {__VLS_StyleScopedClasses['login-brand']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.img)({
    src: "/languagewise-icon.png",
    alt: "",
});
__VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({});
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "login-brand-copy" },
});
/** @type {__VLS_StyleScopedClasses['login-brand-copy']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.h2, __VLS_intrinsics.h2)({});
__VLS_asFunctionalElement1(__VLS_intrinsics.br)({});
__VLS_asFunctionalElement1(__VLS_intrinsics.span, __VLS_intrinsics.span)({});
__VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({});
__VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
    ...{ class: "login-promise" },
});
/** @type {__VLS_StyleScopedClasses['login-promise']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.div, __VLS_intrinsics.div)({
    ...{ class: "login-form-panel" },
});
/** @type {__VLS_StyleScopedClasses['login-form-panel']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
    ...{ class: "login-eyebrow" },
});
/** @type {__VLS_StyleScopedClasses['login-eyebrow']} */ ;
__VLS_asFunctionalElement1(__VLS_intrinsics.h1, __VLS_intrinsics.h1)({
    id: "login-title",
});
__VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
    ...{ class: "login-subtitle" },
});
/** @type {__VLS_StyleScopedClasses['login-subtitle']} */ ;
if (__VLS_ctx.errorMessage) {
    let __VLS_0;
    /** @ts-ignore @type { | typeof __VLS_components.vAlert | typeof __VLS_components.VAlert | typeof __VLS_components['v-alert'] | typeof __VLS_components.vAlert | typeof __VLS_components.VAlert | typeof __VLS_components['v-alert']} */
    vAlert;
    // @ts-ignore
    const __VLS_1 = __VLS_asFunctionalComponent1(__VLS_0, new __VLS_0({
        type: "error",
        variant: "tonal",
        density: "compact",
        ...{ class: "mb-5" },
        role: "alert",
    }));
    const __VLS_2 = __VLS_1({
        type: "error",
        variant: "tonal",
        density: "compact",
        ...{ class: "mb-5" },
        role: "alert",
    }, ...__VLS_functionalComponentArgsRest(__VLS_1));
    /** @type {__VLS_StyleScopedClasses['mb-5']} */ ;
    const { default: __VLS_5 } = __VLS_3.slots;
    (__VLS_ctx.errorMessage);
    // @ts-ignore
    [errorMessage, errorMessage,];
    var __VLS_3;
}
let __VLS_6;
/** @ts-ignore @type { | typeof __VLS_components.vForm | typeof __VLS_components.VForm | typeof __VLS_components['v-form'] | typeof __VLS_components.vForm | typeof __VLS_components.VForm | typeof __VLS_components['v-form']} */
vForm;
// @ts-ignore
const __VLS_7 = __VLS_asFunctionalComponent1(__VLS_6, new __VLS_6({
    ...{ 'onSubmit': {} },
}));
const __VLS_8 = __VLS_7({
    ...{ 'onSubmit': {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_7));
let __VLS_11;
const __VLS_12 = {
    /** @type {typeof __VLS_11.submit} */
    onSubmit: (__VLS_ctx.submit),
};
const { default: __VLS_13 } = __VLS_9.slots;
let __VLS_14;
/** @ts-ignore @type { | typeof __VLS_components.vTextField | typeof __VLS_components.VTextField | typeof __VLS_components['v-text-field']} */
vTextField;
// @ts-ignore
const __VLS_15 = __VLS_asFunctionalComponent1(__VLS_14, new __VLS_14({
    modelValue: (__VLS_ctx.username),
    label: "Username",
    autocomplete: "username",
    variant: "outlined",
    autofocus: true,
    disabled: (__VLS_ctx.submitting),
}));
const __VLS_16 = __VLS_15({
    modelValue: (__VLS_ctx.username),
    label: "Username",
    autocomplete: "username",
    variant: "outlined",
    autofocus: true,
    disabled: (__VLS_ctx.submitting),
}, ...__VLS_functionalComponentArgsRest(__VLS_15));
let __VLS_19;
/** @ts-ignore @type { | typeof __VLS_components.vTextField | typeof __VLS_components.VTextField | typeof __VLS_components['v-text-field']} */
vTextField;
// @ts-ignore
const __VLS_20 = __VLS_asFunctionalComponent1(__VLS_19, new __VLS_19({
    modelValue: (__VLS_ctx.password),
    label: "Password",
    type: "password",
    autocomplete: "current-password",
    variant: "outlined",
    disabled: (__VLS_ctx.submitting),
}));
const __VLS_21 = __VLS_20({
    modelValue: (__VLS_ctx.password),
    label: "Password",
    type: "password",
    autocomplete: "current-password",
    variant: "outlined",
    disabled: (__VLS_ctx.submitting),
}, ...__VLS_functionalComponentArgsRest(__VLS_20));
let __VLS_24;
/** @ts-ignore @type { | typeof __VLS_components.vBtn | typeof __VLS_components.VBtn | typeof __VLS_components['v-btn'] | typeof __VLS_components.vBtn | typeof __VLS_components.VBtn | typeof __VLS_components['v-btn']} */
vBtn;
// @ts-ignore
const __VLS_25 = __VLS_asFunctionalComponent1(__VLS_24, new __VLS_24({
    type: "submit",
    color: "primary",
    size: "large",
    block: true,
    loading: (__VLS_ctx.submitting),
    disabled: (!__VLS_ctx.canSubmit),
}));
const __VLS_26 = __VLS_25({
    type: "submit",
    color: "primary",
    size: "large",
    block: true,
    loading: (__VLS_ctx.submitting),
    disabled: (!__VLS_ctx.canSubmit),
}, ...__VLS_functionalComponentArgsRest(__VLS_25));
const { default: __VLS_29 } = __VLS_27.slots;
// @ts-ignore
[submit, username, submitting, submitting, submitting, password, canSubmit,];
var __VLS_27;
// @ts-ignore
[];
var __VLS_9;
var __VLS_10;
__VLS_asFunctionalElement1(__VLS_intrinsics.p, __VLS_intrinsics.p)({
    ...{ class: "login-account-note" },
});
/** @type {__VLS_StyleScopedClasses['login-account-note']} */ ;
// @ts-ignore
[];
const __VLS_export = (await import('vue')).defineComponent({});
export default {};
