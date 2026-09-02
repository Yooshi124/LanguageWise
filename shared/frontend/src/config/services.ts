import { featureServices, type AppIconName } from './navigation'

export interface ModuleSummary {
  name: string
  description: string
  href: string
  icon: AppIconName
  color: string
}

export interface ServiceMapping {
  service: string
  gateway: string
  frontend: string
  backend: string
  database: string
  technology: string
}

export const moduleSummaries: readonly ModuleSummary[] = featureServices.map((service) => ({
  name: service.label,
  description: service.description ?? '',
  href: service.href,
  icon: service.icon,
  color: service.color ?? 'primary',
}))

export const serviceMappings: readonly ServiceMapping[] = [
  {
    service: 'Shared',
    gateway: '/',
    frontend: 'localhost:3000 -> shared-frontend:80',
    backend: 'localhost:5000 -> shared-backend:8080',
    database: 'localhost:6000 -> shared-db:8080',
    technology: 'SQLite',
  },
  {
    service: 'Mini Games',
    gateway: '/mini-games/',
    frontend: 'Gateway -> mini-games-service-frontend:80',
    backend: 'localhost:5001 -> mini-games-service-backend:8080',
    database: 'localhost:6005 -> mini-games-service-db:8080',
    technology: 'SQLite',
  },
  {
    service: 'Discussion Forum',
    gateway: '/chat-discussion/',
    frontend: 'localhost:3002 and gateway -> chat-discussion-service-frontend:80',
    backend: 'localhost:5002 -> chat-discussion-service-backend:8080',
    database: 'localhost:6002 -> chat-discussion-service-db:8080',
    technology: 'SQLite',
  },
  {
    service: 'Quizzes & Courses',
    gateway: '/quizzes-and-courses/',
    frontend: 'Gateway -> quizzes-courses-service-frontend:80',
    backend: 'localhost:5003 -> quizzes-courses-service-backend:8080',
    database: 'localhost:6003 -> quizzes-courses-service-db:8080',
    technology: 'SQLite',
  },
  {
    service: 'Quests & Achievements',
    gateway: '/quests-and-achievements/',
    frontend: 'Gateway -> quests-achievements-notifications-service-frontend:80',
    backend: 'localhost:5004 -> quests-achievements-notifications-service-backend:8080',
    database: 'Internal PostgREST:3000 -> PostgreSQL:5432',
    technology: 'PostgreSQL + PostgREST',
  },
  {
    service: 'Leaderboard & Analytics',
    gateway: '/analytics/',
    frontend: 'Gateway -> leaderboard-analytics-service-frontend:80',
    backend: 'localhost:5005 -> leaderboard-analytics-service-backend:8080',
    database: 'localhost:5006 -> leaderboard-analytics-service-db:8080',
    technology: 'SQLite',
  },
]
