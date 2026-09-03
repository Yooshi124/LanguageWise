<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { api, ACCEPTED_IMAGE_TYPES, MAX_IMAGE_BYTES, MAX_IMAGES_PER_POST } from '../api.js';

const props = defineProps({
    existing: { type: Array, default: () => [] },
    busy: { type: Boolean, default: false }
});

const emit = defineEmits(['remove-existing']);

/** The chosen files, as { file, url } entries. The parent uploads them. */
const pending = defineModel({ type: Array, default: () => [] });

/** Rejections are reported one at a time; the first is the one worth reading. */
const message = ref('');

const total = computed(() => props.existing.length + pending.value.length);
const full = computed(() => total.value >= MAX_IMAGES_PER_POST);
const maxMegabytes = Math.round(MAX_IMAGE_BYTES / (1024 * 1024));

function choose(event) {
    const chosen = Array.from(event.target.files ?? []);

    // Clearing the input lets the same file be picked again after it is removed.
    event.target.value = '';

    const accepted = [];
    const problems = [];
    let room = MAX_IMAGES_PER_POST - total.value;

    for (const file of chosen) {
        if (room <= 0) {
            problems.push(`A post can have at most ${MAX_IMAGES_PER_POST} images.`);
            break;
        }

        if (!ACCEPTED_IMAGE_TYPES.includes(file.type)) {
            problems.push(`${file.name} is not a PNG, JPEG, GIF or WebP image.`);
            continue;
        }

        if (file.size > MAX_IMAGE_BYTES) {
            problems.push(`${file.name} is larger than ${maxMegabytes} MB.`);
            continue;
        }

        accepted.push({ file, url: URL.createObjectURL(file) });
        room -= 1;
    }

    pending.value = [...pending.value, ...accepted];
    message.value = problems[0] ?? '';
}

function discard(entry) {
    pending.value = pending.value.filter((item) => item !== entry);
    message.value = '';
}

// A preview holds its file in memory until its object URL is revoked. This component
// created them, so a parent that clears the model need not know they exist.
watch(pending, (current, previous) => {
    const kept = new Set(current);
    (previous ?? [])
        .filter((entry) => !kept.has(entry))
        .forEach((entry) => URL.revokeObjectURL(entry.url));
});

onBeforeUnmount(() => pending.value.forEach((entry) => URL.revokeObjectURL(entry.url)));
</script>

<template>
    <div class="cd-images">
        <p class="cd-images__label" id="post-images-label">Images</p>

        <p class="cd-images__hint">
            PNG, JPEG, GIF or WebP, up to {{ maxMegabytes }} MB each and
            {{ MAX_IMAGES_PER_POST }} per post.
        </p>

        <p v-if="message" class="cd-images__message">{{ message }}</p>

        <ul v-if="existing.length || pending.length" class="cd-images__list">
            <li v-for="image in existing" :key="`stored-${image.id}`" class="cd-images__item">
                <img class="cd-images__thumb" :src="api.imageUrl(image.id)" :alt="image.fileName">
                <button
                    type="button"
                    class="lw-command cd-images__remove"
                    :disabled="busy"
                    @click="emit('remove-existing', image)"
                >
                    Remove
                </button>
            </li>

            <li v-for="entry in pending" :key="entry.url" class="cd-images__item">
                <img class="cd-images__thumb" :src="entry.url" :alt="entry.file.name">
                <button type="button" class="lw-command cd-images__remove" :disabled="busy" @click="discard(entry)">
                    Remove
                </button>
            </li>
        </ul>

        <input
            class="cd-images__input"
            type="file"
            :accept="ACCEPTED_IMAGE_TYPES.join(',')"
            multiple
            :disabled="busy || full"
            aria-labelledby="post-images-label"
            @change="choose"
        >

        <p v-if="full" class="cd-images__hint">
            That is the most a post can have. Remove one to add another.
        </p>
    </div>
</template>
