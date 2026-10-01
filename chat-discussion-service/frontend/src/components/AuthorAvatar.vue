<script setup>
import { computed, ref, watch } from 'vue';

const props = defineProps({
    userId: { type: Number, required: true },
    name: { type: String, default: '' }
});

const failed = ref(false);

// The shell routes /api/ to the shared backend, which owns profile pictures.
const src = computed(() => `/api/users/${props.userId}/profile-picture/content`);
const initial = computed(() => (props.name.trim().charAt(0) || '?').toUpperCase());

watch(() => props.userId, () => {
    failed.value = false;
});
</script>

<template>
    <span class="cd-avatar" aria-hidden="true">
        <img v-if="!failed" :src="src" alt="" class="cd-avatar__image" @error="failed = true" />
        <span v-else class="cd-avatar__initial">{{ initial }}</span>
    </span>
</template>
