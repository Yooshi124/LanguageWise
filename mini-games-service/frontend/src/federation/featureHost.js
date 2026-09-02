let hostContext;

export function setFeatureHostContext(context) {
	hostContext = context;
}

export function handleUnauthorized() {
	if (hostContext) {
		void hostContext.signOut();
	}
}