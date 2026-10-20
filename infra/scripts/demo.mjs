import { execFileSync } from 'node:child_process';
import { createHash, randomUUID } from 'node:crypto';
import { appendFileSync, chmodSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { isDeepStrictEqual } from 'node:util';
import { SERVICES, LOCK_WINDOW, WORKER_WINDOW, requireThat, validateFoundation, validateRelease,
  releaseOrder, cleanupDue, beginUp, assertPhase, terraformVariables } from './control.mjs';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const sleep = milliseconds => Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, milliseconds);
const encode = value => ({ S: JSON.stringify(value) });
const decode = item => item?.payload ? JSON.parse(item.payload.S) : undefined;
const key = id => ({ id: { S: id } });
const conditional = error => error?.code === 'ConditionalCheckFailedException' || error?.code === 'TransactionCanceledException';

export function loadFoundation(input, read = readFileSync) {
  requireThat(typeof input === 'string' && input.trim().length > 0, 'FOUNDATION_CONFIG is required');
  const source = input.trim();
  const json = source.startsWith('{') ? source : read(resolve(source), 'utf8');
  return validateFoundation(JSON.parse(json.trim()));
}

export function nativeDependencies(env = process.env, request = fetch) {
  return {
    env, now: Date.now, sleep,
    run(binary, args, options = {}) {
      try {
        return execFileSync(binary, args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'],
          maxBuffer: 16 * 1024 * 1024, timeout: 60_000, killSignal: 'SIGKILL', shell: false,
          env: { ...env, AWS_PAGER: '', AWS_CLI_AUTO_PROMPT: 'off', TF_IN_AUTOMATION: 'true', TF_INPUT: 'false' }, ...options });
      } catch (error) {
        const safe = new Error(`${binary === 'aws' ? 'AWS operation' : 'Terraform operation'} failed; command output suppressed`);
        const code = String(error.stderr ?? '').match(/\b(ConditionalCheckFailedException|TransactionCanceledException|NoSuchKey|NoSuchEntity|ResourceNotFoundException)\b/);
        safe.code = code?.[1];
        safe.uncertainWriter = error.code === 'ETIMEDOUT' || Boolean(error.signal);
        throw safe;
      }
    },
    async probe(url, expectedType) {
      const response = await request(url, { signal: AbortSignal.timeout(20_000), redirect: 'error' });
      const reader = response.body?.getReader();
      try {
        requireThat(response.ok && reader, 'HTTPS smoke check failed');
        const type = response.headers.get('content-type')?.split(';')[0].trim().toLowerCase();
        const types = { json: ['application/json'], html: ['text/html'], javascript: ['application/javascript', 'text/javascript'] };
        requireThat(types[expectedType]?.includes(type), 'Unexpected smoke response content type');
        const chunks = [];
        let length = 0;
        while (true) {
          const { done, value } = await reader.read();
          if (done) break;
          length += value.byteLength;
          requireThat(length <= 1024 * 1024, 'Smoke response exceeds 1 MiB');
          chunks.push(value);
        }
        const body = Buffer.concat(chunks).toString('utf8');
        requireThat(body.trim().length > 0, 'Empty smoke response');
        if (expectedType === 'json') JSON.parse(body);
        if (expectedType === 'html') requireThat(/<(?:!doctype\s+html|html)\b/i.test(body), 'Expected an HTML document');
        if (expectedType === 'javascript') {
          requireThat(!/<(?:!doctype\s+html|html|head|body)\b/i.test(body)
            && /\b(?:export|import|function|const|var|let)\b/.test(body), 'Expected JavaScript, not an SPA fallback');
        }
      } finally { await reader?.cancel(); }
    },
    report(text) {
      if (env.GITHUB_STEP_SUMMARY) appendFileSync(env.GITHUB_STEP_SUMMARY, `${text}\n`);
      console.log(text);
    },
  };
}

export class DemoController {
  constructor(foundation, dependencies = nativeDependencies()) {
    this.config = validateFoundation(foundation);
    this.dependencies = dependencies;
    this.owner = `${dependencies.env.CODEBUILD_BUILD_ID ?? dependencies.env.GITHUB_RUN_ID ?? 'local'}:${randomUUID()}`;
    requireThat(!dependencies.env.TF_WORKSPACE || dependencies.env.TF_WORKSPACE === 'default', 'Only the default Terraform workspace is allowed');
    this.started = dependencies.now();
    this.workerDeadline = this.started + WORKER_WINDOW;
    this.directory = mkdtempSync(join(tmpdir(), 'languagewise-demo-'));
    chmodSync(this.directory, 0o700);
    this.locked = false;
  }

  aws(service, operation, input = {}, timeout = 60_000, extra = []) {
    requireThat(this.dependencies.now() < this.workerDeadline, 'Worker deadline reached');
    try {
      const output = this.dependencies.run('aws', [service, operation, '--region', this.config.region,
        '--output', 'json', '--no-cli-pager', '--cli-input-json', JSON.stringify(input), ...extra],
      { timeout: Math.max(1, Math.min(timeout, this.workerDeadline - this.dependencies.now())) });
      return output?.trim() ? JSON.parse(output) : {};
    } catch (error) {
      if (error.uncertainWriter && !/^(get-|describe-|list-|batch-get-)/.test(operation)) this.uncertainWriter = true;
      throw error;
    }
  }

