<script setup>
defineProps({
    title: { type: String, required: true },
    message: { type: String, default: '' },
    tone: { type: String, default: 'neutral' },
    retryLabel: { type: String, default: '' }
});

defineEmits(['retry']);
</script>

<template>
    <div class="lw-card lw-state" :class="{ 'lw-state--error': tone === 'error' }">
        <h2 class="lw-card__title">{{ title }}</h2>
        <p v-if="message" class="lw-card__hint">{{ message }}</p>
        <slot></slot>
        <p v-if="retryLabel" class="cd-state__actions">
            <button type="button" class="lw-command" @click="$emit('retry')">{{ retryLabel }}</button>
        </p>
    </div>
</template>
