export const SERVICES = Object.freeze([
  'shared-frontend', 'shared-backend', 'shared-db-api',
  'mini-games-service-frontend', 'mini-games-service-backend', 'mini-games-service-db',
  'quizzes-courses-service-frontend', 'quizzes-courses-service-backend', 'quizzes-courses-service-db',
  'chat-discussion-service-frontend', 'chat-discussion-service-backend', 'chat-discussion-service-db',
  'leaderboard-analytics-service-frontend', 'leaderboard-analytics-service-backend',
  'quests-achievements-notifications-service-frontend', 'quests-achievements-notifications-service-backend',
  'quests-achievements-notifications-service-db', 'garry-ai-service-backend', 'mcp-server', 'rag-server',
]);
export const IMAGE_KEYS = Object.freeze([...SERVICES, 'database-initializer']);
export const LEGACY_IMAGE_KEYS = Object.freeze(IMAGE_KEYS.filter(name => !['mcp-server', 'rag-server'].includes(name)));
export const HOUR = 3_600_000;
export const LOCK_WINDOW = 150 * 60_000;
export const WORKER_WINDOW = 90 * 60_000;

export function requireThat(condition, message) {
  if (!condition) throw new Error(message);
}

export function validateFoundation(config) {
  requireThat(config && Object.getPrototypeOf(config) === Object.prototype, 'Foundation must be an object');
  const fields = ['account_id', 'region', 'cluster_arn', 'repository_url', 'control_table_name',
    'artifact_bucket_name', 'cleanup_project_name', 'cleanup_schedule_name', 'cleanup_schedule_group_name',
    'cleanup_alarm_name', 'state_bucket_name', 'demo_state_key', 'service_execution_role_arns',
    'service_task_role_arns', 'secret_arns', 'deploy_role_arn', 'control_role_arn', 'release_role_arn',
    'cleanup_role_arn', 'cleanup_dispatcher_arn', 'cleanup_dispatcher_role_arn', 'cleanup_failure_topic_arn',
    'cleanup_dispatcher_alarm_name', 'cleanup_dispatcher_throttle_alarm_name', 'cleanup_scheduler_alarm_name'];
  requireThat(Object.keys(config).every(field => fields.includes(field)), 'Unexpected foundation field; plaintext secret inputs are forbidden');
  for (const field of ['deploy_role_arn', 'control_role_arn', 'release_role_arn']) {
    requireThat(!Object.hasOwn(config, field) || config[field]?.startsWith(`arn:aws:iam::${config.account_id}:role/`), `Invalid role ARN: ${field}`);
  }
  for (const key of ['account_id', 'region', 'cluster_arn', 'repository_url', 'control_table_name',
    'artifact_bucket_name', 'cleanup_project_name', 'cleanup_schedule_name', 'cleanup_schedule_group_name',
    'cleanup_alarm_name', 'state_bucket_name', 'demo_state_key', 'cleanup_role_arn', 'cleanup_dispatcher_arn',
    'cleanup_dispatcher_role_arn', 'cleanup_failure_topic_arn', 'cleanup_dispatcher_alarm_name',
    'cleanup_dispatcher_throttle_alarm_name', 'cleanup_scheduler_alarm_name']) {
    requireThat(typeof config[key] === 'string' && config[key].length > 0 && !/[\r\n]/.test(config[key]), `Invalid foundation field: ${key}`);
  }
  requireThat(/^\d{12}$/.test(config.account_id), 'Invalid account');
  for (const field of ['cleanup_role_arn', 'cleanup_dispatcher_role_arn']) {
    requireThat(config[field].startsWith(`arn:aws:iam::${config.account_id}:role/`), `Invalid role ARN: ${field}`);
  }
  requireThat(new RegExp(`^arn:aws:lambda:${config.region}:${config.account_id}:function:[a-zA-Z0-9_-]+$`).test(config.cleanup_dispatcher_arn), 'Invalid dispatcher ARN');
  requireThat(new RegExp(`^arn:aws:sns:${config.region}:${config.account_id}:[a-zA-Z0-9_-]+$`).test(config.cleanup_failure_topic_arn), 'Invalid cleanup notification ARN');
  requireThat(/^demo\/[a-zA-Z0-9/_-]+\.tfstate$/.test(config.demo_state_key), 'State key must stay in the disposable demo/ prefix');
  requireThat(/^[a-z]{2}-[a-z]+-\d$/.test(config.region), 'Invalid region');
  requireThat(config.cluster_arn.startsWith(`arn:aws:ecs:${config.region}:${config.account_id}:cluster/`), 'Invalid cluster');
  requireThat(new RegExp(`^${config.account_id}\\.dkr\\.ecr\\.${config.region}\\.amazonaws\\.com/[a-z0-9/_-]+$`).test(config.repository_url), 'Invalid repository');
  for (const key of ['service_execution_role_arns', 'service_task_role_arns', 'secret_arns']) {
    requireThat(config[key] && Object.getPrototypeOf(config[key]) === Object.prototype, `Invalid ARN map: ${key}`);
    for (const arn of Object.values(config[key])) {
      requireThat(typeof arn === 'string' && arn.startsWith('arn:aws:') && arn.split(':')[4] === config.account_id, `Invalid ARN in ${key}`);
    }
  }
  for (const service of IMAGE_KEYS) {
    for (const key of ['service_execution_role_arns', 'service_task_role_arns']) {
      requireThat(config[key][service]?.startsWith(`arn:aws:iam::${config.account_id}:role/`), `Missing role: ${service}`);
    }
  }
  requireThat(Object.keys(config.secret_arns).length > 0, 'Secret metadata is required');
  requireThat(Object.values(config.secret_arns).every(arn => arn.startsWith(`arn:aws:secretsmanager:${config.region}:${config.account_id}:secret:`)), 'Invalid secret ARN');
  requireThat(Object.hasOwn(config.secret_arns, 'mcp-api-key'), 'Private MCP secret metadata is required');
  requireThat(!Object.keys(config.secret_arns).some(name => /rag/i.test(name)), 'RAG technical credentials are not a cloud dependency');
  return config;
}

