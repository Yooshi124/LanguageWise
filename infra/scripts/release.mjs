import { execFileSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { isDeepStrictEqual } from 'node:util';
import { IMAGE_KEYS, requireThat, validateRelease } from './control.mjs';
import { loadFoundation } from './demo.mjs';

export const REQUIRED_WORKFLOWS = Object.freeze([
  'shared.yml', 'mini-games-service.yml', 'quizzes-courses-service.yml',
  'chat-discussion-service.yml', 'quests-achievements-notifications-service.yml',
  'leaderboard-analytics-service.yml', 'mcp-server.yml', 'security-check.yml',
  'aws-infrastructure.yml', 'rag-server.yml', 'ai-e2e.yml',
]);

export function checkRuns(runs, sha) {
  requireThat(/^[a-f0-9]{40}$/.test(sha), 'Invalid release SHA');
  for (const workflow of REQUIRED_WORKFLOWS) {
    const matching = runs.filter(run => run.path === `.github/workflows/${workflow}`
      && run.head_sha === sha && run.head_branch === 'master'
      && ['push', 'workflow_dispatch'].includes(run.event)).sort((left, right) => right.id - left.id);
    if (!matching.length || matching[0].status !== 'completed') return false;
    requireThat(matching[0].conclusion === 'success', `Required workflow failed: ${workflow}`);
  }
  return true;
}

function run(binary, args, options = {}) {
  return execFileSync(binary, args, { encoding: 'utf8', shell: false, timeout: 120_000,
    maxBuffer: 16 * 1024 * 1024, ...options });
}

function github(path) {
  return JSON.parse(run('gh', ['api', `repos/Yooshi124/LanguageWise/${path}`]));
}

export function validateCandidate(candidate, sha, config) {
  const release = validateRelease(candidate, config);
  requireThat(release.sha === sha, 'Candidate does not match the tested workflow SHA');
  return release;
}

export function reuseManifest({ previous, archived, comparison, sha, config, sequence }) {
  validateCandidate(previous, previous?.sha, config);
  requireThat(previous.schemaVersion === 2 && isDeepStrictEqual(previous, archived), 'Image reuse requires the exact immutable published archive');
  requireThat(comparison?.status === 'ahead' && comparison.base_commit?.sha === previous.sha
    && comparison.merge_base_commit?.sha === previous.sha && Array.isArray(comparison.files)
    && comparison.files.length < 300, 'Complete ancestor comparison required for image reuse');
  const runtimeOnly = path => typeof path === 'string' && (
    ['README.md', 'infra/README.md', 'infra/demo/README.md', 'infra/scripts/demo.mjs'].includes(path)
    || /^infra\/demo\/(?:[a-zA-Z0-9_-]+\.tf|\.terraform\.lock\.hcl)$/.test(path)
    || /^infra\/demo\/tests\/[a-zA-Z0-9_-]+\.tftest\.hcl$/.test(path)
    || /^infra\/tests\/[a-zA-Z0-9_-]+\.test\.mjs$/.test(path)
    || /^docs\/(?:[a-zA-Z0-9_-]+\/)*[a-zA-Z0-9_-]+\.md$/.test(path));
  requireThat(comparison.files.every(file => ['added', 'modified', 'removed', 'renamed', 'copied', 'changed', 'unchanged'].includes(file.status)
    && runtimeOnly(file.filename) && (file.previous_filename === undefined || runtimeOnly(file.previous_filename))), 'Changed image inputs require a full build');
  const imageSourceSha = previous.imageSourceSha === undefined ? previous.sha : previous.imageSourceSha;
  requireThat(typeof imageSourceSha === 'string' && /^[a-f0-9]{40}$/.test(imageSourceSha), 'Invalid image source provenance');
  return validateCandidate({ schemaVersion: 2, sha, sequence, images: previous.images,
    reusedFromSha: previous.sha, imageSourceSha }, sha, config);
}

export function assertMaster(sha) {
  requireThat(github('git/ref/heads/master').object?.sha === sha, 'Superseded release: only current master may publish/deploy');
  requireThat(run('git', ['rev-parse', 'HEAD']).trim() === sha, 'Checkout does not match release SHA');
}

async function gate(sha) {
  const deadline = Date.now() + 35 * 60_000;
  while (Date.now() < deadline) {
    assertMaster(sha);
    if (checkRuns(github(`actions/runs?head_sha=${sha}&per_page=100`).workflow_runs, sha)) return;
    await new Promise(resolveWait => setTimeout(resolveWait, 15_000));
  }
  throw new Error('Exact-SHA CI gate timed out; no AWS publication performed');
}

export function buildArguments(service, image, sha) {
  requireThat(IMAGE_KEYS.includes(service), 'Unknown image');
  const common = ['build', '--platform', 'linux/amd64', '--tag', image];
  if (service === 'database-initializer') return [...common, '--file', 'infra/images/database-initializer/Dockerfile', '.'];
  if (service === 'quests-achievements-notifications-service-db') return [...common, '--file', 'infra/images/postgrest/Dockerfile', '.'];
  if (['mcp-server', 'rag-server'].includes(service)) return [...common, service];
  const base = service.startsWith('shared-') ? 'shared' : service === 'garry-ai-service-backend' ? 'garry-ai-service'
    : service.slice(0, service.lastIndexOf('-'));
  const tier = service.endsWith('-frontend') ? 'frontend' : service.endsWith('-backend') ? 'backend' : 'database';
  if (tier === 'database') return [...common, '--file', 'infra/images/database-api/Dockerfile',
    '--build-arg', `APPLICATION_IMAGE=languagewise-build/${service}:${sha}`, '.'];
  return [...common, `${base}/${tier}`];
}

export function build(sha, config, sequence, dependencies = {}) {
  const execute = dependencies.run ?? run;
  const lookup = dependencies.github ?? github;
  const current = dependencies.assertMaster ?? assertMaster;
  const report = dependencies.report ?? console.log;
  const directory = dependencies.directory ?? 'infra/generated';
  const output = resolve(directory, 'release.json');
  current(sha);
  mkdirSync(directory, { recursive: true });
  const archived = resolve(directory, 'published.json');
  let published;
  try {
    execute('aws', ['s3api', 'get-object', '--bucket', config.artifact_bucket_name,
      '--key', `releases/${sha}.json`, '--region', config.region, archived]);
    published = JSON.parse(readFileSync(archived, 'utf8'));
  } catch (error) {
    if (!String(error.stderr).includes('NoSuchKey')) throw error;
  }
  if (published) {
    const existing = validateCandidate(published, sha, config);
    current(sha);
    writeFileSync(output, JSON.stringify(existing, null, 2), { mode: 0o600 });
    report('Reusing the immutable published release; no rebuild or new sequence.');
    return;
  }
  let reused;
  try {
    const previousFile = resolve(directory, 'previous.json');
    execute('aws', ['s3api', 'get-object', '--bucket', config.artifact_bucket_name,
      '--key', 'releases/latest.json', '--region', config.region, previousFile]);
    const previousContent = JSON.parse(readFileSync(previousFile, 'utf8'));
    const previous = validateCandidate(previousContent, previousContent?.sha, config);
    const archiveFile = resolve(directory, 'previous-archive.json');
    execute('aws', ['s3api', 'get-object', '--bucket', config.artifact_bucket_name,
      '--key', `releases/${previous.sha}.json`, '--region', config.region, archiveFile]);
    const candidate = reuseManifest({ previous, archived: JSON.parse(readFileSync(archiveFile, 'utf8')),
      comparison: lookup(`compare/${previous.sha}...${sha}`), sha, config, sequence });
    const digests = [...new Set(Object.values(candidate.images).map(image => image.split('@')[1]))];
    const inventory = JSON.parse(execute('aws', ['ecr', 'describe-images', '--repository-name', config.repository_url.split('/').slice(1).join('/'),
      '--image-ids', JSON.stringify(digests.map(imageDigest => ({ imageDigest }))), '--region', config.region, '--output', 'json']));
    requireThat(Array.isArray(inventory.imageDetails) && inventory.imageDetails.length === digests.length
      && new Set(inventory.imageDetails.map(image => image.imageDigest)).size === digests.length
      && inventory.imageDetails.every(image => digests.includes(image.imageDigest)), 'Reusable ECR inventory is incomplete');
    reused = candidate;
  } catch {
    report('Verified runtime-only image reuse unavailable; building all twenty-one images. Raw verification output suppressed.');
  }
  current(sha);
  if (reused) {
    writeFileSync(output, JSON.stringify(reused, null, 2), { mode: 0o600 });
    report(`Reusing twenty-one immutable images from approved release ${reused.reusedFromSha}; current-SHA checks and a new release sequence remain required.`);
    return;
  }
  const registry = config.repository_url.split('/')[0];
  const password = execute('aws', ['ecr', 'get-login-password', '--region', config.region], { stdio: ['ignore', 'pipe', 'pipe'] });
  execute('docker', ['login', '--username', 'AWS', '--password-stdin', registry], { input: password, stdio: ['pipe', 'pipe', 'pipe'] });
  const images = {};
  try {
    for (const service of IMAGE_KEYS) {
      const tag = `${config.repository_url}:${service}-${sha}`;
      const args = buildArguments(service, tag, sha);
      if (['shared-db-api', 'mini-games-service-db', 'quizzes-courses-service-db', 'chat-discussion-service-db'].includes(service)) {
        const folder = service === 'shared-db-api' ? 'shared' : service.slice(0, -3);
        execute('docker', ['build', '--platform', 'linux/amd64', '--tag', `languagewise-build/${service}:${sha}`, `${folder}/database`],
          { stdio: 'inherit', timeout: 20 * 60_000 });
      }
      execute('docker', args, { stdio: 'inherit', timeout: 20 * 60_000 });
      execute('docker', ['push', tag], { stdio: 'inherit', timeout: 10 * 60_000 });
      const result = JSON.parse(execute('aws', ['ecr', 'describe-images', '--repository-name', config.repository_url.split('/').slice(1).join('/'),
        '--image-ids', `imageTag=${service}-${sha}`, '--region', config.region, '--output', 'json']));
      const digest = result.imageDetails?.[0]?.imageDigest;
      requireThat(/^sha256:[a-f0-9]{64}$/.test(digest), 'Published digest missing');
      images[service] = `${config.repository_url}@${digest}`;
      execute('docker', ['image', 'rm', tag], { stdio: ['ignore', 'pipe', 'pipe'] });
      execute('docker', ['builder', 'prune', '--force'], { stdio: ['ignore', 'pipe', 'pipe'] });
    }
    const release = validateRelease({ schemaVersion: 2, sha, images, sequence, imageSourceSha: sha }, config);
    writeFileSync(output, JSON.stringify(release, null, 2), { mode: 0o600 });
  } finally {
    execute('docker', ['logout', registry], { stdio: ['ignore', 'pipe', 'pipe'] });
  }
}

export async function main(args = process.argv.slice(2)) {
  const [command] = args;
  const sha = process.env.GITHUB_SHA;
  requireThat(args.length === 1 && /^[a-f0-9]{40}$/.test(sha), 'Trusted release command and SHA required');
  if (command === 'gate') return gate(sha);
  if (command === 'current') return assertMaster(sha);
  if (command === 'stage' || command === 'fetch') {
    const config = loadFoundation(process.env.FOUNDATION_CONFIG);
    const path = 'infra/generated/release.json';
    mkdirSync('infra/generated', { recursive: true });
    if (command === 'stage') {
      validateCandidate(JSON.parse(readFileSync(path, 'utf8')), sha, config);
      run('aws', ['s3api', 'put-object', '--bucket', config.artifact_bucket_name,
        '--key', `releases/candidates/${sha}.json`, '--body', path, '--region', config.region]);
    } else {
      run('aws', ['s3api', 'get-object', '--bucket', config.artifact_bucket_name,
        '--key', `releases/candidates/${sha}.json`, '--region', config.region, path]);
      validateCandidate(JSON.parse(readFileSync(path, 'utf8')), sha, config);
    }
    return;
  }
  requireThat(command === 'build', 'Expected gate|current|build');
  return build(sha, loadFoundation(process.env.FOUNDATION_CONFIG), Number(process.env.GITHUB_RUN_NUMBER));
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  main().catch(error => { console.error(error.message); process.exitCode = 1; });
}