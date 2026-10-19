# Disposable Demo Runtime

This root owns only disposable networking, RDS, discovery, logs, the internal
gateway, CloudFront, task definitions and application services. It has no IAM
resources, secret versions or secret-value data sources. The foundation is
retained and supplied as nonsecret values, not remote-state secret lookups.

## Inputs and State

- Terraform `~> 1.16.0`, AWS `~> 6.0`; the provider lock file is authoritative.
- Initialize the S3 backend in trusted automation with `bucket`, `key` and
  `region` from `foundation.state_bucket_name`, `foundation.demo_state_key` and
  `foundation.region`. Native S3 locking and encryption are enabled in the root.
- `foundation`: `account_id`, `region`, `cluster_arn`, `repository_url`,
  `state_bucket_name`, `demo_state_key`, `control_table_name`,
  `artifact_bucket_name`, `cleanup_project_name`, deployment/control role ARNs,
  `service_execution_role_arns`, `service_task_role_arns` and `secret_arns`.
  The deployment/control roles and retained control identifiers belong to the
  controller; this root does not assume or modify those roles.
- Both service role maps contain every Compose name except `shared-db`, plus
  `database-initializer`. Supply a minimal task role even for containers that do
  not use AWS APIs directly.
- `secret_arns` contains `db-shared`, `db-mini-games`, `db-quizzes-courses`,
  `db-chat-discussion`, `db-quests`, `openrouter-api-key`, `signing-private-key`
  and `signing-public-key`. These are metadata ARNs only.
- `release` contains the tested 40-character `sha` and exactly nineteen
  `images`, keyed by those same eighteen Compose names plus
  `database-initializer`. All images must be immutable digests in the single
  `foundation.repository_url`. Do not include `shared-db`, MCP or RAG.

## Two Phases

1. Under the retained controller/state locks, apply with
   `services_enabled=false` (the default). This creates all infrastructure,
   eighteen application task definitions and the initializer definition, but
   **no ECS application services**.
2. Start one Fargate task using `initializer_task_definition_arn`, the foundation
   cluster, `subnet_ids`, `initializer_security_group_ids`, public IP assignment
   and Linux/x86_64 platform 1.4.0. Await STOPPED; require no launch failures and
   exit code zero from container `database-initializer`. A successful RunTask
   API response is not evidence of successful initialization.
3. Only then apply the same release with `services_enabled=true`. Terraform
   creates eighteen one-replica services and waits for steady state. The ALB
   detaches its initial CloudFront managed-prefix-list SG and retains only the
   SG admitting CloudFront's service-managed VPC-origin SG.
4. Repeated Up or a live release must not perform phase one or rerun the
   initializer. The controller owns this gate, expiry and serialization.
5. Down destroys this entire root, including RDS data and backups, with no final
   snapshot. It does not touch foundation resources or secret metadata/values.

The initializer's master JSON is injected by ECS directly from the RDS-managed
secret into `RDS_MASTER_SECRET`. The controller must not fetch it, pass it as a
Terraform variable, or add a plaintext RunTask environment override. Runtime
configuration is `RDS_HOST`, `AWS_DEFAULT_REGION`, and `DATABASE_SECRET_ARNS`
(a JSON map of the five database secret keys to their foundation ARNs).

## Foundation Permissions

- Application execution roles: image pull, their log stream and only the
  specific injected secrets. Only shared authentication receives the signing
  private key; other backends receive the verification key. Only Garry, quests
  and leaderboard receive OpenRouter.
- Initializer execution role: image pull, its logs and `GetSecretValue` for the
  RDS-managed master secret. Its ARN is created at runtime, so the foundation
  must authorize the narrowly scoped RDS-managed-secret resource pattern or an
  appropriate resource-tag boundary without reading its value.
- Initializer task role: `secretsmanager:PutSecretValue` on exactly the five
  retained database secret ARNs. It needs no application-secret read access.
- Deploy/control roles: runtime resource lifecycle plus metadata discovery of
  the CloudFront managed prefix list and the VPC-scoped
  `CloudFront-VPCOrigins-Service-SG`, and PassRole only for the supplied ECS
  roles. CloudFront's first-use service-linked-role prerequisite must be
  satisfied by foundation administration, not IAM resources in this root.

## Application and Image Interfaces

Build contracts are in `../images/README.md`. All five database client images
must contain the RDS global CA bundle at
`/etc/ssl/certs/rds-global-bundle.pem`. The initializer emits secret JSON with
`connection`, `uri` and `password`; the four .NET APIs receive only `connection`
as `Database__ConnectionString`, and PostgREST receives only `uri` as
`PGRST_DB_URI`. Connections verify the RDS hostname and CA, and .NET pools are
bounded to ten connections per API.

The current app-side changes are outside this root's ownership:

- nginx must consume `DNS_RESOLVER=169.254.169.253`,
  `DNS_SUFFIX=.languagewise.internal`, and shared gateway
  `FORWARDED_SCHEME=https`. Preserve runtime upstream DNS re-resolution.
  The original nginx configuration hard-codes Docker DNS, unsuffixed names and
  `$scheme`; passing these environment values alone does not fix it.
- Authentication must consume `Auth__SigningKeyPem` and
  `Auth__VerificationKeyPem` while preserving local file fallback.
- Honor `Mcp__Enabled=false` and `Rag__Enabled=false`; especially shared's RAG
  path must return unavailable without attempting the host-local default.

All HTTP service endpoints use Compose-label names under
`languagewise.internal`; no host-address endpoint is configured. Ingress is
restricted to actual dependency SGs. Only CloudFront is browser-facing; the
ALB and RDS are private. Tasks have public egress IPs but no public ingress.

The initializer creates five restricted owner roles/databases using RDS master
CREATEROLE/CREATEDB privileges, explicitly grants the master SET/ADMIN membership,
and runs schema/seed transactions as each owner. `web_anon` is a NOLOGIN role
that quests can SET ROLE into. No local hard-coded bootstrap password is used.
SCRAM verifiers, not plaintext passwords, enter SQL statements; secret JSON
goes to AWS CLI only through stdin. Subprocess diagnostics are suppressed.

An admin-owned marker commits with each seed. Credentials are marked published
only after Secrets Manager acknowledges the write. A failed publication can be
retried without reseeding. A completed owner is not reseeded or rekeyed; changed
secret ARNs or unmarked pre-existing tables fail closed. This is a fresh-demo
bootstrap, not an automatic migration engine for arbitrary live schemas.
The quiz marker and identity sequences match the API initializer's conventions,
so normal API startup does not repeat its one-time progress seed.

## Offline Verification

From this directory: `terraform fmt -check -recursive`,
`terraform init -backend=false -input=false`, `terraform validate`, and
`terraform test`. Tests use a mocked AWS provider and synthetic identifiers;
they never contact AWS. From the repository root, run
`python -B infra/scripts/initialize-databases.tests.py` with the selected Python.

These checks do not prove RDS privileged-role behavior, image builds, IAM,
CloudFront first-use provisioning, service startup, TLS handshakes, or browser
flows in AWS. Deployment rehearsal must verify those gates, including HTTPS
cookies and streamed responses (CloudFront origin idle timeout is 60 seconds;
nginx and ALB are configured separately). No local apply is part of validation.