export function validateRelease(release, foundation, { allowLegacy = false } = {}) {
  requireThat(release && /^[a-f0-9]{40}$/.test(release.sha), 'Release SHA must be 40 lowercase hex characters');
  requireThat(Number.isSafeInteger(release.sequence) && release.sequence > 0, 'Release sequence must be a positive safe integer');
  requireThat(release.images && Object.getPrototypeOf(release.images) === Object.prototype, 'Missing images');
  const legacy = release.schemaVersion === undefined;
  requireThat(release.schemaVersion === 2 || (allowLegacy && legacy), 'Release schema version 2 is required; legacy releases are read/cleanup only');
  const keys = legacy ? LEGACY_IMAGE_KEYS : IMAGE_KEYS;
  requireThat(Object.keys(release.images).length === keys.length && keys.every(key => Object.hasOwn(release.images, key)), 'Release image inventory must exactly match its schema version');
  for (const image of Object.values(release.images)) {
    requireThat(typeof image === 'string' && image.startsWith(`${foundation.repository_url}@sha256:`)
      && /^[a-f0-9]{64}$/.test(image.slice(`${foundation.repository_url}@sha256:`.length)), 'Images must be exact repository@sha256 digests');
  }
  for (const field of ['imageSourceSha', 'reusedFromSha']) {
    requireThat(release[field] === undefined || !legacy && typeof release[field] === 'string' && /^[a-f0-9]{40}$/.test(release[field]), 'Invalid image source provenance');
  }
  requireThat(release.reusedFromSha === undefined || release.imageSourceSha !== undefined && release.reusedFromSha !== release.sha,
    'Reused images require source provenance and a different approved release');
  requireThat(release.imageSourceSha === undefined || release.reusedFromSha !== undefined || release.imageSourceSha === release.sha,
    'Built images must originate from the current release');
  return { ...(legacy ? {} : { schemaVersion: 2 }), sha: release.sha, images: { ...release.images }, sequence: release.sequence,
    ...(release.imageSourceSha === undefined ? {} : { imageSourceSha: release.imageSourceSha }),
    ...(release.reusedFromSha === undefined ? {} : { reusedFromSha: release.reusedFromSha }) };
}

export function releaseOrder(candidate, previous) {
  if (!previous) return 'new';
  requireThat(candidate.sequence >= previous.sequence, 'Stale release sequence');
  if (candidate.sequence === previous.sequence || candidate.sha === previous.sha) {
    requireThat(candidate.sha === previous.sha && candidate.sequence === previous.sequence
      && candidate.schemaVersion === previous.schemaVersion
      && Object.keys(candidate.images).length === Object.keys(previous.images).length
      && Object.entries(candidate.images).every(([key, image]) => image === previous.images[key]), 'Release identity conflict');
    return 'same';
  }
  return 'new';
}

export function cleanupDue(state, now) {
  return Boolean(state && state.status !== 'down' && (state.desiredMode === 'down'
    || now >= state.expiresAt || (state.status === 'starting' && now >= state.startupDeadline)));
}

export function beginUp(state, quota, release, now) {
  if (state?.desiredMode === 'up') {
    requireThat(!cleanupDue(state, now), 'Existing demo has expired; run Down');
    return { state, quota, repeated: true };
  }
  requireThat(!state || state.status === 'down', 'Previous runtime must be fully removed before Up');
  requireThat((quota?.cycles ?? 0) < 10 && (quota?.hours ?? 0) + 4 <= 40, 'Monthly demo reservation exhausted');
  return {
    state: { desiredMode: 'up', status: 'starting', requestedAt: now, startupDeadline: now + HOUR,
      expiresAt: now + 2 * HOUR, reservedUntil: now + 4 * HOUR, releaseSha: release.sha, release,
      revision: (state?.revision ?? 0) + 1 },
    quota: { cycles: (quota?.cycles ?? 0) + 1, hours: (quota?.hours ?? 0) + 4 }, repeated: false,
  };
}

export function assertPhase(state, now) {
  requireThat(state?.desiredMode === 'up' && !cleanupDue(state, now), 'Demo cancelled or deadline reached');
}

export function terraformVariables(foundation, release, enabled) {
  requireThat(typeof enabled === 'boolean', 'services_enabled must be boolean');
  requireThat(!enabled || release.schemaVersion === 2, 'Legacy releases may only be destroyed');
  return { foundation, release: { sha: release.sha, images: release.images, schema_version: release.schemaVersion ?? 1 }, services_enabled: enabled };
}

export function canAcquireLock(existing) {
  return !existing;
}