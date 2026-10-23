import assert from 'node:assert/strict';
import { test } from 'node:test';
import { REQUIRED_WORKFLOWS, build, buildArguments, checkRuns, reuseManifest, validateCandidate } from '../scripts/release.mjs';
import { IMAGE_KEYS } from '../scripts/control.mjs';
import { allowedSource } from '../scripts/package-cleanup.mjs';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

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
  assert.match(source, /writeFileSync\(output, JSON.stringify\(existing/);
});

function reusableFixture(paths = ['infra/scripts/demo.mjs', 'infra/demo/services.tf', 'docs/demo.md']) {
  const config = { repository_url: '047754151573.dkr.ecr.ap-southeast-2.amazonaws.com/41026release2/languagewise' };
  const previous = { schemaVersion: 2, sha, sequence: 7,
    images: Object.fromEntries(IMAGE_KEYS.map(name => [name, `${config.repository_url}@sha256:${'b'.repeat(64)}`])) };
  return { previous, archived: structuredClone(previous), config, sha: 'c'.repeat(40), sequence: 8,
    comparison: { status: 'ahead', base_commit: { sha }, merge_base_commit: { sha },
      files: paths.map(filename => ({ filename, status: 'modified' })) } };
}

test('runtime-only releases reuse complete immutable images with current-SHA provenance and a new sequence', () => {
  const fixture = reusableFixture();
  const result = reuseManifest(fixture);
  assert.equal(result.sha, fixture.sha);
  assert.equal(result.sequence, 8);
  assert.equal(result.reusedFromSha, sha);
  assert.equal(result.imageSourceSha, sha);
  assert.deepEqual(result.images, fixture.previous.images);
  assert.equal(Object.keys(result.images).length, 21);
  fixture.previous.imageSourceSha = 'd'.repeat(40);
  fixture.previous.reusedFromSha = 'e'.repeat(40);
  fixture.archived = structuredClone(fixture.previous);
  assert.equal(reuseManifest(fixture).imageSourceSha, 'd'.repeat(40));
});

test('image inputs and publisher changes always require a full rebuild', () => {
  for (const path of ['shared/backend/Program.cs', 'rag-server/server.py', 'mcp-server/Dockerfile',
    'infra/images/database-api/Dockerfile', 'infra/scripts/initialize-databases.py', 'infra/scripts/release.mjs',
    'infra/scripts/control.mjs', '.github/workflows/aws-release.yml', 'nuget.config', '.dockerignore',
    'shared/database/LanguageWise.Shared.Db/sql/schema.sql', 'infra/demo/../images/postgrest/Dockerfile']) {
    assert.throws(() => reuseManifest(reusableFixture([path])), /image inputs|comparison/);
  }
});

test('image reuse rejects unproven archives, divergent histories, truncated comparisons and disguised renames', () => {
  const mutations = [
    fixture => { fixture.archived.sequence++; },
    fixture => { delete fixture.previous.images['rag-server']; },
    fixture => { fixture.comparison.status = 'diverged'; },
    fixture => { fixture.comparison.merge_base_commit.sha = 'd'.repeat(40); },
    fixture => { fixture.comparison.base_commit.sha = 'd'.repeat(40); },
    fixture => { delete fixture.comparison.files; },
    fixture => { fixture.comparison.files = Array.from({ length: 300 }, () => ({ filename: 'infra/demo/main.tf', status: 'modified' })); },
    fixture => { fixture.comparison.files[0] = { filename: 'docs/removed.md', previous_filename: 'rag-server/server.py', status: 'renamed' }; },
    fixture => { fixture.comparison.files[0].status = 'unknown'; },
    fixture => { fixture.previous.imageSourceSha = 'invalid'; fixture.archived = structuredClone(fixture.previous); },
  ];
  for (const mutate of mutations) {
    const fixture = reusableFixture();
    mutate(fixture);
    assert.throws(() => reuseManifest(fixture));
  }
});

