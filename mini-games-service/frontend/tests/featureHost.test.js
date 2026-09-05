import { beforeEach, describe, expect, it } from 'vitest';
import { handleUnauthorized, setFeatureHostContext } from '../src/federation/featureHost.js';

describe('featureHost', () => {
  beforeEach(() => setFeatureHostContext(undefined));

  it('does nothing without a host context', () => {
    expect(() => handleUnauthorized()).not.toThrow();
  });

  it('asks the host to sign out on unauthorized responses', () => {
    let signOutCalls = 0;
    setFeatureHostContext({ signOut: () => { signOutCalls += 1; } });

    handleUnauthorized();

    expect(signOutCalls).toBe(1);
  });

  it('forgets the host context when it is cleared', () => {
    let signOutCalls = 0;
    setFeatureHostContext({ signOut: () => { signOutCalls += 1; } });
    setFeatureHostContext(undefined);

    handleUnauthorized();

    expect(signOutCalls).toBe(0);
  });
});
