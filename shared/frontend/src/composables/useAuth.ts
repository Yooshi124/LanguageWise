import { computed, readonly, ref } from 'vue'

type AuthStatus = 'loading' | 'authenticated' | 'signed-out' | 'error'

export interface AuthenticatedUser {
  id: number
  name: string
}

export interface AccountChanges {
  username?: string
  newPassword?: string
  currentPassword: string
}

const user = ref<AuthenticatedUser | null>(null)
const status = ref<AuthStatus>('loading')
let authRequest: Promise<boolean> | undefined

function currentReturnUrl() {
  return `${window.location.pathname}${window.location.search}${window.location.hash}`
}

export function loginUrl() {
  const url = new URL('/login', window.location.origin)
  url.searchParams.set('returnUrl', currentReturnUrl())
  return url.toString()
}

function markSignedOut() {
  user.value = null
  status.value = 'signed-out'
}

function isAuthenticatedUser(value: unknown): value is AuthenticatedUser {
  if (!value || typeof value !== 'object') {
    return false
  }

  const candidate = value as Partial<AuthenticatedUser>
  return (
    typeof candidate.id === 'number' &&
    candidate.id > 0 &&
    typeof candidate.name === 'string' &&
    candidate.name.trim() !== ''
  )
}

export async function ensureAuthenticated(): Promise<boolean> {
  if (status.value === 'authenticated') {
    return true
  }

  if (status.value === 'signed-out') {
    return false
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

    const authenticatedUser: unknown = await response.json()
    if (!isAuthenticatedUser(authenticatedUser)) {
      status.value = 'error'
      throw new Error('The session response was invalid.')
    }

    user.value = authenticatedUser
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

  user.value = null
  status.value = 'loading'
  if (!(await ensureAuthenticated())) {
    throw new Error('Unable to verify the new session.')
  }
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

async function accountError(response: Response, action: string) {
  if (response.status === 409) {
    return new Error('That username is already taken.')
  }

  if (response.status === 400) {
    const problem: { errors?: Record<string, string[]> } = await response.json().catch(() => ({}))
    const message = Object.values(problem.errors ?? {}).flat()[0]
    if (message) {
      return new Error(message)
    }
  }

  return new Error(`Unable to ${action} (${response.status} ${response.statusText})`)
}

export async function createAccount(usernameValue: string, password: string) {
  const response = await fetch('/api/users', {
    method: 'POST',
    credentials: 'same-origin',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: usernameValue, password }),
  })

  if (!response.ok) {
    throw await accountError(response, 'create your account')
  }

  await login(usernameValue, password)
}

export async function updateAccount(changes: AccountChanges) {
  if (!user.value) {
    throw new Error('Sign in to update your account.')
  }

  const response = await fetch(`/api/users/${user.value.id}`, {
    method: 'PATCH',
    credentials: 'same-origin',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(changes),
  })

  if (response.status === 401) {
    markSignedOut()
    throw new Error('Your session has expired. Please sign in again.')
  }

  if (!response.ok) {
    throw await accountError(response, 'update your account')
  }

  const updatedUser: unknown = await response.json()
  if (!isAuthenticatedUser(updatedUser)) {
    throw new Error('The account response was invalid.')
  }

  user.value = updatedUser
}

export function useAuth() {
  return {
    user: readonly(user),
    username: computed(() => user.value?.name ?? null),
    status: readonly(status),
    isAuthenticated: computed(() => status.value === 'authenticated'),
    ensureAuthenticated,
    login,
    loginUrl,
    logout,
    createAccount,
    updateAccount,
  }
}
