import assert from 'node:assert/strict';
import { execFileSync, spawn } from 'node:child_process';
import { randomUUID } from 'node:crypto';

const image = process.argv[2];
assert.match(image ?? '', /^[a-zA-Z0-9][a-zA-Z0-9._/:-]+$/);
const name = `lw-rag-measure-${randomUUID()}`;
const run = args => execFileSync('docker', args, {
  encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'], timeout: 30_000, maxBuffer: 1024 * 1024,
});
let events;
let timer;
let phase = 'create';

try {
  run(['create', '--name', name, '--label', 'languagewise.measurement=true', '--cpus', '0.5',
    '--memory', '1024m', '--memory-swap', '1024m', '--network', 'none',
    '--health-interval', '2s', '--health-start-period', '300s', '--health-retries', '3', image]);
  const started = Date.now();
  phase = 'cold-start';
  run(['start', name]);
  await new Promise((resolve, reject) => {
    events = spawn('docker', ['events', '--since', String(Math.floor(started / 1000)),
      '--filter', `container=${name}`, '--filter', 'event=health_status', '--format', '{{.Action}}'],
    { stdio: ['ignore', 'pipe', 'pipe'] });
    let buffer = '';
    events.stdout.on('data', chunk => {
      buffer = (buffer + chunk.toString()).slice(-4096);
      if (buffer.includes('health_status: healthy')) resolve();
      if (buffer.includes('health_status: unhealthy')) reject(new Error('RAG became unhealthy'));
    });
    events.on('error', reject);
    events.on('exit', () => reject(new Error('Health event stream stopped')));
    timer = setTimeout(() => reject(new Error('RAG cold-start deadline exceeded')), 360_000);
  });
  clearTimeout(timer);
  events.kill();
  const coldStartMilliseconds = Date.now() - started;
  const queryMilliseconds = [];
  phase = 'retrieval';
  for (let attempt = 0; attempt < 5; attempt++) {
    const result = JSON.parse(run(['exec', name, 'python', '-c',
      "import json,time,urllib.request; start=time.monotonic(); request=urllib.request.Request('http://127.0.0.1:8100/query', data=json.dumps({'query':'How do I earn achievements and complete quests?','n_results':3}).encode(), headers={'Content-Type':'application/json'}); result=json.load(urllib.request.urlopen(request, timeout=10)); print(json.dumps({'milliseconds':round((time.monotonic()-start)*1000),'count':result['resultCount'],'confidence':result['confidence']}))"]));
    assert.ok(result.count > 0 && result.confidence !== 'insufficient');
    assert.ok(result.milliseconds < 10_000);
    queryMilliseconds.push(result.milliseconds);
  }
  phase = 'resource-capture';
  const peakMemoryBytes = Number(run(['exec', name, 'python', '-c',
    "from pathlib import Path; print(Path('/sys/fs/cgroup/memory.peak').read_text().strip())"]).trim());
  const serverPeakRssBytes = Number(run(['exec', name, 'python', '-c',
    "from pathlib import Path; print(int(next(line for line in Path('/proc/1/status').read_text().splitlines() if line.startswith('VmHWM:')).split()[1])*1024)"]).trim());
  const disk = run(['exec', name, 'du', '-sk', '/data', '/home/app/.cache/chroma']).trim().split('\n')
    .map(line => Number(line.trim().split(/\s+/)[0]) * 1024);
  const state = JSON.parse(run(['inspect', '--format', '{{json .State}}', name]));
  phase = 'resource-validation';
  console.log(JSON.stringify({ coldStartMilliseconds, peakMemoryBytes, queryMilliseconds, oomKilled: state.OOMKilled }));
  assert.equal(state.OOMKilled, false);
  assert.equal(state.Running, true);
  assert.ok(Number.isSafeInteger(peakMemoryBytes) && peakMemoryBytes > 0 && peakMemoryBytes < 1024 ** 3);
  assert.ok(coldStartMilliseconds < 300_000);
  assert.notEqual(run(['exec', name, 'id', '-u']).trim(), '0');
  phase = 'image-size';
  const imageBytes = Number(run(['image', 'inspect', '--format', '{{.Size}}', image]).trim());
  console.log(JSON.stringify({ cpu: 0.5, memoryLimitBytes: 1024 ** 3, network: 'none',
    coldStartMilliseconds, peakMemoryBytes, serverPeakRssBytes, queryMilliseconds, indexBytes: disk[0], modelCacheBytes: disk[1], imageBytes,
    oomKilled: state.OOMKilled, nonRoot: true, persistentVolumes: false }, null, 2));
} catch {
  console.error(`Isolated 0.5 CPU / 1 GiB RAG measurement failed during ${phase}; no resource limit was raised. Raw output suppressed.`);
  process.exitCode = 1;
} finally {
  clearTimeout(timer);
  events?.kill();
  try { run(['rm', '--force', name]); }
  catch { console.error(`Measurement container cleanup requires attention: ${name}`); process.exitCode = 1; }
}