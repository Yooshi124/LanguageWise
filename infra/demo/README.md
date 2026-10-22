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
- Both service role maps contain every Compose name except `shared-db`,
  plus `database-initializer`. Supply a minimal task role even for containers that do
  not use AWS APIs directly.
- `secret_arns` contains `db-shared`, `db-mini-games`, `db-quizzes-courses`,
  `db-chat-discussion`, `db-quests`, `openrouter-api-key`, `signing-private-key`
  `signing-public-key` and `mcp-api-key`. These are metadata ARNs only.
- `release` contains `schema_version=2`, the tested 40-character `sha` and exactly twenty-one
  `images`, keyed by those same twenty Compose names plus
  `database-initializer`. All images must be immutable digests in the single
  `foundation.repository_url`. Do not include `shared-db`.
- Legacy manifests omit the version and contain nineteen images. The controller
  translates them to `schema_version=1` only for cleanup. Their original eighteen
  task/image inventory still resolves; enabling services requires version 2.

## Cold Startup

1. Under the retained controller/state locks, first verify the disposable runtime
  is absent. Only this verified cold path uses Terraform `-parallelism=10` and
  `cold_start=true`. Apply with `services_enabled=false` (the default). This
  creates all infrastructure, twenty application task definitions and the initializer definition, but
   **no ECS application services**.
2. Start one Fargate task using `initializer_task_definition_arn`, the foundation
   cluster, `subnet_ids`, `initializer_security_group_ids`, public IP assignment
   and Linux/x86_64 platform 1.4.0. Await STOPPED; require no launch failures and
   exit code zero from container `database-initializer`. A successful RunTask
   API response is not evidence of successful initialization.
3. Apply the same release with `services_enabled=true`, `cold_start=true` and
  `startup_phase=dependencies`. Launch the five database APIs and RAG without
  waiting for each individual service inside Terraform. The controller waits
  for their complete deployments and current-revision running tasks before
  launching dependent backends.
4. Apply with `startup_phase=all` (the default) and `cold_start=true` to launch
  all twenty one-replica services. The controller requires complete deployments,
  a complete paginated task inventory and one healthy current-revision task per
  service before HTTPS checks. PostgREST alone permits `UNKNOWN` container/task
  health because its image has no ECS health check; it must still be running.
  The ALB
   detaches its initial CloudFront managed-prefix-list SG and retains only the
   SG admitting CloudFront's service-managed VPC-origin SG.
5. Repeated Up or a live release must not perform phase one or rerun the
  initializer. Live releases, rollbacks and Down retain Terraform
  `-parallelism=2`; default `cold_start=false` preserves per-service Terraform
  waiters. Dependency-only staging is rejected outside an enabled cold start.
  The controller owns these gates, expiry and serialization.
6. Down destroys this entire root, including RDS data and backups, with no final
   snapshot. It does not touch foundation resources or secret metadata/values.

The controller reports elapsed milliseconds for infrastructure, initialization,
dependency launch/readiness, application launch/readiness and each HTTPS check.
Terraform failures expose only allowlisted resource types, error codes and fixed
categories, including readiness timeouts and deployment circuit breakers. HTTPS
failures expose only fixed categories and numeric status; response bodies and
raw provider errors remain suppressed. Task readiness progress is aggregate only.
These changes remove serialized cold-start waiters, not AWS provisioning latency;
a faster live cold start remains unverified until a separately authorized run.

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
- Preserve disabled MCP/RAG behavior for unit CI. AWS enables MCP only on Garry
  and the five feature backends, using private `/mcp` and the injected MCP key.
  Shared and MCP use RAG privately on port 8100. Only MCP and its six callers
  receive the MCP key; MCP also receives the public verification key, never
  OpenRouter or the private signing key. RAG receives no injected secrets.

Garry and MCP independently read admin switches through `Services__Database`
at the private shared database API. MCP has a caller-scoped HTTP ingress edge
to that API, not direct PostgreSQL access. Its AWS-only `AllowedHosts` override
admits local health probes and `mcp-server.languagewise.internal`, without a
wildcard. Compose and host configuration remain unchanged.

MCP listens on 8080 and RAG on 8100. RAG's ECS health probe uses Python urllib,
with a 300-second startup grace, not unavailable curl. The image contains the
model cache; corpus/index and technical key are ephemeral per task. No EFS or
public tool route is added. Terraform uses parallelism two for bounded rolling
overlap. Cloud Map must omit the empty custom-health block to avoid provider
6.69.1 replacement drift; never force-delete in-use discovery services.

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
