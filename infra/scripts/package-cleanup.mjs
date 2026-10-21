import { execFileSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { mkdirSync, readFileSync, renameSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { requireThat } from './control.mjs';

export function allowedSource(path) {
  return /^infra\/demo\/[^/]+\.(tf|hcl)$/.test(path)
    || path === 'infra/demo/.terraform.lock.hcl'
    || ['infra/scripts/demo.mjs', 'infra/scripts/control.mjs'].includes(path);
}

export function main() {
  const sha = execFileSync('git', ['rev-parse', 'HEAD'], { encoding: 'utf8' }).trim();
  const tracked = execFileSync('git', ['ls-tree', '-r', '--name-only', sha, 'infra/demo', 'infra/scripts'], { encoding: 'utf8' })
    .trim().split(/\r?\n/).filter(allowedSource);
  requireThat(tracked.includes('infra/scripts/demo.mjs') && tracked.includes('infra/scripts/control.mjs')
    && tracked.includes('infra/demo/.terraform.lock.hcl'), 'Commit/review implementation before packaging its trusted cleanup source');
  execFileSync('git', ['diff', '--exit-code', sha, '--', ...tracked], { stdio: 'pipe' });
  mkdirSync('infra/generated', { recursive: true });
  const temporary = 'infra/generated/cleanup.zip';
  execFileSync('git', ['archive', '--format=zip', `--output=${temporary}`, sha, ...tracked]);
  const hash = createHash('sha256').update(readFileSync(temporary)).digest('hex');
  const path = `infra/generated/${hash}.zip`;
  renameSync(temporary, path);
  const result = { commit: sha, path, cleanup_bundle_key: `cleanup/${hash}.zip` };
  writeFileSync('infra/generated/cleanup-package.json', JSON.stringify(result, null, 2));
  console.log(JSON.stringify(result, null, 2));
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  try { main(); } catch { console.error('Cleanup packaging failed: commit/review source and remove changes to packaged paths first.'); process.exitCode = 1; }
}