  get(id) {
    return this.aws('dynamodb', 'get-item', { TableName: this.config.control_table_name, Key: key(id), ConsistentRead: true }).Item;
  }

  state() { return decode(this.get('control')); }

  workerIdentity() {
    const identity = this.aws('sts', 'get-caller-identity');
    requireThat(identity.Account === this.config.account_id, 'Unexpected AWS account');
    const buildId = this.dependencies.env.CODEBUILD_BUILD_ID;
    if (buildId) {
      const expectedRole = this.config.cleanup_role_arn;
      const session = `arn:aws:sts::${this.config.account_id}:assumed-role/${expectedRole.split('/').at(-1)}/`;
      requireThat(identity.Arn?.startsWith(`${session}AWSCodeBuild-`), 'Cleanup requires the exact independent CodeBuild role');
      const build = this.aws('codebuild', 'batch-get-builds', { ids: [buildId] }).builds?.[0];
      requireThat(build?.id === buildId && build.projectName === this.config.cleanup_project_name
        && build.arn === `arn:aws:codebuild:${this.config.region}:${this.config.account_id}:build/${buildId}`
        && build.serviceRole === expectedRole && build.buildInProgress === true && build.buildStatus === 'IN_PROGRESS'
        && identity.Arn === `${session}AWSCodeBuild-${buildId.split(':').at(-1)}`
        && build.timeoutInMinutes > 0 && build.timeoutInMinutes <= 120
        && Number.isFinite(Date.parse(build.startTime)) && Date.parse(build.startTime) <= this.started,
      'Cleanup build identity or termination bound is invalid');
      this.verifiedBuild = build;
      return { kind: 'codebuild', buildId, roleArn: expectedRole };
    }
    const roleArn = [this.config.deploy_role_arn, this.config.control_role_arn, this.config.release_role_arn]
      .filter(Boolean).find(arn => identity.Arn?.startsWith(`arn:aws:sts::${this.config.account_id}:assumed-role/${arn.split('/').at(-1)}/`));
    requireThat(roleArn, 'Controller requires a bounded routine assumed role');
    requireThat(['AWS_ACCESS_KEY_ID', 'AWS_SECRET_ACCESS_KEY', 'AWS_SESSION_TOKEN'].every(name => this.dependencies.env[name]),
      'Routine controllers require static temporary session credentials, not a refreshing credential provider');
    const role = this.aws('iam', 'get-role', { RoleName: roleArn.split('/').at(-1) }).Role;
    requireThat(role?.Arn === roleArn && role.MaxSessionDuration <= 7200, 'Routine role session bound must not exceed 7200 seconds');
    return { kind: 'github', roleArn };
  }

  expiredLock(existing) {
    const startedAt = Number(existing?.startedAt?.N);
    const deadline = Number(existing?.deadline?.N);
    const stopDeadline = Number(existing?.stopDeadline?.N);
    const kind = existing?.kind?.S;
    requireThat(existing?.owner?.S && Number.isSafeInteger(startedAt) && Number.isSafeInteger(deadline)
      && deadline === startedAt + LOCK_WINDOW && stopDeadline === startedAt + WORKER_WINDOW
      && this.dependencies.now() >= deadline && ['github', 'codebuild'].includes(kind),
    'Legacy, unbounded or unexpired lock requires manual recovery');
    if (kind === 'codebuild') {
      const buildId = existing.buildId?.S;
      requireThat(buildId?.startsWith(`${this.config.cleanup_project_name}:`), 'Expired build owner is invalid');
      const build = this.aws('codebuild', 'batch-get-builds', { ids: [buildId] }).builds?.[0];
      requireThat(build?.id === buildId && build.serviceRole === this.config.cleanup_role_arn
        && build.arn === `arn:aws:codebuild:${this.config.region}:${this.config.account_id}:build/${buildId}`
        && existing.roleArn?.S === this.config.cleanup_role_arn && build.projectName === this.config.cleanup_project_name
        && build.buildInProgress === false && ['SUCCEEDED', 'FAILED', 'FAULT', 'STOPPED', 'TIMED_OUT'].includes(build.buildStatus)
        && Number.isFinite(Date.parse(build.startTime)) && Date.parse(build.startTime) <= startedAt
        && Number.isFinite(Date.parse(build.endTime)) && Date.parse(build.endTime) <= this.dependencies.now()
        && build.timeoutInMinutes > 0 && build.timeoutInMinutes <= 120,
      'Expired CodeBuild worker is not provably terminated');
    } else {
      const roleArn = existing.roleArn?.S;
      requireThat([this.config.deploy_role_arn, this.config.control_role_arn, this.config.release_role_arn].filter(Boolean).includes(roleArn), 'Expired routine role is invalid');
      const role = this.aws('iam', 'get-role', { RoleName: roleArn.split('/').at(-1) }).Role;
      requireThat(role?.Arn === roleArn && role.MaxSessionDuration <= 7200, 'Expired routine credentials are not bounded');
    }
    return { owner: existing.owner.S, startedAt, deadline, stopDeadline };
  }

