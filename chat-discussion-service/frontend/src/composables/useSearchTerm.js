import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';

export function useSearchTerm({ delay = 300 } = {}) {
    const route = useRoute();
    const router = useRouter();

    const activeTerm = computed(() => (typeof route.query.q === 'string' ? route.query.q : ''));
    const term = ref(activeTerm.value);
    let debounce = null;

    watch(term, (value) => {
        window.clearTimeout(debounce);
        debounce = window.setTimeout(() => {
            const next = value.trim();

            if (next === activeTerm.value) {
                return;
            }

            router.replace({ query: next ? { q: next } : {} });
        }, delay);
    });

    watch(activeTerm, (value) => {
        if (value !== term.value.trim()) {
            term.value = value;
        }
    });

    onBeforeUnmount(() => window.clearTimeout(debounce));

    return { term, activeTerm };
}
