export const ACCEPTED_PICTURE_TYPES = ['image/png', 'image/jpeg', 'image/gif', 'image/webp']
export const MAX_PICTURE_BYTES = 5 * 1024 * 1024

export interface PendingPicture {
  file: File
  url: string
}

export interface ProfilePicture {
  fileName: string
  contentType: string
  sizeBytes: number
  uploadedAt: string
}

export function profilePictureContentUrl(userId: number, picture: ProfilePicture) {
  return `/api/users/${userId}/profile-picture/content?v=${encodeURIComponent(picture.uploadedAt)}`
}

export async function fetchProfilePicture(userId: number): Promise<ProfilePicture | null> {
  const response = await fetch(`/api/users/${userId}/profile-picture`, {
    credentials: 'same-origin',
    headers: { Accept: 'application/json' },
  })
  if (response.status === 404) {
    return null
  }
  if (!response.ok) {
    throw new Error(`Unable to load your profile picture (${response.status} ${response.statusText})`)
  }
  return response.json()
}

export async function uploadProfilePicture(userId: number, file: File): Promise<ProfilePicture> {
  const body = new FormData()
  body.append('file', file)
  const response = await fetch(`/api/users/${userId}/profile-picture`, {
    method: 'PUT',
    credentials: 'same-origin',
    body,
  })
  if (response.status === 400) {
    const problem: { errors?: Record<string, string[]> } = await response.json().catch(() => ({}))
    throw new Error(Object.values(problem.errors ?? {}).flat()[0] ?? 'That image could not be used.')
  }
  if (!response.ok) {
    throw new Error(`Unable to upload your picture (${response.status} ${response.statusText})`)
  }
  return response.json()
}

export async function deleteProfilePicture(userId: number) {
  const response = await fetch(`/api/users/${userId}/profile-picture`, {
    method: 'DELETE',
    credentials: 'same-origin',
  })
  if (!response.ok && response.status !== 404) {
    throw new Error(`Unable to remove your picture (${response.status} ${response.statusText})`)
  }
}