  recoverNativeLock(previous) {
    requireThat(!this.terraformUsed, 'Cannot recover a native lock after using Terraform');
    this.assertLock();
    const path = join(this.directory, 'orphan.tflock');
    const input = { Bucket: this.config.state_bucket_name, Key: `${this.config.demo_state_key}.tflock` };
    let result;
    try { result = this.aws('s3api', 'get-object', input, 60_000, [path]); }
    catch (error) { if (error.code === 'NoSuchKey') return; throw error; }
    const lock = JSON.parse(readFileSync(path, 'utf8'));
    const created = Date.parse(lock.Created);
    requireThat(result.ETag && lock.ID && ['OperationTypeApply', 'OperationTypePlan'].includes(lock.Operation)
      && lock.Path === `${this.config.state_bucket_name}/${this.config.demo_state_key}`
      && Number.isFinite(created) && created >= previous.startedAt && created <= previous.stopDeadline,
    'Native lock is not a proven expired apply/plan for this exact state; manual recovery required');
    this.assertLock();
    requireThat(this.state()?.desiredMode === 'down', 'Recovery requires durable Down intent');
    this.aws('s3api', 'delete-object', { ...input, IfMatch: result.ETag });
  }

  acquire(recover = false) {
    const worker = this.workerIdentity();
    this.lockDeadline = this.dependencies.now() + LOCK_WINDOW;
    const startedAt = this.lockDeadline - LOCK_WINDOW;
    const item = { ...key('lock'), owner: { S: this.owner }, deadline: { N: String(this.lockDeadline) },
      startedAt: { N: String(startedAt) }, stopDeadline: { N: String(startedAt + WORKER_WINDOW) },
      kind: { S: worker.kind }, roleArn: { S: worker.roleArn }, ...(worker.buildId ? { buildId: { S: worker.buildId } } : {}) };
    const put = () => {
      this.aws('dynamodb', 'put-item', { TableName: this.config.control_table_name,
        Item: item, ConditionExpression: 'attribute_not_exists(id)' });
      this.locked = true;
    };
    try {
      put();
    } catch (error) {
      if (!conditional(error)) throw error;
      requireThat(recover && worker.kind === 'codebuild', 'Control lock occupied; never steal an expired lock outside independent cleanup');
      requireThat(this.state()?.desiredMode === 'down', 'Recovery requires durable Down intent');
      const existing = this.get('lock');
      const previous = this.expiredLock(existing);
      const evidence = existing.recovery?.M ?? existing;
      const nativeOwner = this.expiredLock(evidence);
      this.aws('dynamodb', 'put-item', { TableName: this.config.control_table_name,
        Item: { ...item, recovery: { M: evidence } },
        ConditionExpression: '#owner = :owner AND deadline = :deadline', ExpressionAttributeNames: { '#owner': 'owner' },
        ExpressionAttributeValues: { ':owner': { S: previous.owner }, ':deadline': { N: String(previous.deadline) } } });
      this.locked = true;
      try {
        this.recoverNativeLock(nativeOwner);
        this.aws('dynamodb', 'put-item', { TableName: this.config.control_table_name, Item: item,
          ConditionExpression: '#owner = :owner AND deadline = :deadline', ExpressionAttributeNames: { '#owner': 'owner' },
          ExpressionAttributeValues: { ':owner': { S: this.owner }, ':deadline': { N: String(this.lockDeadline) } } });
      }
      catch (failure) { this.uncertainWriter = true; throw failure; }
    }
  }

  assertLock() {
    requireThat(!this.uncertainWriter, 'Writer termination is uncertain; retain lock for operator recovery');
    requireThat(this.locked && this.dependencies.now() < this.lockDeadline, 'Control lock deadline reached');
    requireThat(this.get('lock')?.owner?.S === this.owner, 'Control lock ownership lost');
  }

  lockCondition() {
    return { ConditionCheck: { TableName: this.config.control_table_name, Key: key('lock'),
      ConditionExpression: '#owner = :owner AND deadline > :now', ExpressionAttributeNames: { '#owner': 'owner' },
      ExpressionAttributeValues: { ':owner': { S: this.owner }, ':now': { N: String(this.dependencies.now()) } } } };
  }

  statePut(state, previous) {
    return { Put: { TableName: this.config.control_table_name,
      Item: { ...key('control'), payload: encode(state), revision: { N: String(state.revision) } },
      ConditionExpression: previous ? 'revision = :revision' : 'attribute_not_exists(id)',
      ...(previous ? { ExpressionAttributeValues: { ':revision': { N: String(previous.revision) } } } : {}) } };
  }

  save(state, previous) {
    this.assertLock();
    this.aws('dynamodb', 'transact-write-items', { TransactItems: [this.lockCondition(), this.statePut(state, previous)] });
  }

  markDown(onlyIfDue = false) {
    for (let attempt = 0; attempt < 8; attempt++) {
      const previous = this.state();
      if (onlyIfDue && !cleanupDue(previous, this.dependencies.now())) return undefined;
      const state = { ...previous, desiredMode: 'down', status: previous?.status === 'down' ? 'down' : 'stopping',
        revision: (previous?.revision ?? 0) + 1 };
      try {
        this.aws('dynamodb', 'transact-write-items', { TransactItems: [this.statePut(state, previous)] });
        return state;
      } catch (error) { if (!conditional(error)) throw error; }
    }
    throw new Error('Concurrent control changes prevented Down; retry required');
  }

  phase() {
    this.assertLock();
    const state = this.state();
    assertPhase(state, this.dependencies.now());
    this.aws('dynamodb', 'update-item', { TableName: this.config.control_table_name, Key: key('lock'),
      UpdateExpression: 'SET heartbeat = :now', ConditionExpression: '#owner = :owner',
      ExpressionAttributeNames: { '#owner': 'owner' }, ExpressionAttributeValues: {
        ':owner': { S: this.owner }, ':now': { N: String(this.dependencies.now()) },
      } });
    return state;
  }

