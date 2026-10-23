import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { test } from 'node:test';

const read = (path) => readFileSync(new URL(path, import.meta.url), 'utf8');
const main = read('./main.tf');
const identity = read('./identity.tf');
const permissions = read('./permissions.tf');
const cleanup = read('./cleanup.tf');
const outputs = read('./outputs.tf');
const versions = read('./versions.tf');
const variables = read('./variables.tf');
const bootstrap = read('../bootstrap/main.tf');
const demoVariables = read('../demo/variables.tf');
const demoServices = read('../demo/services.tf');
const demoNetwork = read('../demo/network.tf');
const demoDatabase = read('../demo/database.tf');

const statement = sid => permissions.split(`Sid`).slice(1)
  .find(block => new RegExp(`^\\s*= "${sid}"`).test(block)).split(/\r?\n    \},?/)[0];

test('discovery preserves the existing absent custom-health configuration on reapply', () => {
  const discovery = demoServices.split('resource "aws_service_discovery_service" "app" {')[1]
    .split('resource "aws_ecs_task_definition"')[0];
  assert.doesNotMatch(discovery, /health_check_custom_config|force_destroy|ignore_changes/);
  assert.match(discovery, /routing_policy = "MULTIVALUE"/);
  assert.match(discovery, /type = "A"/);
});

