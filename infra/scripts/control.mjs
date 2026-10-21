export const SERVICES = Object.freeze([
  'shared-frontend', 'shared-backend', 'shared-db-api',
  'mini-games-service-frontend', 'mini-games-service-backend', 'mini-games-service-db',
  'quizzes-courses-service-frontend', 'quizzes-courses-service-backend', 'quizzes-courses-service-db',
  'chat-discussion-service-frontend', 'chat-discussion-service-backend', 'chat-discussion-service-db',
  'leaderboard-analytics-service-frontend', 'leaderboard-analytics-service-backend',
  'quests-achievements-notifications-service-frontend', 'quests-achievements-notifications-service-backend',
  'quests-achievements-notifications-service-db', 'garry-ai-service-backend',
]);
export const IMAGE_KEYS = Object.freeze([...SERVICES, 'database-initializer']);
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
  requireThat(!Object.keys(config.secret_arns).some(name => /mcp|rag/i.test(name)), 'MCP/RAG secrets are excluded');
  return config;
}

export function validateRelease(release, foundation) {
  requireThat(release && /^[a-f0-9]{40}$/.test(release.sha), 'Release SHA must be 40 lowercase hex characters');
  requireThat(Number.isSafeInteger(release.sequence) && release.sequence > 0, 'Release sequence must be a positive safe integer');
  requireThat(release.images && Object.getPrototypeOf(release.images) === Object.prototype, 'Missing images');
  requireThat(Object.keys(release.images).length === IMAGE_KEYS.length && IMAGE_KEYS.every(key => Object.hasOwn(release.images, key)), 'Release requires exactly 18 services and initializer; MCP/RAG are excluded');
  for (const image of Object.values(release.images)) {
    requireThat(typeof image === 'string' && image.startsWith(`${foundation.repository_url}@sha256:`)
      && /^[a-f0-9]{64}$/.test(image.slice(`${foundation.repository_url}@sha256:`.length)), 'Images must be exact repository@sha256 digests');
  }
  return { sha: release.sha, images: { ...release.images }, sequence: release.sequence };
}

export function releaseOrder(candidate, previous) {
  if (!previous) return 'new';
  requireThat(candidate.sequence >= previous.sequence, 'Stale release sequence');
  if (candidate.sequence === previous.sequence || candidate.sha === previous.sha) {
    requireThat(candidate.sha === previous.sha && candidate.sequence === previous.sequence
      && IMAGE_KEYS.every(key => candidate.images[key] === previous.images[key]), 'Release identity conflict');
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
  return { foundation, release: { sha: release.sha, images: release.images }, services_enabled: enabled };
}

export function canAcquireLock(existing) {
  return !existing;
}