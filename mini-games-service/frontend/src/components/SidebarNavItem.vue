<script setup>
import AppIcon from './AppIcon.vue';

defineProps({
	label: { type: String, required: true },
	icon: { type: String, required: true },
	showLabel: { type: Boolean, required: true },
	href: { type: String, default: undefined },
	active: { type: Boolean, default: false },
	disabled: { type: Boolean, default: false },
	static: { type: Boolean, default: false },
});

const emit = defineEmits(['click']);
</script>

<template>
	<!-- While the sidebar is collapsed the label appears as a CSS tooltip. -->
	<a
		v-if="href"
		:href="href"
		class="sidebar-nav-item"
		:class="{ active }"
		:data-tooltip="showLabel ? undefined : label"
		:aria-current="active ? 'page' : undefined"
		:aria-label="showLabel ? undefined : label"
	>
		<AppIcon :name="icon" />
		<span v-if="showLabel">{{ label }}</span>
	</a>
	<button
		v-else-if="!static"
		type="button"
		class="sidebar-nav-item"
		:disabled="disabled"
		:data-tooltip="showLabel ? undefined : label"
		:aria-label="showLabel ? undefined : label"
		@click="emit('click')"
	>
		<AppIcon :name="icon" />
		<span v-if="showLabel">{{ label }}</span>
	</button>
	<div
		v-else
		class="sidebar-nav-item sidebar-nav-item-static"
		:data-tooltip="showLabel ? undefined : label"
		:aria-label="showLabel ? undefined : label"
	>
		<AppIcon :name="icon" />
		<span v-if="showLabel">{{ label }}</span>
	</div>
</template>