  releaseFile(name, release) {
    const path = join(this.directory, name);
    writeFileSync(path, JSON.stringify(release), { mode: 0o600 });
    return path;
  }

  readRelease() {
    const path = join(this.directory, 'latest.json');
    try {
      this.aws('s3api', 'get-object', { Bucket: this.config.artifact_bucket_name, Key: 'releases/latest.json' }, 60_000, [path]);
      return validateRelease(JSON.parse(readFileSync(path, 'utf8')), this.config);
    } catch (error) { if (error.code === 'NoSuchKey') return undefined; throw error; }
  }

  publish(release) {
    this.assertLock();
    const path = this.releaseFile('release.json', release);
    const input = { Bucket: this.config.artifact_bucket_name, ContentType: 'application/json', Body: path };
    try {
      this.aws('s3api', 'put-object', { ...input, Key: `releases/${release.sha}.json`, IfNoneMatch: '*' });
    } catch (error) {
      const archive = join(this.directory, 'archive.json');
      this.aws('s3api', 'get-object', { Bucket: input.Bucket, Key: `releases/${release.sha}.json` }, 60_000, [archive]);
      requireThat(releaseOrder(release, validateRelease(JSON.parse(readFileSync(archive, 'utf8')), this.config)) === 'same', 'Immutable archive conflict');
    }
    const previous = this.readRelease();
    releaseOrder(release, previous);
    this.writeLatest(release);
  }

  writeLatest(release) {
    this.assertLock();
    this.aws('s3api', 'put-object', { Bucket: this.config.artifact_bucket_name, Key: 'releases/latest.json',
      ContentType: 'application/json', Body: this.releaseFile('latest-write.json', release) });
  }

  verifyRole(arn, principal, sourceArn) {
    requireThat(arn?.startsWith(`arn:aws:iam::${this.config.account_id}:role/`), 'Cleanup role must belong to this account');
    const role = this.aws('iam', 'get-role', { RoleName: arn.split('/').at(-1) }).Role;
    const statements = role?.AssumeRolePolicyDocument?.Statement;
    requireThat(role?.Arn === arn && Array.isArray(statements) && statements.length > 0
      && statements.every(statement => statement.Effect === 'Allow' && statement.Action === 'sts:AssumeRole'
        && isDeepStrictEqual(statement.Principal, { Service: principal })
        && !statement.NotAction && !statement.NotPrincipal
        && (sourceArn ? isDeepStrictEqual(statement.Condition, {
          StringEquals: { 'aws:SourceAccount': this.config.account_id }, ArnEquals: { 'aws:SourceArn': sourceArn },
        }) : !statement.Condition)), 'Cleanup role must trust only its source-constrained AWS service');
  }

