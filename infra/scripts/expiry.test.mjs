import test from 'node:test';
import assert from 'node:assert/strict';
import { dispatch } from './expiry.mjs';

test('dispatcher reads once and starts no paid build for absent, down or unexpired state', async () => {
  for (const state of [undefined, { status: 'down' }, { status: 'ready', desiredMode: 'up', expiresAt: 200 }]) {
    let reads = 0;
    await dispatch({ now: () => 100, readControl: async () => { reads++; return state; },
      startCleanup: () => assert.fail('Unexpected build') });
    assert.equal(reads, 1);
  }
});

test('dispatcher starts cleanup only for expiry, failed startup or durable Down', async () => {
  for (const state of [
    { status: 'ready', desiredMode: 'up', expiresAt: 100 },
    { status: 'starting', desiredMode: 'up', expiresAt: 200, startupDeadline: 100 },
    { status: 'stopping', desiredMode: 'down' },
  ]) {
    let starts = 0;
    assert.deepEqual(await dispatch({ now: () => 100, readControl: async () => state,
      startCleanup: async () => { starts++; } }), { started: true });
    assert.equal(starts, 1);
  }
});

test('dispatcher errors propagate to Lambda Errors alarms rather than reporting success', async () => {
  await assert.rejects(dispatch({ readControl: async () => { throw new Error('Read failed'); } }), /Read failed/);
  await assert.rejects(dispatch({ readControl: async () => ({ status: 'stopping', desiredMode: 'down' }),
    startCleanup: async () => { throw new Error('Start failed'); } }), /Start failed/);
});