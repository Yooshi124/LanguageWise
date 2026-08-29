import { createApp } from 'vue'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import 'vuetify/styles'
import App from './App.vue'
import router from './router'
import './styles.css'

const vuetify = createVuetify({
  components,
  directives,
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

createApp(App).use(router).use(vuetify).mount('#app')
