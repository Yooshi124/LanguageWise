import { computed, readonly, ref } from 'vue'

export interface AuthenticatedUser {
  id: number
  username: string
}

type AuthStatus = 'loading' | 'authenticated' | 'signed-out' | 'error'

const apiBase = `${import.meta.env.BASE_URL}api`

const user = ref<AuthenticatedUser | null>(null)
const status = ref<AuthStatus>('loading')
let authRequest: Promise<boolean> | undefined

function currentReturnUrl() {
  return `${window.location.pathname}${window.location.search}${window.location.hash}`
}

export function loginUrl() {
  return `/login.html?returnUrl=${encodeURIComponent(currentReturnUrl())}`
}

export function markSignedOut() {
  user.value = null
  status.value = 'signed-out'
}

export async function ensureAuthenticated(): Promise<boolean> {
  if (status.value === 'authenticated') {
    return true
  }

  if (authRequest) {
    return authRequest
  }

  status.value = 'loading'
  authRequest = (async () => {
    let response: Response

    try {
      response = await fetch(`${apiBase}/me`, {
        credentials: 'same-origin',
        headers: { Accept: 'application/json' },
      })
    } catch (error) {
      status.value = 'error'
      throw error
    }

    if (response.status === 401) {
      markSignedOut()
      return false
    }

    if (!response.ok) {
      status.value = 'error'
      throw new Error(`Unable to verify login (${response.status} ${response.statusText})`)
    }

    user.value = (await response.json()) as AuthenticatedUser
    status.value = 'authenticated'
    return true
  })().finally(() => {
    authRequest = undefined
  })

  return authRequest
}

export async function logout() {
  const response = await fetch('/api/logout', {
    method: 'POST',
    credentials: 'same-origin',
  })

  if (!response.ok) {
    throw new Error(`Unable to log out (${response.status} ${response.statusText})`)
  }

  markSignedOut()
}

export function useAuth() {
  return {
    user: readonly(user),
    status: readonly(status),
    isAuthenticated: computed(() => status.value === 'authenticated'),
    ensureAuthenticated,
    loginUrl,
    logout,
  }
}
