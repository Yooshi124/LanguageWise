import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, rmSync, writeFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { DemoController, loadFoundation, nativeDependencies } from '../scripts/demo.mjs';
import { SERVICES, IMAGE_KEYS, HOUR, LOCK_WINDOW, beginUp, cleanupDue, canAcquireLock,
  assertPhase, releaseOrder, terraformVariables, validateFoundation, validateRelease } from '../scripts/control.mjs';

const now = Date.parse('2026-10-20T10:00:00Z');
const account = '047754151573';
const region = 'ap-southeast-2';
const role = name => `arn:aws:iam::${account}:role/${name}`;
const foundation = {
  account_id: account, region, cluster_arn: `arn:aws:ecs:${region}:${account}:cluster/languagewise`,
  repository_url: `${account}.dkr.ecr.${region}.amazonaws.com/41026release2/languagewise`,
  control_table_name: 'languagewise-control', artifact_bucket_name: 'languagewise-artifacts',
  cleanup_project_name: 'languagewise-cleanup', cleanup_schedule_name: 'languagewise-cleanup',
  cleanup_schedule_group_name: 'languagewise', cleanup_alarm_name: 'languagewise-cleanup-failure',
  cleanup_role_arn: role('cleanup'), cleanup_dispatcher_role_arn: role('dispatcher'),
  cleanup_dispatcher_arn: `arn:aws:lambda:${region}:${account}:function:languagewise-cleanup-dispatcher`,
  cleanup_dispatcher_alarm_name: 'languagewise-cleanup-dispatcher-failure',
  cleanup_dispatcher_throttle_alarm_name: 'languagewise-cleanup-dispatcher-throttled',
  cleanup_scheduler_alarm_name: 'languagewise-cleanup-scheduler-failure',
  cleanup_failure_topic_arn: `arn:aws:sns:${region}:${account}:languagewise-cleanup-failure`,
  deploy_role_arn: role('deployer'), control_role_arn: role('controller'), release_role_arn: role('releaser'),
  state_bucket_name: 'languagewise-state', demo_state_key: 'demo/terraform.tfstate',
  service_execution_role_arns: Object.fromEntries(IMAGE_KEYS.map(name => [name, role(`${name}-execution`)])),
  service_task_role_arns: Object.fromEntries(IMAGE_KEYS.map(name => [name, role(`${name}-task`)])),
  secret_arns: Object.fromEntries(['db-shared', 'db-mini-games', 'db-quizzes-courses', 'db-chat-discussion', 'db-quests',
    'signing-private-key', 'signing-public-key', 'openrouter-api-key'].map(name =>
    [name, `arn:aws:secretsmanager:${region}:${account}:secret:languagewise/demo/${name}-123456`])),
};
const release = (sequence = 1, character = 'a') => ({ sha: character.repeat(40), sequence,
  images: Object.fromEntries(IMAGE_KEYS.map(name => [name, `${foundation.repository_url}@sha256:${character.repeat(64)}`])) });
const ready = () => ({ ...beginUp(undefined, undefined, release(), now).state, status: 'ready' });
const conflict = () => Object.assign(new Error('Conditional write failed'), { code: 'ConditionalCheckFailedException' });
const alarmSpecifications = [
  [foundation.cleanup_alarm_name, 'AWS/CodeBuild', 'FailedBuilds', 'ProjectName', foundation.cleanup_project_name],
  [foundation.cleanup_dispatcher_alarm_name, 'AWS/Lambda', 'Errors', 'FunctionName', foundation.cleanup_dispatcher_arn.split(':').at(-1)],
  [foundation.cleanup_dispatcher_throttle_alarm_name, 'AWS/Lambda', 'Throttles', 'FunctionName', foundation.cleanup_dispatcher_arn.split(':').at(-1)],
  [foundation.cleanup_scheduler_alarm_name, 'AWS/Scheduler', 'TargetErrorCount', 'ScheduleGroup', foundation.cleanup_schedule_group_name],
];
const buildId = `${foundation.cleanup_project_name}:11111111-1111-1111-1111-111111111111`;
const expiredLock = (kind = 'github') => ({ id: { S: 'lock' }, owner: { S: 'old-owner' },
  startedAt: { N: String(now - LOCK_WINDOW) }, deadline: { N: String(now) },
  stopDeadline: { N: String(now - LOCK_WINDOW + 90 * 60_000) }, kind: { S: kind },
  roleArn: { S: kind === 'github' ? foundation.deploy_role_arn : foundation.cleanup_role_arn },
  ...(kind === 'codebuild' ? { buildId: { S: `${foundation.cleanup_project_name}:old` } } : {}) });