  verifyCleanup() {
    const config = this.config;
    const schedule = this.aws('scheduler', 'get-schedule', { Name: config.cleanup_schedule_name, GroupName: config.cleanup_schedule_group_name });
    const project = this.aws('codebuild', 'batch-get-projects', { names: [config.cleanup_project_name] }).projects?.[0];
    const dispatcher = this.aws('lambda', 'get-function-configuration', { FunctionName: config.cleanup_dispatcher_arn });
    requireThat(schedule.State === 'ENABLED' && schedule.ScheduleExpression === 'rate(15 minutes)'
      && schedule.FlexibleTimeWindow?.Mode === 'OFF' && !schedule.EndDate, 'Independent cleanup must run every 15 minutes without an end date');
    requireThat(project?.arn === `arn:aws:codebuild:${config.region}:${config.account_id}:project/${config.cleanup_project_name}`
      && project.serviceRole === config.cleanup_role_arn && schedule.Target?.Arn === config.cleanup_dispatcher_arn
      && isDeepStrictEqual(JSON.parse(schedule.Target.Input), {}), 'Cleanup schedule target must be the pinned dispatcher without overrides');
    requireThat(dispatcher.FunctionArn === config.cleanup_dispatcher_arn && dispatcher.Role === config.cleanup_dispatcher_role_arn
      && dispatcher.Runtime === 'nodejs22.x' && dispatcher.Handler === 'expiry.handler' && dispatcher.State === 'Active'
      && dispatcher.Timeout > 0 && dispatcher.Timeout <= 30 && dispatcher.MemorySize <= 128
      && isDeepStrictEqual(dispatcher.Environment?.Variables, {
        CONTROL_TABLE_NAME: config.control_table_name, CLEANUP_PROJECT_NAME: config.cleanup_project_name,
      }), 'Dispatcher must use only the configured control table and cleanup project');
    requireThat(project.timeoutInMinutes <= 120 && project.timeoutInMinutes >= 90 && project.concurrentBuildLimit === 1
      && project.environment?.privilegedMode === false && project.source?.type === 'S3'
      && (!project.source.buildspec || project.source.buildspec.includes('node infra/scripts/demo.mjs cleanup'))
      && project.sourceVersion, 'Cleanup build requires bounded serialized runtime and version-pinned S3 source');
    const environment = project.environment.environmentVariables ?? [];
    const cleanupConfig = environment.find(variable => variable.name === 'FOUNDATION_CONFIG');
    requireThat(cleanupConfig?.type === 'PLAINTEXT' && cleanupConfig.value.trim().startsWith('{')
      && isDeepStrictEqual(validateFoundation(JSON.parse(cleanupConfig.value)), config), 'Independent cleanup must use the same nonsecret foundation configuration');
    requireThat(!environment.some(variable => /^(GITHUB_TOKEN|GH_TOKEN|AWS_ACCESS_KEY_ID|AWS_SECRET_ACCESS_KEY|AWS_SESSION_TOKEN)$/.test(variable.name)), 'Cleanup must use its own service credentials');
    const location = project.source.location;
    const match = location?.match(/^([^/]+)\/(.+?([a-f0-9]{64})[^/]*)$/);
    requireThat(match && match[1] === config.artifact_bucket_name, 'Cleanup source must have a content-addressed key in the artifact bucket');
    const archive = join(this.directory, 'cleanup.zip');
    this.aws('s3api', 'get-object', { Bucket: match[1], Key: match[2], VersionId: project.sourceVersion }, 60_000, [archive]);
    requireThat(createHash('sha256').update(readFileSync(archive)).digest('hex') === match[3], 'Cleanup source provenance mismatch');
    const alarms = [
      [config.cleanup_alarm_name, 'AWS/CodeBuild', 'FailedBuilds', 'ProjectName', config.cleanup_project_name],
      [config.cleanup_dispatcher_alarm_name, 'AWS/Lambda', 'Errors', 'FunctionName', config.cleanup_dispatcher_arn.split(':').at(-1)],
      [config.cleanup_dispatcher_throttle_alarm_name, 'AWS/Lambda', 'Throttles', 'FunctionName', config.cleanup_dispatcher_arn.split(':').at(-1)],
      [config.cleanup_scheduler_alarm_name, 'AWS/Scheduler', 'TargetErrorCount', 'ScheduleGroup', config.cleanup_schedule_group_name],
    ];
    for (const [name, namespace, metric, dimension, value] of alarms) {
      const alarm = this.aws('cloudwatch', 'describe-alarms', { AlarmNames: [name] }).MetricAlarms?.[0];
      requireThat(alarm?.ActionsEnabled && isDeepStrictEqual(alarm.AlarmActions, [config.cleanup_failure_topic_arn])
        && alarm.Namespace === namespace && alarm.MetricName === metric && alarm.Statistic === 'Sum'
        && isDeepStrictEqual(alarm.Dimensions, [{ Name: dimension, Value: value }])
        && alarm.Threshold === 1 && alarm.ComparisonOperator === 'GreaterThanOrEqualToThreshold'
        && alarm.Period > 0 && alarm.Period <= 900 && alarm.EvaluationPeriods === 1, 'Cleanup failure alarm must be armed');
    }
    const topic = this.aws('sns', 'get-topic-attributes', { TopicArn: config.cleanup_failure_topic_arn }).Attributes;
    const topicPolicy = JSON.parse(topic?.Policy ?? '{}');
    requireThat(topic?.TopicArn === config.cleanup_failure_topic_arn && topicPolicy.Statement?.some(statement =>
      statement.Effect === 'Allow' && statement.Action === 'sns:Publish' && statement.Resource === config.cleanup_failure_topic_arn
      && isDeepStrictEqual(statement.Principal, { Service: 'cloudwatch.amazonaws.com' })
      && statement.Condition?.StringEquals?.['aws:SourceAccount'] === config.account_id
      && Array.isArray(statement.Condition?.ArnEquals?.['aws:SourceArn'])
      && isDeepStrictEqual(statement.Condition.ArnEquals['aws:SourceArn'].toSorted(), alarms.map(([name]) =>
        `arn:aws:cloudwatch:${config.region}:${config.account_id}:alarm:${name}`).toSorted())), 'Cleanup SNS policy must allow the exact account alarms');
    this.verifyRole(project.serviceRole, 'codebuild.amazonaws.com', project.arn);
    this.verifyRole(dispatcher.Role, 'lambda.amazonaws.com');
    this.verifyRole(schedule.Target.RoleArn, 'scheduler.amazonaws.com',
      `arn:aws:scheduler:${config.region}:${config.account_id}:schedule-group/${config.cleanup_schedule_group_name}`);
    const identity = this.aws('sts', 'get-caller-identity');
    requireThat(identity.Account === config.account_id, 'Unexpected AWS account');
    if (!this.dependencies.env.CODEBUILD_BUILD_ID) {
      requireThat(!identity.Arn?.includes(`assumed-role/${project.serviceRole.split('/').at(-1)}/`), 'GitHub must not use the independent cleanup role');
    }
  }

  verifySecrets(initial = false) {
    for (const [name, arn] of Object.entries(this.config.secret_arns)) {
      const secret = this.aws('secretsmanager', 'describe-secret', { SecretId: arn });
      requireThat(secret.ARN === arn && !secret.DeletedDate && (initial && name.startsWith('db-')
        || Object.values(secret.VersionIdsToStages ?? {}).some(stages => stages.includes('AWSCURRENT'))), 'Required secret has no current version or is deleted');
    }
  }

