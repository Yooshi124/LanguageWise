import { createApp } from 'vue'
import { QuizzesCoursesComponent } from './federation/feature'
import router from './router'

createApp(QuizzesCoursesComponent).use(router).mount('#app')
