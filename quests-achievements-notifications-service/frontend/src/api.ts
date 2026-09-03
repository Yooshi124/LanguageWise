import type { Preferences, Profile } from './models'

const base = '/quests-and-achievements/api'

export class ApiError extends Error {
  constructor(public readonly status: number) {
    super(`Request failed with status ${status}`)
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${base}${path}`, { credentials: 'same-origin', ...init })
  } catch {
    throw new ApiError(0)
  }
  if (!response.ok) throw new ApiError(response.status)
  return response.json() as Promise<T>
}

export const profileApi = {
  load: () => request<Profile>('/profile'),
  savePreferences: (preferences: Preferences) => request<{ message: string }>('/preferences', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(preferences),
  }),
}