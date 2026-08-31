import { createApp } from 'vue'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { aliases, mdi } from 'vuetify/iconsets/mdi-svg'
import 'vuetify/styles'
import App from './App.vue'

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

createApp(App).use(vuetify).mount('#app')
