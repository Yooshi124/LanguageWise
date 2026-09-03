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
