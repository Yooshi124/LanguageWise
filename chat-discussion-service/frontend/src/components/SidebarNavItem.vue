<script setup>
import AppIcon from './AppIcon.vue';

defineProps({
    label: { type: String, required: true },
    icon: { type: String, required: true },
    showLabel: { type: Boolean, required: true },
    href: { type: String, default: undefined },
    active: { type: Boolean, default: false },
    disabled: { type: Boolean, default: false },
    static: { type: Boolean, default: false }
});

const emit = defineEmits(['click']);
</script>

<template>
    <v-tooltip :text="label" location="end" :disabled="showLabel">
        <template #activator="{ props }">
            <a
                v-if="href"
                v-bind="props"
                :href="href"
                class="sidebar-nav-item"
                :class="{ active }"
                :aria-current="active ? 'page' : undefined"
                :aria-label="showLabel ? undefined : label"
            >
                <AppIcon :name="icon" />
                <span v-if="showLabel">{{ label }}</span>
            </a>
            <button
                v-else-if="!static"
                v-bind="props"
                type="button"
                class="sidebar-nav-item"
                :disabled="disabled"
                :aria-label="showLabel ? undefined : label"
                @click="emit('click')"
            >
                <AppIcon :name="icon" />
                <span v-if="showLabel">{{ label }}</span>
            </button>
            <div
                v-else
                v-bind="props"
                class="sidebar-nav-item sidebar-nav-item-static"
                :aria-label="showLabel ? undefined : label"
            >
                <AppIcon :name="icon" />
                <span v-if="showLabel">{{ label }}</span>
            </div>
        </template>
    </v-tooltip>
</template>