function harness(options = {}) {
  const rows = new Map();
  const objects = new Map();
  const calls = [];
  const reports = [];
  const archive = Buffer.from('offline trusted cleanup bundle');
  const hash = createHash('sha256').update(archive).digest('hex');
  objects.set('cleanup.zip', archive);
  objects.set('releases/latest.json', JSON.stringify(options.latest ?? release()));
  if (options.state) rows.set('control', { id: { S: 'control' }, payload: { S: JSON.stringify(options.state) }, revision: { N: String(options.state.revision) } });
  if (options.lock) rows.set('lock', options.lock);
  let clock = now;
  let applyCount = 0;
  let initialized = false;
  if (options.nativeLock) objects.set(`${foundation.demo_state_key}.tflock`, JSON.stringify(options.nativeLock));
  const outputs = {
    initializer_task_definition_arn: { value: `arn:aws:ecs:${region}:${account}:task-definition/initializer:1` },
    subnet_ids: { value: ['subnet-demo'] }, initializer_security_group_ids: { value: ['sg-demo'] },
    service_names: { value: SERVICES.map(name => `languagewise-demo-${name}`) }, url: { value: 'https://d123example.cloudfront.net' },
  };
  const store = state => rows.set('control', { id: { S: 'control' }, payload: { S: JSON.stringify(state) }, revision: { N: String(state.revision) } });
  const state = () => rows.get('control') ? JSON.parse(rows.get('control').payload.S) : undefined;
  const dependencies = {
    env: options.codebuild ? { CODEBUILD_BUILD_ID: options.buildId ?? buildId } : { GITHUB_RUN_ID: '123',
      AWS_ACCESS_KEY_ID: 'test-only', AWS_SECRET_ACCESS_KEY: 'test-only', AWS_SESSION_TOKEN: 'test-only' },
    now: () => clock, sleep: duration => { clock += duration; },
    report: text => reports.push(text),
    async probe(url, expectedType) {
      calls.push({ probe: url, expectedType });
      if (options.probeFailure && applyCount === 1) throw new Error('Smoke failure');
    },
    run(binary, args, settings) {
      if (binary === 'terraform') {
        const variablesArg = args.find(arg => arg.startsWith('-var-file='));
        calls.push({ binary, args, settings, variables: variablesArg ? JSON.parse(readFileSync(variablesArg.slice(10), 'utf8')) : undefined });
        if (args.includes('apply')) {
          applyCount++;
          if (options.unlockFailure) {
            objects.set(`${foundation.demo_state_key}.tflock`, JSON.stringify({ ID: 'provider-unlock-failure',
              Operation: 'OperationTypeApply', Path: `${foundation.state_bucket_name}/${foundation.demo_state_key}`,
              Created: new Date(clock).toISOString() }));
            throw new Error('Provider failed to unlock state');
          }
          if (options.timeoutApply === applyCount) throw Object.assign(new Error('Timed out'), { uncertainWriter: true });
          if (options.cancelOnApply === applyCount) {
            const current = state();
            store({ ...current, desiredMode: 'down', status: 'stopping', revision: current.revision + 1 });
          }
          if (options.failApply === applyCount) throw new Error('Terraform apply failed');
        }
        return args.includes('output') ? JSON.stringify(outputs) : '';
      }
      assert.equal(binary, 'aws');
      assert.equal(settings.shell, undefined);
      assert.ok(!args.includes('get-secret-value'));
      const [service, operation] = args;
      const inputIndex = args.indexOf('--cli-input-json');
      const input = JSON.parse(args[inputIndex + 1]);
      calls.push({ service, operation, input, args, settings });
      options.beforeAws?.({ service, operation, input, rows, store, state });
      let response = {};
      if (service === 'dynamodb') {
        if (operation === 'get-item') response.Item = rows.get(input.Key.id.S);
        if (operation === 'put-item') {
          const existing = rows.get(input.Item.id.S);
          if (input.ConditionExpression === 'attribute_not_exists(id)' ? existing
            : existing?.owner.S !== input.ExpressionAttributeValues[':owner'].S
              || existing?.deadline.N !== input.ExpressionAttributeValues[':deadline'].N) throw conflict();
          rows.set(input.Item.id.S, input.Item);
        }
        if (operation === 'delete-item') {
          if (rows.get('lock')?.owner.S !== input.ExpressionAttributeValues[':owner'].S) throw conflict();
          if (input.ExpressionAttributeValues[':deadline'] && rows.get('lock')?.deadline.N !== input.ExpressionAttributeValues[':deadline'].N) throw conflict();
          rows.delete(input.Key.id.S);
        }
        if (operation === 'transact-write-items') {
          for (const item of input.TransactItems) {
            if (item.ConditionCheck) {
              const values = item.ConditionCheck.ExpressionAttributeValues;
              if (rows.get('lock')?.owner.S !== values[':owner'].S || Number(rows.get('lock').deadline.N) <= Number(values[':now'].N)) throw conflict();
            }
            if (item.Put?.ConditionExpression) {
              const existing = rows.get(item.Put.Item.id.S);
              if (item.Put.ConditionExpression === 'attribute_not_exists(id)' ? existing : existing?.revision.N !== item.Put.ExpressionAttributeValues[':revision'].N) throw conflict();
            }
          }
          for (const item of input.TransactItems) if (item.Put) rows.set(item.Put.Item.id.S, item.Put.Item);
        }
      } else if (service === 's3api') {
        if (operation === 'get-object') {
          const content = input.Key.startsWith('cleanup/') ? archive : objects.get(input.Key);
          if (content === undefined) throw Object.assign(new Error('Missing object'), { code: 'NoSuchKey' });
          writeFileSync(args[inputIndex + 2], content);
          response.ETag = '"original-etag"';
        }
        if (operation === 'delete-object') {
          if (options.etagChanged || input.IfMatch !== '"original-etag"') throw new Error('Precondition failed');
          objects.delete(input.Key);
        }
        if (operation === 'put-object') {
          if (input.IfNoneMatch && objects.has(input.Key)) throw new Error('Precondition failed');
          objects.set(input.Key, readFileSync(input.Body, 'utf8'));
          if (options.failLatestOnce && input.Key === 'releases/latest.json') {
            options.failLatestOnce = false;
            throw new Error('Publication response lost');
          }
        }
      } else if (service === 'scheduler') response = {
        State: options.scheduleDisabled ? 'DISABLED' : 'ENABLED', ScheduleExpression: 'rate(15 minutes)', FlexibleTimeWindow: { Mode: 'OFF' },
        Target: { Arn: options.wrongTarget ? 'arn:aws:scheduler:::aws-sdk:codebuild:startBuild' : foundation.cleanup_dispatcher_arn,
          RoleArn: role('scheduler'), Input: JSON.stringify({}) },
      };
      else if (service === 'codebuild' && operation === 'batch-get-projects') response.projects = [{
        arn: `arn:aws:codebuild:${region}:${account}:project/${foundation.cleanup_project_name}`, serviceRole: role('cleanup'),
        timeoutInMinutes: 120, concurrentBuildLimit: 1, environment: { privilegedMode: false,
          environmentVariables: [{ name: 'FOUNDATION_CONFIG', type: 'PLAINTEXT', value: JSON.stringify(options.cleanupConfig ?? foundation) }] }, sourceVersion: 'immutable-version',
        source: { type: 'S3', location: `${foundation.artifact_bucket_name}/cleanup/${options.badHash ? '0'.repeat(64) : hash}.zip` },
      }];
      else if (service === 'codebuild' && operation === 'batch-get-builds') response.builds = input.ids.map(id => ({
        id, arn: `arn:aws:codebuild:${region}:${account}:build/${id}`, projectName: foundation.cleanup_project_name,
        serviceRole: foundation.cleanup_role_arn, timeoutInMinutes: 120,
        startTime: new Date(id.endsWith(':old') || options.completedBuildIds?.includes(id) ? now - LOCK_WINDOW - 1000 : now - 1000).toISOString(),
        ...(id.endsWith(':old') || options.completedBuildIds?.includes(id) ? { buildInProgress: Boolean(options.oldBuildRunning), buildStatus: options.oldBuildRunning ? 'IN_PROGRESS' : 'TIMED_OUT',
          endTime: new Date(now - 30 * 60_000).toISOString() } : { buildInProgress: true, buildStatus: 'IN_PROGRESS' }),
      }));
      else if (service === 'lambda') response = {
        FunctionArn: foundation.cleanup_dispatcher_arn, Role: foundation.cleanup_dispatcher_role_arn,
        Runtime: 'nodejs22.x', Handler: 'expiry.handler', State: 'Active', Timeout: 15, MemorySize: 128,
        Environment: { Variables: options.dispatcherEnvironment ?? {
          CONTROL_TABLE_NAME: foundation.control_table_name, CLEANUP_PROJECT_NAME: foundation.cleanup_project_name,
        } },
      };
      else if (service === 'cloudwatch' && operation === 'describe-alarms') {
        const [, Namespace, MetricName, dimension, value] = alarmSpecifications.find(([name]) => name === input.AlarmNames[0]);
        response.MetricAlarms = [{
        ActionsEnabled: !options.alarmDisabled, AlarmActions: [foundation.cleanup_failure_topic_arn], Namespace, MetricName, Statistic: 'Sum',
        Dimensions: [{ Name: dimension, Value: value }],
        Threshold: 1, ComparisonOperator: 'GreaterThanOrEqualToThreshold', Period: 900, EvaluationPeriods: 1,
      }]; }
      else if (service === 'sns') response.Attributes = { TopicArn: foundation.cleanup_failure_topic_arn, Policy: JSON.stringify({ Statement: [{
        Effect: 'Allow', Action: 'sns:Publish', Resource: foundation.cleanup_failure_topic_arn, Principal: { Service: 'cloudwatch.amazonaws.com' },
        Condition: { StringEquals: { 'aws:SourceAccount': account }, ArnEquals: { 'aws:SourceArn': alarmSpecifications.map(([name]) =>
          `arn:aws:cloudwatch:${region}:${account}:alarm:${name}`) } },
      }] }) };
      else if (service === 'iam') response.Role = { Arn: role(input.RoleName), MaxSessionDuration: options.roleSessionDuration ?? 7200,
        AssumeRolePolicyDocument: { Statement: [{
        Effect: 'Allow', Action: 'sts:AssumeRole', Principal: { Service: input.RoleName === 'cleanup' ? 'codebuild.amazonaws.com'
          : input.RoleName === 'dispatcher' ? 'lambda.amazonaws.com' : 'scheduler.amazonaws.com' },
        ...(input.RoleName === 'dispatcher' ? {} : { Condition: { StringEquals: { 'aws:SourceAccount': account }, ArnEquals: { 'aws:SourceArn': input.RoleName === 'cleanup'
          ? `arn:aws:codebuild:${region}:${account}:project/${foundation.cleanup_project_name}`
          : `arn:aws:scheduler:${region}:${account}:schedule-group/${options.wrongSchedulerTrust ? 'other' : foundation.cleanup_schedule_group_name}` } } }),
      }] } };
      else if (service === 'sts') response = { Account: account, Arn: options.identityArn ?? (dependencies.env.CODEBUILD_BUILD_ID
        ? `arn:aws:sts::${account}:assumed-role/cleanup/AWSCodeBuild-${dependencies.env.CODEBUILD_BUILD_ID.split(':').at(-1)}`
        : `arn:aws:sts::${account}:assumed-role/deployer/123`) };
      else if (service === 'secretsmanager') {
        const database = input.SecretId.includes('/db-');
        response = { ARN: input.SecretId, ...(options.deletedDatabase && database ? { DeletedDate: '2026-10-19' } : {}),
          VersionIdsToStages: options.missingSecret || database && (options.dbNeverInitialized || options.firstUp && !initialized)
            ? {} : { version: ['AWSCURRENT'] } };
      }
      else if (service === 'ecs') {
        if (operation === 'run-task') response.tasks = [{ taskArn: 'arn:aws:ecs:task/initializer' }];
        if (operation === 'describe-tasks') {
          initialized = !options.initializerFailure;
          response.tasks = [{ lastStatus: 'STOPPED', containers: [{ exitCode: options.initializerFailure ? 1 : 0 }] }];
        }
        if (operation === 'describe-services') response.services = input.services.map(serviceName => ({ serviceName, status: 'ACTIVE', desiredCount: 1,
          runningCount: 1, pendingCount: 0, deployments: [{ rolloutState: 'COMPLETED' }] }));
      } else if (service === 'resourcegroupstaggingapi') response = {
        ResourceTagMappingList: (options.taggedResources ?? []).map(ResourceARN => ({ ResourceARN })),
        ...(options.tagPagination ? { PaginationToken: 'more' } : {}),
      };
      else if (service === 'rds' && options.residualDatabase) response.DBInstances = [{ DBInstanceIdentifier: 'languagewise-demo-database' }];
      return JSON.stringify(response);
    },
  };
  const controller = new DemoController(foundation, dependencies);
  return { controller, calls, reports, rows, objects, state, store, dependencies,
    advance: duration => { clock += duration; }, dispose: () => rmSync(controller.directory, { recursive: true, force: true }) };
}