  terraform(release, enabled, destroy = false) {
    this.assertLock();
    const state = destroy ? this.state() : this.phase();
    const deadline = destroy ? this.workerDeadline : Math.min(this.workerDeadline, state.expiresAt,
      state.status === 'starting' ? state.startupDeadline : this.workerDeadline);
    const variables = this.releaseFile('demo.tfvars.json', terraformVariables(this.config, release, enabled));
    const plan = join(this.directory, 'demo.tfplan');
    const invoke = args => {
      this.assertLock();
      if (!destroy) this.phase();
      const remaining = deadline - this.dependencies.now();
      requireThat(remaining > 0, 'Terraform phase deadline reached');
      this.terraformUsed = true;
      try {
        return this.dependencies.run('terraform', [`-chdir=${join(ROOT, 'infra/demo')}`, ...args],
        { timeout: Math.min(75 * 60_000, remaining), killSignal: 'SIGKILL', env: {
          ...this.dependencies.env, TF_DATA_DIR: join(this.directory, '.terraform'), TF_IN_AUTOMATION: 'true', TF_INPUT: 'false',
          TF_WORKSPACE: 'default', TF_CLI_ARGS: '', TF_CLI_ARGS_init: '', TF_CLI_ARGS_plan: '', TF_CLI_ARGS_apply: '',
        } });
      } catch (error) {
        if (error.uncertainWriter) this.uncertainWriter = true;
        throw error;
      }
    };
    invoke(['init', '-input=false', '-reconfigure', `-backend-config=bucket=${this.config.state_bucket_name}`,
      `-backend-config=key=${this.config.demo_state_key}`, `-backend-config=region=${this.config.region}`,
      '-backend-config=workspace_key_prefix=demo/workspaces', '-backend-config=use_lockfile=true']);
    invoke(['plan', '-input=false', '-lock-timeout=60s', ...(destroy ? ['-destroy'] : []), `-var-file=${variables}`, `-out=${plan}`]);
    invoke(['apply', '-input=false', '-lock-timeout=60s', plan]);
    this.assertLock();
    return JSON.parse(invoke(['output', '-json']));
  }

  initialize(outputs) {
    this.phase();
    const taskDefinition = outputs.initializer_task_definition_arn?.value;
    const networkConfiguration = { awsvpcConfiguration: { subnets: outputs.subnet_ids?.value,
      securityGroups: outputs.initializer_security_group_ids?.value, assignPublicIp: 'ENABLED' } };
    requireThat(taskDefinition?.startsWith(`arn:aws:ecs:${this.config.region}:${this.config.account_id}:task-definition/`)
      && Array.isArray(networkConfiguration.awsvpcConfiguration.subnets) && networkConfiguration.awsvpcConfiguration.subnets.length > 0
      && Array.isArray(networkConfiguration.awsvpcConfiguration.securityGroups) && networkConfiguration.awsvpcConfiguration.securityGroups.length > 0, 'Terraform initializer outputs are missing');
    const run = this.aws('ecs', 'run-task', { cluster: this.config.cluster_arn, taskDefinition,
      launchType: 'FARGATE', networkConfiguration, count: 1, startedBy: `demo-${randomUUID()}` });
    requireThat(!run.failures?.length && run.tasks?.length === 1, 'Initializer could not start');
    const task = run.tasks[0].taskArn;
    this.initializerTask = task;
    while (true) {
      this.phase();
      const result = this.aws('ecs', 'describe-tasks', { cluster: this.config.cluster_arn, tasks: [task] });
      requireThat(!result.failures?.length && result.tasks?.length === 1, 'Initializer task missing');
      const current = result.tasks[0];
      if (current.lastStatus === 'STOPPED') {
        requireThat(current.containers?.length > 0 && current.containers.every(container => container.exitCode === 0), 'Initializer failed');
        this.initializerTask = undefined;
        return;
      }
      this.dependencies.sleep(10_000);
    }
  }

  async smoke(outputs) {
    const state = this.phase();
    const names = outputs.service_names?.value;
    requireThat(Array.isArray(names) && names.length === SERVICES.length && SERVICES.every(name => names.includes(`languagewise-demo-${name}`)), 'Expected 18 ECS service names');
    while (true) {
      this.phase();
      const services = [];
      for (let offset = 0; offset < names.length; offset += 10) {
        const result = this.aws('ecs', 'describe-services', { cluster: this.config.cluster_arn, services: names.slice(offset, offset + 10) });
        requireThat(!result.failures?.length, 'ECS services missing');
        services.push(...(result.services ?? []));
      }
      if (services.length === 18 && services.every(service => service.status === 'ACTIVE' && service.desiredCount === 1
        && service.runningCount === 1 && service.pendingCount === 0 && service.deployments?.length === 1
        && service.deployments[0].rolloutState === 'COMPLETED')) break;
      this.dependencies.sleep(10_000);
    }
    const url = outputs.url?.value;
    requireThat(typeof url === 'string' && /^https:\/\/[a-z0-9]+\.cloudfront\.net\/?$/.test(url), 'Expected an HTTPS CloudFront URL');
    const probes = [
      ['/health', 'json'], ...['/', '/mini-games/', '/quizzes-and-courses/', '/chat-discussion/', '/analytics/', '/quests-and-achievements/'].map(path => [path, 'html']),
      ...['mini-games', 'quizzes-courses', 'chat-discussion', 'leaderboard-analytics', 'quests-achievements']
        .map(remote => [`/remotes/${remote}/remoteEntry.js`, 'javascript']),
    ];
    for (const [path, expectedType] of probes) {
      this.phase();
      await this.dependencies.probe(new URL(path, url).href, expectedType);
    }
    this.phase();
    this.dependencies.report(`Demo ${state.releaseSha}: ${url}\nExpires ${new Date(state.expiresAt).toISOString()} (two hours from Up request).`);
  }

