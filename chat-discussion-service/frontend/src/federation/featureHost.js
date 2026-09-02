let hostContext = null;

export function setFeatureHostContext(context) {
    hostContext = context;
}

export function getFeatureUser() {
    return hostContext?.user ?? null;
}

export function hasFeatureHostContext() {
    return hostContext !== null;
}

export function redirectToSignIn(returnUrl) {
    hostContext?.signIn(returnUrl);
}

export async function signOut() {
    await hostContext?.signOut();
}