test('Down before the first release is idempotent only when disposable state and resources are absent', async () => {
  const fixture = harness();
  fixture.objects.delete('releases/latest.json');
  await fixture.controller.execute('down');
  assert.equal(fixture.state().status, 'down');
  assert.ok(!fixture.calls.some(call => call.binary === 'terraform'));
  assert.ok(!fixture.rows.has('lock'));
});

test('missing release never discards an existing disposable state', async () => {
  const fixture = harness();
  fixture.objects.delete('releases/latest.json');
  fixture.objects.set(foundation.demo_state_key, '{"resources":[{"type":"aws_db_instance"}]}');
  await assert.rejects(fixture.controller.execute('down'), /Existing disposable state/);
  assert.equal(fixture.state().status, 'stopping');
  assert.ok(!fixture.calls.some(call => call.binary === 'terraform'));
});

test('foundation accepts JSON or a file path, but never Terraform output wrappers', () => {
  assert.deepEqual(loadFoundation(JSON.stringify(foundation)), foundation);
  assert.deepEqual(loadFoundation('infra/generated/foundation.json', () => JSON.stringify(foundation)), foundation);
  assert.throws(() => loadFoundation(JSON.stringify({ foundation: { value: foundation } })), /foundation field/);
  assert.throws(() => validateFoundation({ ...foundation, secret_arns: {} }), /metadata/);
  assert.throws(() => validateFoundation({ ...foundation, secret_value: 'not-an-allowed-input' }), /Unexpected foundation/);
  assert.throws(() => validateFoundation({ ...foundation, demo_state_key: 'foundation/terraform.tfstate' }), /disposable/);
});

