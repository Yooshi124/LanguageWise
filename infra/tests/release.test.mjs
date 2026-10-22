import assert from 'node:assert/strict';
import { test } from 'node:test';
import { REQUIRED_WORKFLOWS, buildArguments, checkRuns, validateCandidate } from '../scripts/release.mjs';
import { IMAGE_KEYS } from '../scripts/control.mjs';
import { allowedSource } from '../scripts/package-cleanup.mjs';
import { readFileSync } from 'node:fs';

const sha = 'a'.repeat(40);
const runs = REQUIRED_WORKFLOWS.map((workflow, index) => ({ id: index + 1,
  path: `.github/workflows/${workflow}`, head_sha: sha, head_branch: 'master',
  event: 'push', status: 'completed', conclusion: 'success' }));

test('all exact-SHA workflows are mandatory', () => {
  for (const workflow of ['mcp-server.yml', 'rag-server.yml', 'ai-e2e.yml', 'aws-infrastructure.yml']) {
    assert.ok(REQUIRED_WORKFLOWS.includes(workflow));
    assert.equal(checkRuns(runs.filter(run => run.path !== `.github/workflows/${workflow}`), sha), false);
  }
  assert.equal(checkRuns(runs, sha), true);
  assert.equal(checkRuns(runs.slice(1), sha), false);
  assert.equal(checkRuns(runs.map(run => ({ ...run, head_sha: 'b'.repeat(40) })), sha), false);
  assert.equal(checkRuns(runs.map(run => ({ ...run, event: 'pull_request' })), sha), false);
});

test('failed or newer in-flight reruns cannot reuse an old success', () => {
  assert.throws(() => checkRuns([{ ...runs[0], id: 100, conclusion: 'failure' }, ...runs], sha), /failed/);
  assert.equal(checkRuns([{ ...runs[0], id: 100, status: 'in_progress' }, ...runs], sha), false);
});

test('build contexts match every Compose application image', () => {
  for (const service of IMAGE_KEYS) {
    const args = buildArguments(service, `example:${service}`, sha);
    assert.equal(args[0], 'build');
    assert.ok(args.includes('linux/amd64'));
    if (service.endsWith('-frontend')) assert.match(args.at(-1), /\/frontend$/);
    if (service.endsWith('-backend')) assert.match(args.at(-1), /\/backend$/);
  }
  assert.equal(buildArguments('shared-backend', 'example', sha).at(-1), 'shared/backend');
  assert.equal(buildArguments('garry-ai-service-backend', 'example', sha).at(-1), 'garry-ai-service/backend');
  assert.equal(buildArguments('mini-games-service-backend', 'example', sha).at(-1), 'mini-games-service/backend');
  assert.ok(buildArguments('shared-db-api', 'example', sha).includes('APPLICATION_IMAGE=languagewise-build/shared-db-api:' + sha));
  assert.equal(buildArguments('mcp-server', 'example', sha).at(-1), 'mcp-server');
  assert.equal(buildArguments('rag-server', 'example', sha).at(-1), 'rag-server');
  assert.throws(() => buildArguments('untrusted-server', 'example', sha), /Unknown/);
});

test('cleanup packaging admits only runtime configuration and controller source', () => {
  for (const path of ['infra/demo/database.tf', 'infra/demo/.terraform.lock.hcl', 'infra/scripts/demo.mjs', 'infra/scripts/control.mjs']) {
    assert.equal(allowedSource(path), true);
  }
  for (const path of ['infra/demo/terraform.tfstate', 'infra/demo/secrets.tfvars.json', 'infra/demo/.terraform/provider.exe',
    'infra/foundation/main.tf', 'infra/scripts/.env', 'infra/demo/tests/state.tf']) assert.equal(allowedSource(path), false);
});

test('demo resource names and creation tags match the foundation IAM boundary', () => {
  const read = name => readFileSync(new URL(`../demo/${name}`, import.meta.url), 'utf8');
  assert.match(read('gateway.tf'), /name\s*= "languagewise-demo-gateway"/);
  assert.match(read('database.tf'), /identifier\s*= "languagewise-demo-postgres"/);
  assert.match(read('variables.tf'), /Layer\s*= "demo"/);
  assert.match(read('services.tf'), /ReverseProxy__TrustedNetwork\s*= "10\.82\.0\.0\/16"/);
});

test('secret-bearing workflows are trusted, pinned, bounded and serialized', () => {
  for (const filename of ['aws-demo.yml', 'aws-release.yml']) {
    const text = readFileSync(new URL(`../../.github/workflows/${filename}`, import.meta.url), 'utf8');
    assert.match(text, /group: languagewise-aws-demo/);
    assert.match(text, /cancel-in-progress: false/);
    assert.match(text, /github\.repository == 'Yooshi124\/LanguageWise' && github\.ref == 'refs\/heads\/master'/);
    assert.match(text, /timeout-minutes: 100/);
    assert.doesNotMatch(text, /pull_request_target|id-token: write|self-hosted|upload-artifact/);
    for (const match of text.matchAll(/uses: ([^\s]+)/g)) assert.match(match[1], /@[a-f0-9]{40}$/);
    assert.match(text, /role-duration-seconds: 7200/);
    assert.match(text, /role-skip-session-tagging: true/);
  }
});

test('candidate content must match its exact tested workflow SHA', () => {
  const config = { repository_url: '047754151573.dkr.ecr.ap-southeast-2.amazonaws.com/41026release2/languagewise' };
  const manifest = { schemaVersion: 2, sha, sequence: 7, images: Object.fromEntries(IMAGE_KEYS.map(name => [name, `${config.repository_url}@sha256:${'b'.repeat(64)}`])) };
  assert.equal(validateCandidate(manifest, sha, config).sequence, 7);
  assert.throws(() => validateCandidate(manifest, 'c'.repeat(40), config), /tested workflow SHA/);
  const source = readFileSync(new URL('../scripts/release.mjs', import.meta.url), 'utf8');
  assert.match(source, /Reusing the immutable published release/);
  assert.match(source, /writeFileSync\('infra\/generated\/release.json', JSON.stringify\(existing/);
});