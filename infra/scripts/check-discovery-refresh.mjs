import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { copyFileSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../../', import.meta.url));
const directory = mkdtempSync(join(tmpdir(), 'languagewise-discovery-proof-'));
const terraform = process.env.TERRAFORM_BINARY || 'terraform';
const options = {
  encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'], timeout: 120_000, maxBuffer: 8 * 1024 * 1024,
  env: { ...process.env, AWS_ACCESS_KEY_ID: 'fixture', AWS_SECRET_ACCESS_KEY: 'fixture', AWS_SESSION_TOKEN: '',
    AWS_PROFILE: '', AWS_EC2_METADATA_DISABLED: 'true', AWS_CONFIG_FILE: join(directory, 'no-config'),
    AWS_SHARED_CREDENTIALS_FILE: join(directory, 'no-credentials'), TF_IN_AUTOMATION: 'true', TF_INPUT: 'false' },
};
const run = args => execFileSync(terraform, [`-chdir=${directory}`, ...args], options);

try {
  copyFileSync(resolve(root, 'infra/tests/fixtures/discovery-refresh/main.tf'), join(directory, 'main.tf'));
  copyFileSync(resolve(root, 'infra/demo/.terraform.lock.hcl'), join(directory, '.terraform.lock.hcl'));
  run(['init', '-backend=false', '-input=false', '-lockfile=readonly',
    `-plugin-dir=${resolve(root, 'infra/demo/.terraform/providers')}`]);
  const attributes = {
    id: 'srv-fixture', arn: 'arn:aws:servicediscovery:ap-southeast-2:123456789012:service/srv-fixture',
    region: 'ap-southeast-2', name: 'fixture', namespace_id: 'ns-fixture', type: 'DNS_HTTP',
    description: '', force_destroy: false,
    dns_config: [{ namespace_id: 'ns-fixture', routing_policy: 'MULTIVALUE', dns_records: [{ ttl: 10, type: 'A' }] }],
    health_check_config: [], health_check_custom_config: [], tags: {}, tags_all: {},
  };
  writeFileSync(join(directory, 'terraform.tfstate'), JSON.stringify({
    version: 4, terraform_version: '1.16.6', serial: 1, lineage: 'discovery-offline-fixture', outputs: {},
    resources: [{ mode: 'managed', type: 'aws_service_discovery_service', name: 'app',
      provider: 'provider["registry.terraform.io/hashicorp/aws"]',
      instances: [{ schema_version: 0, attributes, sensitive_attributes: [] }] }],
  }));
  for (const empty of [true, false]) {
    const plan = join(directory, `${empty}.tfplan`);
    run(['plan', '-refresh=false', '-input=false', '-lock=false', `-var=empty_custom_health=${empty}`, `-out=${plan}`]);
    const output = JSON.parse(run(['show', '-json', plan]));
    const change = output.resource_changes.find(resource => resource.type === 'aws_service_discovery_service').change;
    assert.deepEqual(change.actions, empty ? ['delete', 'create'] : ['no-op']);
    assert.deepEqual(change.replace_paths ?? [], empty ? [['health_check_custom_config']] : []);
  }
  console.log('Pinned-provider discovery regression passed: empty block replaces; omitted block is no-op. No AWS API calls.');
} catch {
  console.error('Offline discovery regression failed; raw Terraform output suppressed. Initialize infra/demo with the locked provider first.');
  process.exitCode = 1;
} finally {
  rmSync(directory, { recursive: true, force: true });
}