test('manifest contains exact 18 Compose services plus initializer and immutable repository digests', () => {
  assert.equal(SERVICES.length, 18);
  assert.deepEqual(validateRelease(release(), foundation), release());
  for (const invalid of [
    { ...release(), sha: 'abc' }, { ...release(), sequence: '1' },
    { ...release(), images: { ...release().images, mcp: 'excluded' } },
    { ...release(), images: { ...release().images, 'database-initializer': `${foundation.repository_url}:latest` } },
    { ...release(), images: { ...release().images, 'database-initializer': `other@sha256:${'a'.repeat(64)}` } },
    { ...release(), images: { ...release().images, 'database-initializer': `${foundation.repository_url}@sha256:${'a'.repeat(65)}` } },
  ]) assert.throws(() => validateRelease(invalid, foundation));
});

test('quota atomically reserves four nonrefundable hours and repeated Up preserves deadlines', () => {
  const initial = beginUp(undefined, { cycles: 9, hours: 36 }, release(), now);
  assert.deepEqual(initial.quota, { cycles: 10, hours: 40 });
  assert.equal(initial.state.expiresAt, now + 2 * HOUR);
  assert.equal(initial.state.startupDeadline, now + HOUR);
  const repeated = beginUp(initial.state, initial.quota, release(2, 'b'), now + 1);
  assert.equal(repeated.state, initial.state);
  assert.equal(repeated.repeated, true);
  assert.throws(() => beginUp(undefined, initial.quota, release(), now), /exhausted/);
  assert.throws(() => beginUp({ ...initial.state, desiredMode: 'down' }, undefined, release(), now), /fully removed/);
});

test('cleanup catches partial startup, expiry and explicit Down, not ready unexpired demos', () => {
  assert.equal(cleanupDue(ready(), now), false);
  assert.equal(cleanupDue(ready(), now + 2 * HOUR), true);
  assert.equal(cleanupDue({ ...ready(), status: 'starting' }, now + HOUR), true);
  assert.equal(cleanupDue({ ...ready(), desiredMode: 'down' }, now), true);
  assert.equal(cleanupDue({ desiredMode: 'down', status: 'down' }, now), false);
  assert.throws(() => assertPhase({ ...ready(), desiredMode: 'down' }, now), /cancelled/);
});

test('release sequence is monotonic and SHA archives cannot change digests', () => {
  assert.equal(releaseOrder(release(), undefined), 'new');
  assert.equal(releaseOrder(release(), release()), 'same');
  assert.throws(() => releaseOrder(release(), release(2, 'b')), /Stale/);
  assert.throws(() => releaseOrder(release(1, 'b'), release()), /conflict/);
  assert.throws(() => releaseOrder(release(2), release()), /conflict/);
});

test('Terraform variables expose only foundation, immutable release and boolean services switch', () => {
  assert.deepEqual(terraformVariables(foundation, release(), false), { foundation,
    release: { sha: release().sha, images: release().images }, services_enabled: false });
  assert.throws(() => terraformVariables(foundation, release(), 'false'), /boolean/);
});

test('expired locks are never stolen and competing owner cannot be removed', async () => {
  assert.equal(canAcquireLock(undefined), true);
  assert.equal(canAcquireLock({ deadline: now - 1 }), false);
  const fixture = harness({ lock: { owner: { S: 'other' }, deadline: { N: String(now - LOCK_WINDOW) } } });
  await assert.rejects(fixture.controller.execute('up'), /never steal/);
  assert.equal(fixture.rows.get('lock').owner.S, 'other');
});

test('new Up applies disabled services then initializes once and applies enabled services', async () => {
  const fixture = harness();
  await fixture.controller.execute('up');
  assert.equal(fixture.state().status, 'ready');
  assert.equal(fixture.state().expiresAt, now + 2 * HOUR);
  const plans = fixture.calls.filter(call => call.binary === 'terraform' && call.args.includes('plan'));
  assert.deepEqual(plans.map(call => call.variables.services_enabled), [false, true]);
  assert.equal(fixture.calls.filter(call => call.operation === 'run-task').length, 1);
  assert.equal(fixture.rows.has('lock'), false);
  assert.equal(JSON.parse(fixture.rows.get('quota#2026-10').payload.S).hours, 4);
  assert.equal(fixture.calls.filter(call => call.probe).length, 12);
  for (const call of fixture.calls.filter(call => call.binary === 'terraform' && call.args.includes('init'))) {
    assert.ok(call.args.includes('-backend-config=key=demo/terraform.tfstate'));
    assert.ok(call.args.includes('-backend-config=use_lockfile=true'));
    assert.ok(call.args.includes('-backend-config=workspace_key_prefix=demo/workspaces'));
    assert.equal(call.settings.env.TF_WORKSPACE, 'default');
  }
  assert.ok(fixture.calls.filter(call => call.binary === 'terraform' && call.args.includes('apply')).every(call => call.args.at(-1).endsWith('demo.tfplan')));
  assert.ok(!fixture.calls.some(call => call.args?.some(arg => arg.startsWith('-target'))));
});

test('repeated Up never provisions, initializes, charges or extends the lease', async () => {
  const fixture = harness({ state: ready() });
  await fixture.controller.execute('up');
  assert.deepEqual(fixture.state(), ready());
  assert.ok(!fixture.calls.some(call => call.binary === 'terraform' || call.operation === 'run-task'));
  assert.ok(!fixture.rows.has('quota#2026-10'));
});

for (const [name, settings] of [['disabled scheduler', { scheduleDisabled: true }], ['provenance mismatch', { badHash: true }], ['unpopulated secret', { missingSecret: true }]]) {
  test(`Up fails before reservation or Terraform for ${name}`, async () => {
    const fixture = harness(settings);
    await assert.rejects(fixture.controller.execute('up'));
    assert.ok(!fixture.calls.some(call => call.binary === 'terraform'));
    assert.ok(!fixture.rows.has('quota#2026-10'));
    assert.equal(fixture.rows.has('lock'), false);
  });
}

test('Down arriving during paid apply prevents initializer and triggers exact-plan destroy', async () => {
  const fixture = harness({ cancelOnApply: 1 });
  await assert.rejects(fixture.controller.execute('up'), /cancelled/);
  assert.equal(fixture.state().desiredMode, 'down');
  assert.equal(fixture.state().status, 'down');
  assert.equal(fixture.calls.filter(call => call.operation === 'run-task').length, 0);
  assert.equal(fixture.calls.filter(call => call.binary === 'terraform' && call.args.includes('-destroy')).length, 1);
  assert.equal(JSON.parse(fixture.rows.get('quota#2026-10').payload.S).cycles, 1);
});

