import { createApp } from 'vue';
import App from './App.vue';
import './styles.css';

// App.vue owns routing: it resolves the current path (relative to the app
// base) to a game page and renders it inside the shared sidebar shell.
createApp(App).mount('#app');