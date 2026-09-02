import { computed } from 'vue';

let hostContext = null;

export function setFeatureHostContext(context) {
    hostContext = context;
}

export function getFeatureUser() {
    return hostContext?.user ?? null;
}

export function useFeatureUser() {
    return computed(() => getFeatureUser());
}

export function isOwnedByFeatureUser(item) {
    const user = getFeatureUser();
    return user !== null && item != null && item.userId === user.id;
}

export async function signOut() {
    await hostContext?.signOut();
}