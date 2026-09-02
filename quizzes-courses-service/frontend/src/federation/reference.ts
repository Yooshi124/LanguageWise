import ReferenceFeature from './ReferenceFeature.vue'
import type { FederatedFeatureModule } from './contracts'

export const metadata: FederatedFeatureModule['metadata'] = {
  key: 'quizzes-courses-reference',
  displayName: 'Federation reference remote',
  icon: 'courses',
  basePath: '/federation-spike',
  requiresAuth: false,
}

export const routes: FederatedFeatureModule['routes'] = [
  { path: '', name: 'federation-reference', component: ReferenceFeature },
  { path: 'details', name: 'federation-reference-details', component: ReferenceFeature },
]