test('initializer failure attempts cleanup and never enables services', async () => {
  const fixture = harness({ initializerFailure: true });
  await assert.rejects(fixture.controller.execute('up'), /Initializer failed/);
  assert.equal(fixture.state().status, 'down');
  assert.ok(!fixture.calls.some(call => call.variables?.services_enabled));
});

test('Down intent is durable even when another writer holds the lock', async () => {
  const fixture = harness({ state: ready(), lock: { owner: { S: 'other' }, deadline: { N: String(now + LOCK_WINDOW) } } });
  await assert.rejects(fixture.controller.execute('down'), /occupied/);
  assert.equal(fixture.state().desiredMode, 'down');
  assert.equal(fixture.rows.get('lock').owner.S, 'other');
});

test('Down races the new-Up reservation through revision CAS rather than being overwritten', async () => {
  let cancelled = false;
  const fixture = harness({ beforeAws({ operation, input, store }) {
    if (!cancelled && operation === 'transact-write-items' && input.TransactItems.length === 3) {
      cancelled = true;
      store({ desiredMode: 'down', status: 'stopping', revision: 1 });
    }
  } });
  await assert.rejects(fixture.controller.execute('up'), /Conditional/);
  assert.equal(fixture.state().desiredMode, 'down');
  assert.ok(!fixture.rows.has('quota#2026-10'));
  assert.ok(!fixture.calls.some(call => call.binary === 'terraform'));
});

test('merge while Down publishes immutable archive and latest without provisioning', async () => {
  const fixture = harness({ state: { desiredMode: 'down', status: 'down', revision: 1 } });
  await fixture.controller.execute('release', release(2, 'b'));
  assert.deepEqual(JSON.parse(fixture.objects.get('releases/latest.json')), release(2, 'b'));
  assert.deepEqual(JSON.parse(fixture.objects.get(`releases/${'b'.repeat(40)}.json`)), release(2, 'b'));
  assert.ok(!fixture.calls.some(call => call.binary === 'terraform' || call.operation === 'run-task'));
});

test('stale queued releases cannot overwrite latest', async () => {
  const fixture = harness({ latest: release(3, 'c') });
  await assert.rejects(fixture.controller.execute('release', release(2, 'b')), /Stale/);
  assert.deepEqual(JSON.parse(fixture.objects.get('releases/latest.json')), release(3, 'c'));
  assert.ok(!fixture.calls.some(call => call.operation === 'put-object'));
});

test('release while Up deploys directly with services enabled and never seeds', async () => {
  const fixture = harness({ state: ready() });
  await fixture.controller.execute('release', release(2, 'b'));
  assert.deepEqual(fixture.calls.filter(call => call.variables).map(call => call.variables.services_enabled), [true]);
  assert.ok(!fixture.calls.some(call => call.operation === 'run-task'));
  assert.equal(fixture.state().releaseSha, 'b'.repeat(40));
  assert.equal(fixture.state().expiresAt, now + 2 * HOUR);
});

test('failed functional rollout redeploys previous compatible images without changing latest', async () => {
  const fixture = harness({ state: ready(), probeFailure: true });
  await assert.rejects(fixture.controller.execute('release', release(2, 'b')), /Smoke/);
  assert.deepEqual(fixture.calls.filter(call => call.variables).map(call => call.variables.release.sha), ['b'.repeat(40), 'a'.repeat(40)]);
  assert.deepEqual(JSON.parse(fixture.objects.get('releases/latest.json')), release());
  assert.equal(fixture.state().desiredMode, 'up');
});

test('periodic cleanup is a no-op before expiry and destroys after expiry with AWS-only credentials', async () => {
  const unexpired = harness({ state: ready() });
  await unexpired.controller.execute('cleanup');
  assert.ok(!unexpired.calls.some(call => call.operation === 'put-item' || call.binary === 'terraform'));
  const expired = harness({ state: ready() });
  expired.dependencies.env = { CODEBUILD_BUILD_ID: buildId };
  expired.advance(2 * HOUR);
  expired.controller.workerDeadline = now + 3 * HOUR;
  await expired.controller.execute('cleanup');
  assert.equal(expired.state().status, 'down');
});

test('residual database prevents successful Down and fails the independent build for its alarm', async () => {
  const fixture = harness({ state: { ...ready(), desiredMode: 'down' }, residualDatabase: true });
  await assert.rejects(fixture.controller.execute('cleanup'), /Residual/);
  assert.equal(fixture.state().status, 'stopping');
  assert.ok(!fixture.reports.some(message => message.includes('Down verified')));
});

test('lock loss and worker deadline stop subsequent operations', () => {
  const fixture = harness();
  try {
    fixture.controller.acquire();
    fixture.rows.get('lock').owner.S = 'other';
    assert.throws(() => fixture.controller.assertLock(), /ownership lost/);
    fixture.advance(91 * 60_000);
    assert.throws(() => fixture.controller.get('control'), /Worker deadline/);
  } finally { fixture.dispose(); }
});

test('timed-out Terraform retains its lock rather than permitting an overlapping cleanup writer', async () => {
  const fixture = harness({ timeoutApply: 1 });
  await assert.rejects(fixture.controller.execute('up'), /Timed out/);
  assert.equal(fixture.state().desiredMode, 'down');
  assert.equal(fixture.rows.has('lock'), true);
  assert.equal(fixture.calls.filter(call => call.binary === 'terraform' && call.args.includes('apply')).length, 1);
  assert.ok(fixture.reports.some(message => message.includes('lock retained')));
});

test('publication failure after rollout restores previous control record and S3 latest', async () => {
  const fixture = harness({ state: ready(), failLatestOnce: true });
  await assert.rejects(fixture.controller.execute('release', release(2, 'b')), /Publication/);
  assert.equal(fixture.state().releaseSha, release().sha);
  assert.deepEqual(JSON.parse(fixture.objects.get('releases/latest.json')), release());
  assert.equal(fixture.state().desiredMode, 'up');
});

