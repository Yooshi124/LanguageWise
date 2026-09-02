<script setup>
import { api } from '../api.js';

defineProps({
    images: { type: Array, default: () => [] }
});
</script>

<template>
    <ul v-if="images.length" class="cd-gallery">
        <li v-for="image in images" :key="image.id" class="cd-gallery__item">
            <!-- The link opens the image at its own size; the figure is column-width. -->
            <a :href="api.imageUrl(image.id)" target="_blank" rel="noopener">
                <img class="cd-gallery__image" :src="api.imageUrl(image.id)" :alt="image.fileName" loading="lazy">
            </a>
        </li>
    </ul>
</template>

<style scoped>
.cd-gallery {
    display: flex;
    flex-wrap: wrap;
    gap: 0.75rem;
    margin: 0 0 1.25rem;
    padding: 0;
    list-style: none;
}

.cd-gallery__item {
    flex: 1 1 14rem;
    max-width: 100%;
}

.cd-gallery__image {
    display: block;
    width: 100%;
    max-height: 22rem;
    object-fit: contain;
    border: 1px solid var(--lw-colour-border);
    border-radius: var(--lw-radius-sm);
    background: var(--lw-colour-bg);
}
</style>
