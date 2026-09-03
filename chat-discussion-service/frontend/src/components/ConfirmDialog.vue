<script setup>
defineProps({
    modelValue: { type: Boolean, default: false },
    title: { type: String, default: 'Are you sure?' },
    message: { type: String, default: '' },
    confirmLabel: { type: String, default: 'Delete' },
    cancelLabel: { type: String, default: 'Cancel' },
    busy: { type: Boolean, default: false }
});

const emit = defineEmits(['update:modelValue', 'confirm']);
</script>

<template>
    <v-dialog
        :model-value="modelValue"
        max-width="420"
        @update:model-value="emit('update:modelValue', $event)"
    >
        <v-card class="cd-confirm">
            <v-card-title class="cd-confirm__title">{{ title }}</v-card-title>
            <v-card-text v-if="message" class="cd-confirm__message">{{ message }}</v-card-text>
            <v-card-actions class="cd-confirm__actions">
                <v-spacer />
                <v-btn variant="text" :disabled="busy" @click="emit('update:modelValue', false)">
                    {{ cancelLabel }}
                </v-btn>
                <v-btn color="error" variant="flat" :loading="busy" @click="emit('confirm')">
                    {{ confirmLabel }}
                </v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
</template>
