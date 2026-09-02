import { computed, readonly, ref } from 'vue'

type AuthStatus = 'loading' | 'authenticated' | 'signed-out' | 'error'

interface AuthenticatedUserResponse {
  id: number
  name: string
}

const username = ref<string | null>(null)
const status = ref<AuthStatus>('loading')
let authRequest: Promise<boolean> | undefined

function currentReturnUrl() {
  return `${window.location.pathname}${window.location.search}${window.location.hash}`
}

export function loginUrl() {
  const url = new URL('/login.html', window.location.origin)
  url.searchParams.set('returnUrl', currentReturnUrl())
  return url.toString()
}

function markSignedOut() {
  username.value = null
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
      response = await fetch('/api/check-login', {
        method: 'POST',
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

    const user = (await response.json()) as AuthenticatedUserResponse
    username.value = user.name
    status.value = 'authenticated'
    return true
  })().finally(() => {
    authRequest = undefined
  })

  return authRequest
}

export async function login(usernameValue: string, password: string) {
  const response = await fetch('/api/login', {
    method: 'POST',
    credentials: 'same-origin',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: usernameValue, password }),
  })

  if (response.status === 401) {
    throw new Error('Invalid username or password. Please try again.')
  }

  if (!response.ok) {
    throw new Error(`Unable to sign in (${response.status} ${response.statusText})`)
  }

  username.value = usernameValue
  status.value = 'authenticated'
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
    username: readonly(username),
    status: readonly(status),
    isAuthenticated: computed(() => status.value === 'authenticated'),
    ensureAuthenticated,
    login,
    loginUrl,
    logout,
  }
}