test('cleanup rechecks eligibility after a revision conflict and leaves a fresh demo alone', async () => {
  let changed = false;
  const fixture = harness({ state: { ...ready(), desiredMode: 'down' }, beforeAws({ operation, input, store }) {
    if (!changed && operation === 'transact-write-items' && input.TransactItems.length === 1) {
      changed = true;
      store({ ...ready(), revision: 10 });
    }
  } });
  await fixture.controller.execute('cleanup');
  assert.equal(fixture.state().desiredMode, 'up');
  assert.equal(fixture.state().revision, 10);
  assert.ok(!fixture.calls.some(call => call.binary === 'terraform'));
});

test('failed initializer is stopped and confirmed terminated before teardown', async () => {
  const fixture = harness({ initializerFailure: true });
  await assert.rejects(fixture.controller.execute('up'), /Initializer/);
  const stopped = fixture.calls.findIndex(call => call.operation === 'stop-task');
  const destroyed = fixture.calls.findIndex(call => call.args?.includes('-destroy'));
  assert.ok(stopped >= 0 && stopped < destroyed);
});

test('preflight uses only secret metadata and bounded structured CLI arguments', async () => {
  const fixture = harness();
  await fixture.controller.execute('up');
  for (const call of fixture.calls.filter(call => call.service)) {
    assert.ok(call.args.includes('--cli-input-json'));
    assert.ok(call.settings.timeout > 0 && call.settings.timeout <= 60_000);
  }
  assert.deepEqual(new Set(fixture.calls.filter(call => call.service === 'secretsmanager').map(call => call.operation)), new Set(['describe-secret']));
  assert.ok(fixture.calls.some(call => call.operation === 'update-item'));
});

test('independent cleanup configured for another state is rejected before provisioning', async () => {
  const fixture = harness({ cleanupConfig: { ...foundation, demo_state_key: 'demo/other.tfstate' } });
  await assert.rejects(fixture.controller.execute('up'), /same nonsecret/);
  assert.ok(!fixture.calls.some(call => call.binary === 'terraform'));
});

test('preflight elapsed time counts against the two-hour presentation lease', async () => {
  const fixture = harness();
  fixture.advance(10 * 60_000);
  await fixture.controller.execute('up');
  assert.equal(fixture.state().requestedAt, now);
  assert.equal(fixture.state().expiresAt, now + 2 * HOUR);
});

test('first Up accepts empty database metadata then requires current versions before enabling services', async () => {
  const fixture = harness({ firstUp: true });
  await fixture.controller.execute('up');
  const seed = fixture.calls.findIndex(call => call.operation === 'run-task');
  const enable = fixture.calls.findIndex(call => call.variables?.services_enabled);
  const metadata = fixture.calls.map((call, index) => ({ ...call, index }))
    .filter(call => call.service === 'secretsmanager');
  assert.equal(metadata.filter(call => call.index < seed).length, 8);
  assert.equal(metadata.filter(call => call.index > seed && call.index < enable).length, 8);
  assert.equal(fixture.state().status, 'ready');
});

test('database versions still absent after initialization prevent service enablement', async () => {
  const fixture = harness({ dbNeverInitialized: true });
  await assert.rejects(fixture.controller.execute('up'), /current version/);
  assert.equal(fixture.calls.filter(call => call.operation === 'run-task').length, 1);
  assert.ok(!fixture.calls.some(call => call.variables?.services_enabled));
  assert.equal(fixture.state().status, 'down');
});

test('deleted database metadata blocks even the first Up before reservation', async () => {
  const fixture = harness({ firstUp: true, deletedDatabase: true });
  await assert.rejects(fixture.controller.execute('up'), /deleted/);
  assert.ok(!fixture.calls.some(call => call.binary === 'terraform'));
  assert.ok(!fixture.rows.has('quota#2026-10'));
});

for (const [label, options] of [
  ['old direct CodeBuild target', { wrongTarget: true }],
  ['dispatcher overrides', { dispatcherEnvironment: { CONTROL_TABLE_NAME: foundation.control_table_name,
    CLEANUP_PROJECT_NAME: foundation.cleanup_project_name, TF_WORKSPACE: 'production' } }],
  ['another dispatcher project', { dispatcherEnvironment: { CONTROL_TABLE_NAME: foundation.control_table_name, CLEANUP_PROJECT_NAME: 'other' } }],
  ['wrong schedule group trust', { wrongSchedulerTrust: true }],
  ['disabled alarm actions', { alarmDisabled: true }],
]) {
  test(`readiness rejects ${label} before reservation`, async () => {
    const fixture = harness(options);
    await assert.rejects(fixture.controller.execute('up'));
    assert.ok(!fixture.calls.some(call => call.binary === 'terraform'));
    assert.ok(!fixture.rows.has('quota#2026-10'));
  });
}

test('production TF_WORKSPACE is rejected and cannot route Terraform into another state', () => {
  const fixture = harness();
  fixture.dependencies.env.TF_WORKSPACE = 'production';
  try { assert.throws(() => new DemoController(foundation, fixture.dependencies), /default Terraform workspace/); }
  finally { fixture.dispose(); }
});

test('routine role credentials longer than two hours cannot acquire a lock', async () => {
  const fixture = harness({ roleSessionDuration: 14400 });
  await assert.rejects(fixture.controller.execute('up'), /7200/);
  assert.ok(!fixture.rows.has('lock'));
});

test('routine workers cannot acquire using a potentially refreshing credential provider', async () => {
  const fixture = harness();
  delete fixture.dependencies.env.AWS_SESSION_TOKEN;
  await assert.rejects(fixture.controller.execute('up'), /static temporary session credentials/);
  assert.ok(!fixture.rows.has('lock'));
});

test('new locks record original identity and fixed stop/deadline timestamps without heartbeat renewal', () => {
  const fixture = harness();
  try {
    fixture.controller.acquire();
    const original = structuredClone(fixture.rows.get('lock'));
    fixture.store(ready());
    fixture.advance(60_000);
    fixture.controller.phase();
    assert.equal(original.kind.S, 'github');
    assert.equal(original.roleArn.S, foundation.deploy_role_arn);
    assert.equal(Number(original.startedAt.N), now);
    assert.equal(Number(original.stopDeadline.N), now + 90 * 60_000);
    assert.equal(Number(original.deadline.N), now + 150 * 60_000);
    assert.equal(fixture.rows.get('lock').deadline.N, original.deadline.N);
    const heartbeat = fixture.calls.find(call => call.operation === 'update-item');
    assert.equal(heartbeat.input.UpdateExpression, 'SET heartbeat = :now');
  } finally { fixture.dispose(); }
});

