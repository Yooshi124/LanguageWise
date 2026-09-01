<script setup>
import { ref } from 'vue';
import AppIcon from './components/AppIcon.vue';
import AppSidebar from './components/AppSidebar.vue';
import GamePage from './GamePage.vue';
import GuessTheWord from './GuessTheWord.vue';
import WordSearch from './WordSearch.vue';
import Associations from './Associations.vue';

const routes = {
	'/': GamePage,
	'/game': GamePage,
	'/game/guess-the-word': GuessTheWord,
	'/game/word-search': WordSearch,
	'/game/associations': Associations,
};

// Through the gateway the app is served under its base path (e.g. /mini-games/),
// so strip that prefix before resolving the route.
const path = window.location.pathname.replace(import.meta.env.BASE_URL, '/');
const page = routes[path] ?? GamePage;

const sidebarExpanded = ref(false);
const mobileSidebarOpen = ref(false);
</script>

<template>
	<div class="app-shell" :class="{ 'sidebar-expanded': sidebarExpanded }">
		<AppSidebar v-model:expanded="sidebarExpanded" v-model:mobile-open="mobileSidebarOpen" />
		<button
			type="button"
			class="mobile-nav-trigger"
			aria-label="Open service navigation"
			@click="mobileSidebarOpen = true"
		>
			<AppIcon name="menu" />
		</button>
		<button
			v-if="mobileSidebarOpen"
			type="button"
			class="sidebar-scrim"
			aria-label="Close service navigation"
			@click="mobileSidebarOpen = false"
		/>
		<div class="app-shell-content">
			<component :is="page" />
		</div>
	</div>
</template>