  reserve(previous, release) {
    this.assertLock();
    const month = new Date(this.started).toISOString().slice(0, 7);
    const id = `quota#${month}`;
    const quota = decode(this.get(id));
    requireThat(this.dependencies.now() < this.started + 60 * 60_000, 'Startup deadline elapsed during preflight');
    const next = beginUp(previous, quota, release, this.started);
    if (next.repeated) return next;
    this.aws('dynamodb', 'transact-write-items', { TransactItems: [this.lockCondition(), this.statePut(next.state, previous),
      { Put: { TableName: this.config.control_table_name, Item: { ...key(id), payload: encode(next.quota) } } }] });
    return next;
  }

  verifyResiduals() {
    const config = this.config;
    const tags = this.aws('resourcegroupstaggingapi', 'get-resources', { TagFilters: [
      { Key: 'Project', Values: ['languagewise'] }, { Key: 'Lifecycle', Values: ['disposable-demo'] }], ResourcesPerPage: 100 });
    const retainedDefinition = new RegExp(`^arn:aws:ecs:${config.region}:${config.account_id}:task-definition/languagewise-demo-[a-z0-9-]+:[1-9][0-9]*$`);
    requireThat(!tags.PaginationToken && (tags.ResourceTagMappingList ?? [])
      .every(resource => retainedDefinition.test(resource.ResourceARN)), 'Tagged runtime resources remain');
    const vpcs = this.aws('ec2', 'describe-vpcs', { Filters: [{ Name: 'tag:Project', Values: ['languagewise'] }, { Name: 'tag:Lifecycle', Values: ['disposable-demo'] }] }).Vpcs ?? [];
    requireThat(vpcs.length === 0, 'Demo VPC remains; dependent ALB/NAT/ENIs/endpoints may survive');
    const services = this.aws('ecs', 'list-services', { cluster: config.cluster_arn });
    const tasks = this.aws('ecs', 'list-tasks', { cluster: config.cluster_arn });
    requireThat(!services.nextToken && !tasks.nextToken && !services.serviceArns?.length && !tasks.taskArns?.length, 'ECS services/tasks remain in dedicated cluster');
    for (const [operation, field] of [['describe-nat-gateways', 'NatGateways'], ['describe-addresses', 'Addresses'], ['describe-network-interfaces', 'NetworkInterfaces'], ['describe-vpc-endpoints', 'VpcEndpoints']]) {
      const result = this.aws('ec2', operation, { Filters: [{ Name: 'tag:Project', Values: ['languagewise'] }, { Name: 'tag:Lifecycle', Values: ['disposable-demo'] }] });
      requireThat(!result.NextToken && !(result[field] ?? []).filter(item => item.State !== 'deleted').length, 'Residual demo network resources remain');
    }
    const databases = this.aws('rds', 'describe-db-instances');
    const snapshots = this.aws('rds', 'describe-db-snapshots', { SnapshotType: 'manual' });
    const backups = this.aws('rds', 'describe-db-instance-automated-backups');
    const loadBalancers = this.aws('elbv2', 'describe-load-balancers');
    for (const [result, field, identity] of [[databases, 'DBInstances', 'DBInstanceIdentifier'], [snapshots, 'DBSnapshots', 'DBInstanceIdentifier'],
      [backups, 'DBInstanceAutomatedBackups', 'DBInstanceIdentifier'], [loadBalancers, 'LoadBalancers', 'LoadBalancerName']]) {
      requireThat(!result.Marker && !result.NextMarker && !(result[field] ?? []).some(item => /^languagewise-demo(?:-|$)/.test(item[identity])), 'Residual demo RDS/backup/load balancer remains');
    }
  }

  async destroy() {
    this.assertLock();
    const state = this.state();
    requireThat(state?.desiredMode === 'down', 'Down intent changed');
    const release = state.release ? validateRelease(state.release, this.config) : this.readRelease();
    if (!release) {
      try {
        this.aws('s3api', 'get-object', { Bucket: this.config.state_bucket_name, Key: this.config.demo_state_key },
          60_000, [join(this.directory, 'unconfigured-state.json')]);
      } catch (error) {
        if (error.code !== 'NoSuchKey') throw error;
        this.verifyResiduals();
        const current = this.state();
        this.save({ ...current, status: 'down', revision: current.revision + 1 }, current);
        this.dependencies.report('Demo is Down; no release or disposable state has been created.');
        return;
      }
      throw new Error('Existing disposable state requires its persisted release before destroy');
    }
    const running = this.aws('ecs', 'list-tasks', { cluster: this.config.cluster_arn,
      family: 'languagewise-demo-database-initializer', desiredStatus: 'RUNNING' });
    requireThat(!running.nextToken, 'Initializer task inventory incomplete');
    const tasks = [...new Set([...(running.taskArns ?? []), ...(this.initializerTask ? [this.initializerTask] : [])])];
    for (const task of tasks) {
      this.assertLock();
      this.aws('ecs', 'stop-task', { cluster: this.config.cluster_arn, task, reason: 'Demo Down cancels initialization' });
    }
    while (tasks.length > 0) {
      this.assertLock();
      const result = this.aws('ecs', 'describe-tasks', { cluster: this.config.cluster_arn, tasks });
      requireThat(!result.failures?.length && result.tasks?.length === tasks.length, 'Cannot verify initializer termination');
      if (result.tasks.every(task => task.lastStatus === 'STOPPED')) break;
      this.dependencies.sleep(5_000);
    }
    this.terraform(release, false, true);
    this.verifyResiduals();
    const current = this.state();
    this.save({ ...current, status: 'down', revision: current.revision + 1 }, current);
    this.dependencies.report('Demo Down verified; foundation and non-billed demo task-definition revisions are retained; task definitions are forgotten from state.');
  }