for (const command of ['up', 'down', 'release']) {
  test(`${command} cannot steal even a fully bounded expired lock`, async () => {
    const fixture = harness({ lock: expiredLock(), state: { ...ready(), desiredMode: 'down' } });
    await assert.rejects(fixture.controller.execute(command, release(2, 'b')), /never steal/);
    assert.equal(fixture.rows.get('lock').owner.S, 'old-owner');
    assert.ok(!fixture.calls.some(call => call.operation === 'delete-item' || call.operation === 'delete-object'));
  });
}

const nativeLock = () => ({ ID: 'terraform-lock-id', Operation: 'OperationTypeApply',
  Path: `${foundation.state_bucket_name}/${foundation.demo_state_key}`,
  Created: new Date(now - LOCK_WINDOW + 30 * 60_000).toISOString() });

for (const kind of ['github', 'codebuild']) {
  test(`verified AWS cleanup recovers expired ${kind} and exact native apply lock before destroy`, async () => {
    const fixture = harness({ codebuild: true, lock: expiredLock(kind), nativeLock: nativeLock(),
      state: { ...ready(), desiredMode: 'down' } });
    await fixture.controller.execute('cleanup');
    assert.equal(fixture.state().status, 'down');
    const deletion = fixture.calls.find(call => call.operation === 'put-item' && call.input.ExpressionAttributeValues?.[':deadline']);
    assert.equal(deletion.input.ConditionExpression, '#owner = :owner AND deadline = :deadline');
    assert.equal(deletion.input.ExpressionAttributeValues[':deadline'].N, String(now));
    const s3Delete = fixture.calls.findIndex(call => call.operation === 'delete-object');
    const destroy = fixture.calls.findIndex(call => call.args?.includes('-destroy'));
    assert.ok(s3Delete >= 0 && s3Delete < destroy);
    assert.deepEqual(fixture.calls[s3Delete].input, { Bucket: foundation.state_bucket_name,
      Key: `${foundation.demo_state_key}.tflock`, IfMatch: '"original-etag"' });
    assert.ok(!fixture.calls.some(call => call.args?.includes('force-unlock')));
    assert.ok(!fixture.calls.some(call => call.variables?.services_enabled));
  });
}

for (const [label, modify, extra] of [
  ['legacy row', lock => { delete lock.startedAt; }],
  ['extended deadline', lock => { lock.deadline.N = String(now + 1); }],
  ['extended stop bound', lock => { lock.stopDeadline.N = String(now); }],
  ['unknown worker kind', lock => { lock.kind.S = 'local'; }],
  ['unapproved routine role', lock => { lock.roleArn.S = role('other'); }],
  ['still-running CodeBuild worker', () => {}, { oldBuildRunning: true, oldKind: 'codebuild' }],
  ['forged cleanup env', () => {}, { identityArn: `arn:aws:sts::${account}:assumed-role/deployer/123` }],
]) {
  test(`cleanup refuses ${label} without deleting either lock`, async () => {
    const lock = expiredLock(extra?.oldKind ?? 'github');
    modify(lock);
    const fixture = harness({ ...extra, codebuild: true, lock, nativeLock: nativeLock(), state: { ...ready(), desiredMode: 'down' } });
    await assert.rejects(fixture.controller.execute('cleanup'));
    assert.equal(fixture.rows.get('lock').owner.S, 'old-owner');
    assert.ok(!fixture.calls.some(call => call.operation === 'delete-item' || call.operation === 'delete-object'));
    assert.ok(!fixture.calls.some(call => call.binary === 'terraform'));
  });
}

test('DynamoDB owner/deadline CAS prevents a changed lock from authorizing native unlock', async () => {
  const fixture = harness({ codebuild: true, lock: expiredLock(), state: { ...ready(), desiredMode: 'down' },
    beforeAws({ operation, input, rows }) {
      if (operation === 'put-item' && input.ExpressionAttributeValues?.[':deadline']) rows.get('lock').deadline.N = String(now + 1);
    } });
  await assert.rejects(fixture.controller.execute('cleanup'), /Conditional/);
  assert.equal(fixture.rows.get('lock').owner.S, 'old-owner');
  assert.ok(!fixture.calls.some(call => call.operation === 'delete-object' || call.binary === 'terraform'));
});

for (const [label, native, extra] of [
  ['different state path', { ...nativeLock(), Path: `${foundation.state_bucket_name}/foundation/terraform.tfstate` }],
  ['unsupported operation', { ...nativeLock(), Operation: 'OperationTypeRefresh' }],
  ['creation before original worker', { ...nativeLock(), Created: new Date(now - LOCK_WINDOW - 1).toISOString() }],
  ['unbounded creation time', { ...nativeLock(), Created: new Date(now).toISOString() }],
  ['invalid creation time', { ...nativeLock(), Created: 'invalid' }],
  ['changed ETag', nativeLock(), { etagChanged: true }],
]) {
  test(`native ${label} fails closed, retains the replacement control lock, and never destroys`, async () => {
    const fixture = harness({ ...extra, codebuild: true, lock: expiredLock(), nativeLock: native,
      state: { ...ready(), desiredMode: 'down' } });
    await assert.rejects(fixture.controller.execute('cleanup'));
    assert.ok(fixture.rows.has('lock'));
    assert.ok(fixture.objects.has(`${foundation.demo_state_key}.tflock`));
    assert.ok(!fixture.calls.some(call => call.binary === 'terraform'));
    if (!extra?.etagChanged) assert.ok(!fixture.calls.some(call => call.operation === 'delete-object'));
  });
}

test('cleanup leaves a fresh unexpired demo and its expired lock untouched', async () => {
  const fixture = harness({ codebuild: true, lock: expiredLock(), state: ready() });
  await fixture.controller.execute('cleanup');
  assert.equal(fixture.rows.get('lock').owner.S, 'old-owner');
  assert.ok(!fixture.calls.some(call => call.operation === 'delete-item' || call.operation === 'delete-object'));
});

test('bounded recovery also succeeds when the failed worker left no native lock', async () => {
  const fixture = harness({ codebuild: true, lock: expiredLock(), state: { ...ready(), desiredMode: 'down' } });
  await fixture.controller.execute('cleanup');
  assert.equal(fixture.state().status, 'down');
  assert.ok(!fixture.calls.some(call => call.operation === 'delete-object'));
});