test('candidate validation preserves valid provenance and rejects malformed or contradictory source claims', () => {
  const fixture = reusableFixture();
  const reused = reuseManifest(fixture);
  assert.deepEqual(validateCandidate(reused, fixture.sha, fixture.config), reused);
  for (const metadata of [{ imageSourceSha: 'invalid' }, { reusedFromSha: sha },
    { imageSourceSha: sha }, { imageSourceSha: sha, reusedFromSha: fixture.sha },
    { imageSourceSha: null, reusedFromSha: sha }]) {
    assert.throws(() => validateCandidate({ ...reused, imageSourceSha: undefined, reusedFromSha: undefined, ...metadata }, fixture.sha, fixture.config), /provenance|source|originate/);
  }
});

function publisherFixture(options = {}) {
  const fixture = reusableFixture(options.paths);
  const directory = mkdtempSync(join(tmpdir(), 'languagewise-release-test-'));
  const calls = [];
  const reports = [];
  const dependencies = { directory, report: text => reports.push(text), assertMaster: () => {},
    github: () => fixture.comparison,
    run(binary, args) {
      calls.push({ binary, args });
      if (binary === 'aws' && args.includes('get-object')) {
        const key = args[args.indexOf('--key') + 1];
        if (key === `releases/${fixture.sha}.json`) throw Object.assign(new Error('Absent'), { stderr: 'NoSuchKey' });
        if (options.missingPrevious) throw new Error('Absent previous release');
        const manifest = key === 'releases/latest.json' ? fixture.previous : fixture.archived;
        writeFileSync(args.at(-1), JSON.stringify(manifest));
      }
      if (binary === 'aws' && args.includes('get-login-password')) return 'fixture';
      if (binary === 'aws' && args.includes('describe-images')) {
        const ids = args[args.indexOf('--image-ids') + 1];
        if (ids.startsWith('[') && options.missingDigest) return JSON.stringify({ imageDetails: [] });
        return JSON.stringify({ imageDetails: [{ imageDigest: 'sha256:' + 'b'.repeat(64) }] });
      }
      return '';
    },
  };
  return { ...fixture, directory, calls, reports, dependencies, dispose: () => rmSync(directory, { recursive: true, force: true }) };
}

test('verified runtime-only publication skips Docker and verifies ECR before writing the current-SHA candidate', () => {
  const fixture = publisherFixture();
  try {
    build(fixture.sha, fixture.config, fixture.sequence, fixture.dependencies);
    assert.ok(!fixture.calls.some(call => call.binary === 'docker'));
    assert.ok(fixture.calls.some(call => call.args.includes('describe-images')));
    const candidate = JSON.parse(readFileSync(join(fixture.directory, 'release.json'), 'utf8'));
    assert.equal(candidate.sha, fixture.sha);
    assert.equal(candidate.reusedFromSha, fixture.previous.sha);
    assert.deepEqual(candidate.images, fixture.previous.images);
  } finally { fixture.dispose(); }
});

for (const options of [{ missingPrevious: true }, { missingDigest: true }, { paths: ['shared/backend/Program.cs'] }]) {
  test(`unproven image reuse falls back to all twenty-one builds: ${JSON.stringify(options)}`, () => {
    const fixture = publisherFixture(options);
    try {
      build(fixture.sha, fixture.config, fixture.sequence, fixture.dependencies);
      assert.equal(fixture.calls.filter(call => call.binary === 'docker' && call.args[0] === 'push').length, 21);
      const candidate = JSON.parse(readFileSync(join(fixture.directory, 'release.json'), 'utf8'));
      assert.equal(candidate.imageSourceSha, fixture.sha);
      assert.equal(candidate.reusedFromSha, undefined);
      assert.equal(Object.keys(candidate.images).length, 21);
    } finally { fixture.dispose(); }
  });
}

test('a superseded release cannot publish a reused candidate or start Docker', () => {
  const fixture = publisherFixture();
  let checks = 0;
  fixture.dependencies.assertMaster = () => { if (++checks > 1) throw new Error('Superseded release'); };
  try {
    assert.throws(() => build(fixture.sha, fixture.config, fixture.sequence, fixture.dependencies), /Superseded/);
    assert.ok(!fixture.calls.some(call => call.binary === 'docker'));
  } finally { fixture.dispose(); }
});