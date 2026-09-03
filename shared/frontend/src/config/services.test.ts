import { describe, expect, it } from 'vitest'
import { serviceMappings } from './services'

describe('serviceMappings', () => {
  it('describes the deployed Leaderboard and Analytics service', () => {
    expect(serviceMappings.find(({ service }) => service === 'Leaderboard & Analytics')).toEqual({
      service: 'Leaderboard & Analytics',
      gateway: '/analytics/',
      frontend: 'Gateway -> leaderboard-analytics-service-frontend:80',
      backend: 'localhost:5005 -> leaderboard-analytics-service-backend:8080',
      database: 'None (fetches data on demand from quizzes-courses-service)',
      technology: 'HTTP client (no local storage)',
    })
  })
})