  async execute(command, manifest) {
    requireThat(['up', 'down', 'cleanup', 'release'].includes(command), 'Expected up|down|cleanup|release');
    try {
      if (command === 'cleanup' && !this.markDown(true)) return;
      if (command === 'down') this.markDown();
      if (command === 'cleanup') this.verifyCleanup();
      this.acquire(command === 'cleanup');
      if (command === 'down' || command === 'cleanup') return await this.destroy();
      this.verifyCleanup();
      if (command === 'up') {
        const release = this.readRelease();
        requireThat(release, 'Publish a complete tested release before Up');
        const previous = this.state();
        if (previous?.desiredMode === 'up') {
          beginUp(previous, undefined, release, this.dependencies.now());
          this.dependencies.report('Demo already requested Up; lease, quota and initializer unchanged.');
          return;
        }
        this.verifySecrets(true);
        const next = this.reserve(previous, release);
        try {
          this.verifyCleanup();
          const infrastructure = this.terraform(release, false);
          this.initialize(infrastructure);
          this.verifySecrets();
          const outputs = this.terraform(release, true);
          await this.smoke(outputs);
          const current = this.phase();
          this.save({ ...current, status: 'ready', revision: current.revision + 1 }, current);
        } catch (error) {
          this.markDown();
          try { await this.destroy(); } catch { this.dependencies.report('Up failed; cleanup remains required and expiry is retained.'); }
          throw error;
        }
      } else {
        const release = validateRelease(manifest, this.config);
        const previousLatest = this.readRelease();
        if (releaseOrder(release, previousLatest) === 'same') return;
        const state = this.state();
        if (state?.desiredMode !== 'up') { this.publish(release); return; }
        requireThat(state.status === 'ready', 'Cannot release into an incomplete Up');
        this.phase();
        this.verifySecrets();
        const previous = validateRelease(state.release, this.config);
        try {
          const outputs = this.terraform(release, true);
          await this.smoke(outputs);
          this.phase();
          const current = this.phase();
          this.save({ ...current, releaseSha: release.sha, release, revision: current.revision + 1 }, current);
          this.publish(release);
        } catch (error) {
          try {
            this.phase();
            const outputs = this.terraform(previous, true);
            await this.smoke(outputs);
            const current = this.phase();
            this.save({ ...current, releaseSha: previous.sha, release: previous, revision: current.revision + 1 }, current);
          } catch {
            this.markDown();
            try { await this.destroy(); } catch { this.dependencies.report('Rollback failed; cleanup remains required.'); }
          }
          if (!this.uncertainWriter) {
            const latest = this.readRelease();
            if (previousLatest && latest?.sha === release.sha) this.writeLatest(previousLatest);
          }
          throw error;
        }
      }
    } catch (error) {
      throw error;
    } finally {
      if (this.locked && !this.uncertainWriter && this.terraformUsed) {
        try {
          this.assertLock();
          this.aws('s3api', 'get-object', { Bucket: this.config.state_bucket_name,
            Key: `${this.config.demo_state_key}.tflock` }, 60_000, [join(this.directory, 'release-check.tflock')]);
          this.uncertainWriter = true;
        } catch (error) { if (error.code !== 'NoSuchKey') this.uncertainWriter = true; }
      }
      if (this.locked && !this.uncertainWriter) {
        try {
          this.aws('dynamodb', 'delete-item', { TableName: this.config.control_table_name, Key: key('lock'),
            ConditionExpression: '#owner = :owner', ExpressionAttributeNames: { '#owner': 'owner' },
            ExpressionAttributeValues: { ':owner': { S: this.owner } } });
        } catch { this.dependencies.report('Lock release failed; operator must verify worker termination before recovery.'); }
      }
      if (this.uncertainWriter) this.dependencies.report('Uncertain writer or recovery: lock retained. Bounded independent cleanup or manual verification required.');
      rmSync(this.directory, { recursive: true, force: true });
    }
  }
}

export async function main(args = process.argv.slice(2), dependencies = nativeDependencies()) {
  requireThat(args.length === 1 && ['up', 'down', 'cleanup', 'release'].includes(args[0]), 'Usage: node infra/scripts/demo.mjs up|down|cleanup|release');
  const foundation = loadFoundation(dependencies.env.FOUNDATION_CONFIG);
  const manifest = args[0] === 'release'
    ? JSON.parse(readFileSync(resolve(dependencies.env.RELEASE_MANIFEST ?? 'infra/generated/release.json'), 'utf8')) : undefined;
  return new DemoController(foundation, dependencies).execute(args[0], manifest);
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  main().catch(() => {
    console.error('Demo command failed. Output suppressed to protect credentials/state; inspect control status and cleanup alarm.');
    process.exitCode = 1;
  });
}