test('non-timeout provider unlock failure retains evidence and later AWS cleanup automatically destroys', async () => {
  const failed = harness({ unlockFailure: true });
  await assert.rejects(failed.controller.execute('up'), /failed to unlock/);
  assert.equal(failed.state().desiredMode, 'down');
  assert.ok(failed.rows.has('lock'));
  const original = structuredClone(failed.rows.get('lock'));
  assert.equal(Number(original.startedAt.N), now);
  const retry = harness({ codebuild: true, lock: original, nativeLock: JSON.parse(failed.objects.get(`${foundation.demo_state_key}.tflock`)),
    state: failed.state() });
  retry.advance(LOCK_WINDOW);
  retry.controller.workerDeadline += LOCK_WINDOW;
  await retry.controller.execute('cleanup');
  assert.equal(retry.state().status, 'down');
  assert.ok(retry.calls.some(call => call.operation === 'delete-object'));
  assert.ok(!retry.rows.has('lock'));
});

test('recovery failure preserves original evidence across multiple fresh bounded cleanup leases', async () => {
  let lock = expiredLock();
  const original = structuredClone(lock);
  const completedBuildIds = [];
  for (let attempt = 0; attempt < 2; attempt++) {
    const retryId = `${foundation.cleanup_project_name}:retry-${attempt}`;
    const fixture = harness({ codebuild: true, lock, nativeLock: nativeLock(), etagChanged: true,
      completedBuildIds, buildId: retryId, state: { ...ready(), desiredMode: 'down' } });
    fixture.advance(attempt * LOCK_WINDOW);
    fixture.controller.workerDeadline += attempt * LOCK_WINDOW;
    await assert.rejects(fixture.controller.execute('cleanup'), /Precondition/);
    lock = structuredClone(fixture.rows.get('lock'));
    assert.deepEqual(lock.recovery.M, original);
    assert.equal(Number(lock.deadline.N), now + (attempt + 1) * LOCK_WINDOW);
    assert.ok(!fixture.calls.some(call => call.binary === 'terraform'));
    completedBuildIds.push(retryId);
  }
  const recovered = harness({ codebuild: true, lock, nativeLock: { ...nativeLock(), Operation: 'OperationTypePlan' },
    completedBuildIds, buildId: `${foundation.cleanup_project_name}:final`, state: { ...ready(), desiredMode: 'down' } });
  recovered.advance(2 * LOCK_WINDOW);
  recovered.controller.workerDeadline += 2 * LOCK_WINDOW;
  await recovered.controller.execute('cleanup');
  assert.equal(recovered.state().status, 'down');
});

test('recovery cannot delete the current worker native lock after Terraform was used', () => {
  const fixture = harness({ nativeLock: nativeLock(), state: { ...ready(), desiredMode: 'down' } });
  try {
    fixture.controller.acquire();
    fixture.controller.terraformUsed = true;
    assert.throws(() => fixture.controller.recoverNativeLock({ startedAt: now - LOCK_WINDOW, stopDeadline: now }), /after using Terraform/);
    assert.ok(!fixture.calls.some(call => call.operation === 'delete-object'));
  } finally { fixture.dispose(); }
});

test('smoke paths and federation entries match actual navigation and shared nginx locations', async () => {
  const fixture = harness();
  await fixture.controller.execute('up');
  const navigation = readFileSync(new URL('../../shared/frontend/src/config/navigation.ts', import.meta.url), 'utf8');
  const nginx = readFileSync(new URL('../../shared/frontend/nginx.conf', import.meta.url), 'utf8');
  const probes = fixture.calls.filter(call => call.probe);
  assert.deepEqual(probes.filter(call => call.expectedType === 'html').map(call => new URL(call.probe).pathname).toSorted(),
    [...navigation.matchAll(/href: '([^']+)'/g)].map(match => match[1]).toSorted());
  const entries = [...navigation.matchAll(/remoteEntryPath: '([^']+)'/g)].map(match => match[1]);
  assert.equal(entries.length, 5);
  assert.deepEqual(probes.filter(call => call.expectedType === 'javascript').map(call => new URL(call.probe).pathname).toSorted(), entries.toSorted());
  for (const path of entries) assert.ok(nginx.includes(`location = ${path} {`));
});

for (const [label, type, body, expectedType, succeeds] of [
  ['module JavaScript', 'application/javascript; charset=utf-8', 'export const get = () => {};', 'javascript', true],
  ['text JavaScript', 'text/javascript', 'var remote = function() {};', 'javascript', true],
  ['HTML fallback', 'text/html', '<!doctype html><html></html>', 'javascript', false],
  ['mislabelled HTML fallback', 'application/javascript', '<html><body>const app</body></html>', 'javascript', false],
  ['empty entry', 'application/javascript', '', 'javascript', false],
  ['non-script entry', 'application/javascript', 'not found', 'javascript', false],
  ['oversized entry', 'application/javascript', `const entry = "${'a'.repeat(1024 * 1024)}";`, 'javascript', false],
  ['health JSON', 'application/json', '{"status":"healthy"}', 'json', true],
  ['invalid health JSON', 'application/json', 'not json', 'json', false],
  ['HTML page', 'text/html', '<!doctype html><html></html>', 'html', true],
]) {
  test(`native smoke probe validates ${label} without network access`, async () => {
    const dependencies = nativeDependencies({}, async (url, options) => {
      assert.equal(options.redirect, 'error');
      assert.ok(options.signal);
      return new Response(body, { headers: { 'Content-Type': type } });
    });
    if (succeeds) await dependencies.probe('https://example.invalid/', expectedType);
    else await assert.rejects(dependencies.probe('https://example.invalid/', expectedType));
  });
}

test('residual inventory permits only own demo task-definition metadata and still rejects paid resources', async () => {
  const definition = `arn:aws:ecs:${region}:${account}:task-definition/languagewise-demo-shared-backend:12`;
  const retained = harness({ state: ready(), taggedResources: [definition] });
  await retained.controller.execute('down');
  assert.equal(retained.state().status, 'down');
  assert.ok(retained.reports.some(report => report.includes('non-billed demo task-definition')));
  for (const options of [
    { taggedResources: [definition, `arn:aws:rds:${region}:${account}:db:languagewise-demo-postgres`] },
    { taggedResources: [definition.replace(account, '000000000000')] },
    { taggedResources: [definition.replace('languagewise-demo-', 'another-app-')] },
    { taggedResources: [definition], tagPagination: true },
  ]) {
    const fixture = harness({ state: ready(), ...options });
    await assert.rejects(fixture.controller.execute('down'), /Tagged runtime/);
    assert.equal(fixture.state().status, 'stopping');
  }
});