// Authentication against the shared backend, which owns the login session
// cookie. Through the gateway this app is same-origin with it; in standalone
// dev it lives on port 3000 and the check may fail, which simply renders the
// sidebar's signed-out state.
import { computed, readonly, ref } from 'vue';

const sharedFrontend = `${window.location.protocol}//${window.location.hostname}:3000`;
const sharedApiBase = window.location.port === '3000' ? '' : sharedFrontend;

const user = ref(null);
const status = ref('loading');
let authRequest;

function currentReturnUrl() {
	return `${window.location.pathname}${window.location.search}${window.location.hash}`;
}

export function loginUrl() {
	const url = new URL('/login.html', sharedFrontend);
	url.searchParams.set('returnUrl', currentReturnUrl());
	return url.toString();
}

function markSignedOut() {
	user.value = null;
	status.value = 'signed-out';
}

export async function ensureAuthenticated() {
	if (status.value === 'authenticated') {
		return true;
	}

	if (authRequest) {
		return authRequest;
	}

	status.value = 'loading';
	authRequest = (async () => {
		let response;

		try {
			response = await fetch(`${sharedApiBase}/api/check-login`, {
				method: 'POST',
				credentials: 'include',
				headers: { Accept: 'application/json' },
			});
		} catch (error) {
			status.value = 'error';
			throw error;
		}

		if (response.status === 401) {
			markSignedOut();
			return false;
		}

		if (!response.ok) {
			status.value = 'error';
			throw new Error(`Unable to verify login (${response.status} ${response.statusText})`);
		}

		const authenticatedUser = await response.json();
		user.value = { username: authenticatedUser.name };
		status.value = 'authenticated';
		return true;
	})().finally(() => {
		authRequest = undefined;
	});

	return authRequest;
}

export async function logout() {
	const response = await fetch(`${sharedApiBase}/api/logout`, {
		method: 'POST',
		credentials: 'include',
	});

	if (!response.ok) {
		throw new Error(`Unable to log out (${response.status} ${response.statusText})`);
	}

	markSignedOut();
}

export function useAuth() {
	return {
		user: readonly(user),
		status: readonly(status),
		isAuthenticated: computed(() => status.value === 'authenticated'),
		ensureAuthenticated,
		loginUrl,
		logout,
	};
}
