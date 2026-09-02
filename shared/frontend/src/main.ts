import { createApp } from 'vue'
import { VueQueryPlugin } from '@tanstack/vue-query'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { aliases, mdi } from 'vuetify/iconsets/mdi-svg'
import 'vuetify/styles'
import App from './App.vue'
import router from './router'
import './styles.css'
import './features/quizzes-courses.css'
import './features/mini-games.css'
import './features/chat-discussion.css'
import './features/quests-achievements.css'

const vuetify = createVuetify({
  components,
  directives,
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: { mdi },
  },
  theme: {
    defaultTheme: 'languageWise',
    themes: {
      languageWise: {
        dark: false,
        colors: {
          primary: '#4f46e5',
          secondary: '#0f766e',
          surface: '#ffffff',
          background: '#f6f7fb',
          error: '#b42318',
        },
      },
    },
  },
})

createApp(App).use(router).use(vuetify).use(VueQueryPlugin).mount('#app')