test('SMTP credentials are metadata-only and injected only into the notifications backend', () => {
  const consumers = main.split('secret_consumers = {')[1].split('database_secrets =')[0];
  const smtpConsumers = [...consumers.matchAll(/^\s*([a-z-]+)\s*=\s*(\[[^\n]+\])/gm)]
    .filter(match => JSON.parse(match[2]).includes('smtp-credentials')).map(match => match[1]);
  assert.deepEqual(smtpConsumers, ['quests-achievements-notifications-service-backend']);
  assert.match(main, /secret_names\s*=.*"smtp-credentials"/);
  assert.doesNotMatch(main, /aws_secretsmanager_secret_version/);
  const secrets = demoServices.split('service_secrets =')[1];
  assert.match(secrets, /name == "quests-achievements-notifications-service-backend" \? \{\s*Smtp__Username/);
  for (const [setting, field] of [['Username', 'username'], ['Password', 'password']]) {
    assert.match(secrets, new RegExp(`Smtp__${setting}\\s*=\\s*"\\$\\{var\\.foundation\\.secret_arns\\["smtp-credentials"\\]\\}:${field}::"`));
  }
  assert.doesNotMatch(secrets, /Smtp__(Host|Port)\s*=/);
  assert.match(identity, /Resource = \[for name in local\.secret_consumers\[each\.key\] : aws_secretsmanager_secret\.application\[name\]\.arn\]/);
});

test('EC2 creation authorizes new tagged resources separately from tagged parents', () => {
  const creation = statement('CreateTaggedNetwork');
  assert.match(creation, /Condition = local\.demo_creation/);
  assert.doesNotMatch(creation, /Resource\s*= "\*"/);
  assert.match(creation, /"vpc", "subnet", "internet-gateway", "route-table", "security-group"/);
  const parent = statement('CreateWithinTaggedVpc');
  assert.match(parent, /Condition = local\.demo_tagged/);
  assert.match(parent, /:vpc\/\*/);
  for (const action of ['CreateSubnet', 'CreateRouteTable', 'CreateSecurityGroup']) assert.ok(parent.includes(`ec2:${action}`));
  const group = statement('AuthorizeTaggedSecurityGroup');
  const rule = statement('CreateTaggedSecurityGroupRule');
  assert.match(group, /Condition = local\.demo_tagged/);
  assert.match(group, /:security-group\/\*/);
  assert.match(rule, /Condition = local\.demo_creation/);
  assert.match(rule, /:security-group-rule\/\*/);
  for (const action of ['AuthorizeSecurityGroupIngress', 'AuthorizeSecurityGroupEgress']) {
    assert.ok(group.includes(`ec2:${action}`));
    assert.ok(rule.includes(`ec2:${action}`));
    assert.ok(statement('TagNetworkAtCreation').includes(`"${action}"`));
    assert.ok(!statement('MutateTaggedNetwork').includes(`ec2:${action}`));
  }
});

test('CloudFront managed prefix-list entries have a regional AWS-owned read grant', () => {
  assert.match(demoNetwork, /data "aws_ec2_managed_prefix_list" "cloudfront"/);
  const grant = statement('AwsManagedPrefixListEntries');
  assert.match(grant, /Effect = "Allow", Condition = local\.regional/);
  assert.match(grant, /Action\s*= \["ec2:GetManagedPrefixListEntries"\]/);
  assert.match(grant, /Resource\s*= "arn:aws:ec2:\$\{local\.region\}:aws:prefix-list\/\*"/);
  assert.doesNotMatch(grant, /Resource\s*= "\*"|ec2:\*|ModifyManagedPrefixList|CreateManagedPrefixList/);
});

test('VPC security-group enumeration is restricted to owned demo VPCs in the configured region', () => {
  const grant = statement('DemoVpcSecurityGroupRead');
  assert.match(grant, /Action\s*= \["ec2:GetSecurityGroupsForVpc"\]/);
  assert.match(grant, /Condition = local\.demo_tagged/);
  assert.match(grant, /Resource\s*= "arn:aws:ec2:\$\{local\.region\}:\$\{local\.account_id\}:vpc\/\*"/);
  assert.doesNotMatch(grant, /Resource\s*= "\*"|ec2:\*|ec2:(Create|Modify|Delete)/);
});

test('Cloud Map private DNS dependencies require global-endpoint service delegation rather than direct Route 53 access', () => {
  const delegation = permissions.split('discovery_dns_delegation = {')[1].split('demo_tagged =')[0];
  assert.match(delegation, /"aws:RequestedRegion" = "us-east-1"/);
  assert.match(delegation, /"ForAnyValue:StringEquals"\s*= \{ "aws:CalledVia" = \["servicediscovery\.amazonaws\.com"\] \}/);
  assert.doesNotMatch(delegation, /IfExists|\bNull\b/);
  for (const sid of ['DiscoveryDnsZoneCreate', 'DiscoveryDnsZoneLifecycle', 'DiscoveryDnsZoneLookup']) {
    assert.match(statement(sid), /Condition = local\.discovery_dns_delegation/);
  }
  assert.match(statement('DiscoveryDnsZoneCreate'), /Action\s*= \["route53:CreateHostedZone"\]/);
  assert.match(statement('DiscoveryDnsZoneLookup'), /Action\s*= \["route53:ListHostedZonesByName"\]/);
  const lifecycle = statement('DiscoveryDnsZoneLifecycle');
  assert.match(lifecycle, /Resource\s*= "arn:aws:route53:::hostedzone\/\*"/);
  for (const action of ['GetHostedZone', 'DeleteHostedZone', 'ChangeResourceRecordSets']) assert.ok(lifecycle.includes(`route53:${action}`));
  assert.doesNotMatch(lifecycle, /route53:\*|HealthCheck/);
});

test('Cloud Map tagging requires demo request tags with supported wildcard scope and excludes cleanup', () => {
  const grant = statement('DiscoveryDemoTagging');
  assert.match(grant, /Action\s*= \["servicediscovery:TagResource"\]/);
  assert.match(grant, /Resource\s*= "\*"/);
  assert.match(grant, /merge\(local\.demo_creation/);
  assert.doesNotMatch(grant, /aws:ResourceTag|\bNull\b/);
  assert.match(grant, /"aws:TagKeys" = \["Project", "Layer", "Lifecycle"\]/);
  assert.match(permissions, /!contains\(\["CloudFrontInitialOwnershipTags", "DiscoveryDemoTagging"\], statement\.Sid\)/);
  assert.match(statement('MutateTaggedDiscovery'), /Condition = local\.demo_tagged/);
});

test('foundation retains required service-linked roles without delegating IAM creation to routine workers', () => {
  const roles = identity.split('resource "aws_iam_service_linked_role" "runtime"')[1].split('resource "aws_iam_role"')[0];
  for (const service of ['elasticloadbalancing.amazonaws.com', 'rds.amazonaws.com', 'vpcorigin.cloudfront.amazonaws.com']) {
    assert.ok(roles.includes(`"${service}"`));
  }
  assert.match(roles, /aws_service_name = each\.key/);
  assert.match(roles, /prevent_destroy = true/);
  assert.doesNotMatch(permissions + cleanup, /iam:CreateServiceLinkedRole/);
});

test('CodeBuild worker metadata reads authorize the exact project rather than nonexistent build-scoped access', () => {
  const grant = statement('CleanupBuildIdentity');
  assert.match(grant, /Action = \["codebuild:BatchGetBuilds"\]/);
  assert.match(grant, /Resource\s*= "arn:aws:codebuild:\$\{local\.region\}:\$\{local\.account_id\}:project\/\$\{local\.foundation_contract\.cleanup_project_name\}"/);
  assert.match(grant, /Condition = local\.regional/);
  assert.doesNotMatch(grant, /:build\/|Resource\s*= "\*"/);
});

test('ECS deployment inventory reads stay within regional demo service scope', () => {
  const grant = statement('EcsServices');
  assert.match(grant, /"ecs:ListServiceDeployments"/);
  assert.match(grant, /"ecs:DescribeServiceDeployments"/);
  assert.match(grant, /Condition = local\.regional/);
  assert.match(grant, /Resource\s*= "arn:aws:ecs:\$\{local\.region\}:\$\{local\.account_id\}:service\/languagewise\/\$\{local\.prefix\}-\*"/);
  assert.doesNotMatch(grant, /Resource\s*= "\*"|"ecs:\*"/);
  const deployment = statement('EcsDeploymentRead');
  assert.match(deployment, /Action\s*= \["ecs:DescribeServiceDeployments"\]/);
  assert.match(deployment, /Condition = local\.regional/);
  assert.match(deployment, /Resource\s*= "arn:aws:ecs:\$\{local\.region\}:\$\{local\.account_id\}:service-deployment\/languagewise\/\$\{local\.prefix\}-\*\/\*"/);
  assert.doesNotMatch(deployment, /Resource\s*= "\*"|"ecs:\*"|ecs:StopServiceDeployment/);
});

test('approved free-plan database uses micro while preserving storage and private managed credentials', () => {
  const instance = demoDatabase.split('resource "aws_db_instance" "demo" {')[1].split('resource "aws_ecs_task_definition"')[0];
  assert.match(instance, /instance_class\s*= "db\.t4g\.micro"/);
  assert.match(instance, /engine_version\s*= "18\.6"/);
  assert.match(instance, /allocated_storage\s*= 20/);
  assert.match(instance, /storage_type\s*= "gp3"/);
  assert.match(instance, /storage_encrypted\s*= true/);
  assert.match(instance, /publicly_accessible\s*= false/);
  assert.match(instance, /manage_master_user_password\s*= true/);
});

test('task definitions are prefix-scoped at registration and retained without deregistration rights', () => {
  assert.match(statement('RegionalInventory'), /Resource = "\*"[\s\S]*ecs:DescribeTaskDefinition/);
  assert.match(statement('EcsRegisterDefinitions'), /Condition = local\.demo_creation/);
  assert.match(statement('EcsRegisterDefinitions'), /task-definition\/\$\{local\.prefix\}-\*:\*/);
  assert.doesNotMatch(permissions, /ecs:DeregisterTaskDefinition/);
  for (const source of [demoServices, demoDatabase]) {
    assert.match(source, /resource "aws_ecs_task_definition"[^]*?skip_destroy\s*= true/);
  }
});

test('CloudFront creation uses real IAM actions and KMS access is regional metadata-only', () => {
  const creation = statement('CreateTaggedCloudFront');
  assert.match(creation, /cloudfront:CreateDistribution"/);
  assert.match(creation, /cloudfront:CreateVpcOrigin"/);
  assert.match(creation, /Resource\s*= "\*"/);
  for (const tag of ['Project', 'Layer']) assert.ok(creation.includes(`aws:RequestTag/${tag}`));
  assert.doesNotMatch(permissions, /cloudfront:CreateDistributionWithTags/);
  assert.match(statement('MutateTaggedCloudFront'), /aws:ResourceTag\/Project/);
  const kms = statement('RdsManagedKeyMetadata');
  assert.match(kms, /Condition = local\.regional/);
  assert.match(kms, /arn:aws:kms:\$\{local\.region\}:\$\{local\.account_id\}:key\/\*/);
  assert.deepEqual([...permissions.matchAll(/"(kms:[^"]+)"/g)].map(match => match[1]), ['kms:DescribeKey']);
});

test('flat output names exactly match the script interface', () => {
  const expected = [
    'account_id', 'region', 'cluster_arn', 'repository_url', 'control_table_name',
    'artifact_bucket_name', 'cleanup_project_name', 'cleanup_schedule_name',
    'cleanup_schedule_group_name', 'cleanup_alarm_name', 'deploy_role_arn',
    'control_role_arn', 'service_execution_role_arns', 'service_task_role_arns',
    'secret_arns', 'state_bucket_name', 'demo_state_key', 'release_role_arn', 'cleanup_role_arn',
    'cleanup_dispatcher_arn', 'cleanup_dispatcher_role_arn', 'cleanup_dispatcher_alarm_name',
    'cleanup_dispatcher_throttle_alarm_name', 'cleanup_scheduler_alarm_name', 'cleanup_failure_topic_arn',
  ];
  const actual = [...outputs.matchAll(/output "([^"]+)"/g)].map((match) => match[1]);
  assert.deepEqual(actual.toSorted(), expected.toSorted());
  const documented = read('./README.md').match(/Exact output keys:\s*```text\s*([\s\S]*?)```/)[1]
    .trim().split(/\s+/);
  assert.deepEqual(documented.toSorted(), expected.toSorted());
  for (const name of expected) {
    assert.match(outputs, new RegExp(`value\\s*= local\\.foundation_contract\\.${name}\\b`), name);
  }
  assert.match(cleanup, /value\s*= jsonencode\(local\.foundation_contract\)/);
  assert.match(cleanup, /name\s*= "TF_VAR_foundation"/);
});

test('role keys are precisely Compose minus shared-db plus the initializer', () => {
  const compose = read('../../docker-compose.yml').split(/^services:\s*$/m)[1];
  assert.ok(compose);
  const expected = [...compose.matchAll(/^  ([a-z][a-z0-9-]*):\s*$/gm)]
    .map((match) => match[1])
    .filter((name) => name !== 'shared-db');
  assert.equal(expected.length, 20);
  const serviceBlock = main.match(/services\s*= toset\(\[([\s\S]*?)\]\)/)[1];
  const actual = [...serviceBlock.matchAll(/"([^"]+)"/g)].map((match) => match[1]);
  assert.deepEqual(actual.toSorted(), [...expected, 'database-initializer'].toSorted());
  assert.equal(new Set(actual).size, 21);
  assert.match(identity, /for_each\s*= local\.services/g);
  for (const service of actual) {
    assert.ok(`lw-demo-exec-${service}`.length <= 64, service);
    assert.ok(`lw-demo-task-${service}`.length <= 64, service);
  }
});

test('secret consumers do not leak signing or OpenRouter to other services', () => {
  const block = main.match(/secret_consumers = \{([\s\S]*?)\n  \}/)[1];
  const consumers = Object.fromEntries([...block.matchAll(/([a-z][a-z0-9-]*)\s*= \[([^\]]*)\]/g)]
    .map((match) => [match[1], [...match[2].matchAll(/"([^"]+)"/g)].map((secret) => secret[1])]));
  const using = (secret) => Object.entries(consumers).filter(([, secrets]) => secrets.includes(secret))
    .map(([service]) => service).toSorted();
  assert.deepEqual(using('signing-private-key'), ['shared-backend']);
  assert.deepEqual(using('openrouter-api-key'), [
    'garry-ai-service-backend', 'leaderboard-analytics-service-backend',
    'quests-achievements-notifications-service-backend',
  ]);
  assert.equal(using('signing-public-key').length, 7);
  assert.deepEqual(using('mcp-api-key'), [
    'chat-discussion-service-backend', 'garry-ai-service-backend', 'leaderboard-analytics-service-backend',
    'mcp-server', 'mini-games-service-backend', 'quests-achievements-notifications-service-backend',
    'quizzes-courses-service-backend',
  ]);
  assert.deepEqual(consumers['mcp-server'], ['signing-public-key', 'mcp-api-key']);
  assert.equal(consumers['rag-server'], undefined);
  for (const [service, secrets] of Object.entries(consumers)) {
    assert.ok(!service.endsWith('-frontend'));
    assert.ok(secrets.length > 0);
  }
  assert.deepEqual(using('db-shared'), ['shared-db-api']);
  assert.deepEqual(using('db-mini-games'), ['mini-games-service-db']);
  assert.deepEqual(using('db-quizzes-courses'), ['quizzes-courses-service-db']);
  assert.deepEqual(using('db-chat-discussion'), ['chat-discussion-service-db']);
  assert.deepEqual(using('db-quests'), ['quests-achievements-notifications-service-db']);
});

test('routine permissions contain no secret reads or IAM writes', () => {
  const routine = permissions + cleanup;
  assert.doesNotMatch(routine, /secretsmanager:(GetSecretValue|PutSecretValue|UpdateSecret)/);
  const actionValues = [...routine.matchAll(/\bAction\s*=\s*(?:\[([^\]]*)\]|"([^"]*)")/g)]
    .flatMap((match) => match[2] ? [match[2]] : [...match[1].matchAll(/"([^"]+)"/g)].map((action) => action[1]));
  const iamActions = actionValues.filter((action) => action.startsWith('iam:'));
  assert.deepEqual([...new Set(iamActions)].toSorted(), ['iam:GetRole', 'iam:PassRole']);
  assert.doesNotMatch(routine, /ecr:(Delete|BatchDelete|SetRepositoryPolicy)|scheduler:(Delete|Update)|codebuild:(Update|Delete)/);
  assert.match(permissions, /iam:PassedToService"\s*= "ecs-tasks\.amazonaws\.com"/);
  assert.match(permissions, /for role in aws_iam_role\.execution : role\.arn/);
  assert.match(permissions, /for role in aws_iam_role\.task : role\.arn/);
  assert.match(permissions, /length\(jsonencode\([\s\S]*?<= 6144/);
  assert.match(cleanup, /for_each\s*= aws_iam_policy\.cleanup_runtime/);
  assert.match(permissions, /cleanup_policy_parts\s*=\s*\{/);
  assert.ok(permissions.includes('regex(":(Create|Register|RunTask)", action)'));
});

test('cleanup has real mandatory source inputs, pinned source, and verified toolchain', () => {
  for (const name of ['cleanup_bundle_key', 'cleanup_bundle_version', 'terraform_sha256']) {
    const block = variables.match(new RegExp(`variable "${name}" \\{([\\s\\S]*?)(?=\\nvariable |$)`))[1];
    assert.doesNotMatch(block, /\bdefault\s*=/);
  }
  assert.match(cleanup, /source_version\s*= var\.cleanup_bundle_version/);
  assert.match(cleanup, /"s3:VersionId" = var\.cleanup_bundle_version/);
  assert.match(cleanup, /sha256sum --check --strict/);
  assert.match(cleanup, /node infra\/scripts\/demo\.mjs cleanup/);
  assert.match(cleanup, /nodejs = "22"/);
  assert.match(cleanup, /aws\/codebuild\/standard:7\.0/);
  assert.match(cleanup, /build_timeout\s*= 120/);
  assert.match(cleanup, /rate\(15 minutes\)/);
  assert.match(cleanup, /Action = "codebuild:StartBuild", Resource = aws_codebuild_project\.cleanup\.arn/);
});

test('CodeBuild can inspect only its source object while the download stays version-pinned', () => {
  const policy = cleanup.split('resource "aws_iam_role_policy" "cleanup"')[1]
    .split('resource "aws_cloudwatch_log_group" "cleanup"')[0];
  const current = policy.split('Sid       = "CleanupSourceCurrentObjectRead"')[1].split('\n      },')[0];
  const pinned = policy.split('Sid       = "CleanupSourcePinnedVersionRead"')[1].split('\n      },')[0];
  for (const grant of [current, pinned]) {
    assert.match(grant, /Resource\s*= "\$\{data\.aws_s3_bucket\.artifacts\.arn\}\/\$\{var\.cleanup_bundle_key\}"/);
    assert.doesNotMatch(grant, /Resource\s*= "\*"|s3:PutObject|s3:DeleteObject/);
  }
  assert.match(current, /Action = \["s3:GetObject"\]/);
  assert.match(current, /Condition = local\.regional/);
  assert.doesNotMatch(current, /s3:VersionId/);
  assert.match(pinned, /Action = \["s3:GetObjectVersion"\]/);
  assert.match(pinned, /"s3:VersionId" = var\.cleanup_bundle_version/);
  assert.match(cleanup, /source_version\s*= var\.cleanup_bundle_version/);
});

test('retained state and application values stay outside disposable control', () => {
  assert.match(bootstrap, /prevent_destroy = true/);
  assert.match(bootstrap, /status = "Enabled"/);
  assert.match(bootstrap, /sse_algorithm = "AES256"/);
  assert.match(bootstrap, /aws:SecureTransport/);
  assert.match(versions, /key\s*= "foundation\/terraform\.tfstate"/);
  assert.match(versions, /use_lockfile\s*= true/);
  assert.match(permissions, /NeverAccessAdministrativeState/);
  assert.match(permissions, /\$\{local\.state_arn\}\/foundation\/\*/);
  assert.doesNotMatch(main + identity + permissions + cleanup, /resource "aws_(iam_access_key|secretsmanager_secret_version|dynamodb_table_item)"/);
  assert.match(main, /billing_mode\s*= "PAY_PER_REQUEST"/);
  assert.match(main, /hash_key\s*= "id"/);
  assert.match(main, /point_in_time_recovery\s*\{\s*enabled = false\s*\}/);
  assert.match(main, /deletion_protection_enabled\s*= true/);
});

test('bootstrap exclusively owns protected state and artifact buckets before foundation setup', () => {
  const foundation = main + identity + permissions + cleanup + outputs + versions + variables;
  assert.deepEqual([...bootstrap.matchAll(/resource "aws_s3_bucket" "([^"]+)"/g)]
    .map((match) => match[1]).toSorted(), ['artifacts', 'state']);
  assert.doesNotMatch(bootstrap, /\bdata "aws_/);
  assert.match(bootstrap, /bucket\s*= "languagewise-047754151573-ap-southeast-2-artifacts"/);
  for (const name of ['state', 'artifacts']) {
    const bucket = bootstrap.split(`resource "aws_s3_bucket" "${name}"`)[1].split('\nresource ')[0];
    assert.match(bucket, /force_destroy\s*= false/);
    assert.match(bucket, /prevent_destroy = true/);
    const versioning = bootstrap.split(`resource "aws_s3_bucket_versioning" "${name}"`)[1].split('\nresource ')[0];
    assert.match(versioning, /status = "Enabled"/);
    const encryption = bootstrap.split(`resource "aws_s3_bucket_server_side_encryption_configuration" "${name}"`)[1].split('\nresource ')[0];
    assert.match(encryption, /sse_algorithm = "AES256"/);
    const publicBlock = bootstrap.split(`resource "aws_s3_bucket_public_access_block" "${name}"`)[1].split('\nresource ')[0];
    for (const setting of ['block_public_acls', 'block_public_policy', 'ignore_public_acls', 'restrict_public_buckets']) {
      assert.match(publicBlock, new RegExp(`${setting}\\s*= true`));
    }
    const policy = bootstrap.split(`resource "aws_s3_bucket_policy" "${name}"`)[1].split('\nresource ')[0];
    assert.match(policy, /Effect = "Deny"/);
    assert.match(policy, /"aws:SecureTransport" = "false"/);
    const lifecycle = bootstrap.split(`resource "aws_s3_bucket_lifecycle_configuration" "${name}"`)[1].split('\nresource ')[0];
    assert.match(lifecycle, /days_after_initiation = 1/);
    assert.match(lifecycle, new RegExp(`depends_on = \\[aws_s3_bucket_versioning\\.${name}\\]`));
    if (name === 'state') assert.match(lifecycle, /noncurrent_days = 30/);
    else assert.doesNotMatch(lifecycle, /\b(?:expiration|noncurrent_version_expiration)\s*\{/);
  }
  assert.match(bootstrap, /output "artifact_bucket_name"\s*\{\s*value = aws_s3_bucket\.artifacts\.id/);
  assert.match(main, /data "aws_s3_bucket" "artifacts"\s*\{\s*bucket = "languagewise-\$\{local.account_id\}-\$\{local.region\}-artifacts"/);
  assert.doesNotMatch(foundation, /resource "aws_s3_bucket[^\"]*" "artifacts"/);
  assert.doesNotMatch(foundation, /(?<!data\.)aws_s3_bucket\.artifacts\b|aws_s3_bucket_versioning\.artifacts/);
  assert.match(outputs, /artifact_bucket_name\s*= data\.aws_s3_bucket\.artifacts\.id/);
  assert.match(cleanup, /location = "\$\{data\.aws_s3_bucket\.artifacts\.id\}\/\$\{var.cleanup_bundle_key\}"/);
  assert.match(permissions, /\$\{data\.aws_s3_bucket\.artifacts\.arn\}\/releases\/\*/);
});

test('ECR automatically expires only untagged images without counting shared service tags', () => {
  const lifecycle = main.split('resource "aws_ecr_lifecycle_policy" "existing"')[1].split('\nimport ')[0];
  assert.match(lifecycle, /repository = aws_ecr_repository\.existing\.name/);
  assert.match(lifecycle, /tagStatus\s*= "untagged"/);
  assert.match(lifecycle, /countType\s*= "sinceImagePushed"/);
  assert.match(lifecycle, /countUnit\s*= "days"/);
  assert.match(lifecycle, /countNumber = 1/);
  assert.match(lifecycle, /action = \{ type = "expire" \}/);
  assert.equal([...lifecycle.matchAll(/rulePriority\s*=/g)].length, 1);
  assert.doesNotMatch(lifecycle, /imageCountMoreThan|tagPrefixList|tagPatternList|tagStatus\s*= "(?:any|tagged)"/);
});

test('demo provider tags satisfy foundation IAM and preserve residual inventory tags', () => {
  const tags = demoVariables.split('default_tags {')[1];
  for (const [key, value] of [['Project', 'languagewise'], ['Layer', 'demo']]) {
    assert.match(tags, new RegExp(`${key}\\s*= "${value}"`));
    for (const scope of ['RequestTag', 'ResourceTag']) {
      assert.match(permissions, new RegExp(`"aws:${scope}/${key}"\\s*= "${value}"`));
    }
  }
  assert.match(tags, /Lifecycle\s*= "disposable-demo"/);
});

test('shared backend trusts forwarded headers only from the configured demo network', () => {
  const network = demoNetwork.match(/resource "aws_vpc" "demo"\s*\{\s*cidr_block\s*= "([^"]+)"/)[1];
  assert.equal(network, '10.82.0.0/16');
  const backend = demoServices.split('shared-backend = {')[1].split('\n      }')[0];
  assert.equal(backend.match(/ReverseProxy__TrustedNetwork\s*= "([^"]+)"/)[1], network);
  for (const key of ['Services__Database', 'Services__Achievements', 'Services__Garry']) {
    assert.match(backend, new RegExp(`${key}\\s*= local\\.endpoint\\[`));
  }
  assert.equal([...demoServices.matchAll(/ReverseProxy__TrustedNetwork/g)].length, 1);
});

test('retained dispatcher has only exact control-read, cleanup-start and scoped logging grants', () => {
  assert.match(versions, /source\s*= "hashicorp\/archive"[\s\S]*?version\s*= "2\.7\.1"/);
  assert.match(cleanup, /resource "aws_lambda_function" "dispatcher"[\s\S]*?runtime\s*= "nodejs22.x"/);
  assert.match(cleanup, /filename\s*= data\.archive_file\.expiry\.output_path/);
  assert.match(cleanup, /file\("\$\{path.module\}\/\.\.\/scripts\/expiry.mjs"\)/);
  assert.match(cleanup, /arn\s*= aws_lambda_function\.dispatcher\.arn/);
  assert.doesNotMatch(cleanup, /aws-sdk:codebuild:startBuild/);
  const policy = cleanup.split('resource "aws_iam_role_policy" "dispatcher"')[1].split('resource "aws_lambda_function"')[0];
  assert.match(policy, /dynamodb:GetItem/);
  assert.match(policy, /dynamodb:LeadingKeys/);
  assert.match(policy, /codebuild:StartBuild/);
  assert.doesNotMatch(policy, /PutItem|UpdateItem|DeleteItem|secretsmanager:|s3:|iam:|Resource\s*= "\*"/);
  assert.match(cleanup, /metric_name\s*= each.key/);
  assert.match(cleanup, /Errors\s*= local.foundation_contract.cleanup_dispatcher_alarm_name/);
  assert.match(cleanup, /Throttles\s*= local.foundation_contract.cleanup_dispatcher_throttle_alarm_name/);
  assert.match(identity, /max_session_duration\s*= 7200/);
  assert.doesNotMatch(identity, /14400/);
  const contract = outputs.split('output "account_id"')[0];
  assert.doesNotMatch(contract, /aws_lambda_function|aws_codebuild_project|aws_iam_role\.cleanup|aws_iam_role\.dispatcher/);
});

test('CloudFront initial tagging cannot take over differently owned resources or reach cleanup', () => {
  const grant = permissions.split('Sid      = "CloudFrontInitialOwnershipTags"')[1].split('Sid       = "MutateTaggedCloudFront"')[0];
  assert.match(grant, /cloudfront:TagResource/);
  assert.match(grant, /"aws:RequestTag\/Project" = "languagewise"/);
  assert.match(grant, /"aws:ResourceTag\/Project" = "true"/);
  assert.match(grant, /"aws:ResourceTag\/Layer" = "true"/);
  assert.match(grant, /"aws:TagKeys" = \["Project", "Layer", "Lifecycle"\]/);
  assert.match(permissions, /!contains\(\["CloudFrontInitialOwnershipTags", "DiscoveryDemoTagging"\], statement\.Sid\)/);
  assert.match(permissions, /Action = \["s3:GetBucketLocation", "s3:ListBucket"\]/);
});

test('missing native lock is distinguishable from denied access without widening state object rights', () => {
  const grant = permissions.split('Sid       = "DemoStateList"')[1].split('Sid       = "StateLocation"')[0];
  assert.match(grant, /s3:ListBucket/);
  assert.match(grant, /Condition = local.regional/);
  assert.doesNotMatch(grant, /s3:prefix/);
  assert.match(permissions, /Resource = local.demo_objects/);
  assert.match(permissions, /NeverAccessAdministrativeState/);
});