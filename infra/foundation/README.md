# Retained Foundation

Administrative Terraform root for account `047754151573`, Sydney
`ap-southeast-2`. It is not part of Demo Down. No AWS apply, plan, credential
inspection or secret-value access was performed while implementing it.

## Offline Checks

```powershell
terraform -chdir=infra/foundation init -backend=false -input=false
terraform -chdir=infra/foundation fmt -check
terraform -chdir=infra/foundation validate
node --test infra/foundation/contract.test.mjs
```

Review the generated provider lockfiles for bootstrap, foundation and demo;
retain checksums for both Linux amd64 and Windows amd64. Offline validation
does not prove live IAM authorization, import safety, cleanup success or cost.
No Terraform tests that run a plan/apply are required for these offline checks.

## Operator Setup

1. Provision both reviewed bootstrap buckets administratively and preserve the
   bootstrap state. Outputs are `state_bucket_name` and `artifact_bucket_name`.
   Bootstrap alone owns the retained buckets and all their protections; foundation
   uses `data.aws_s3_bucket.artifacts` for the fixed artifact bucket name.
   Initialize this root's S3 backend with `-backend-config="bucket=..."`.
   The fixed key is `foundation/terraform.tfstate`, encrypted with native S3
   locking. The backend bucket argument and `state_bucket_name` variable must
   identify the same bootstrap bucket; Terraform cannot interpolate backend inputs.
2. Verify the bootstrap artifact bucket's versioning is enabled, AES256 encryption,
   public-access blocking, TLS-only policy and abort-only upload lifecycle are in
   place. Foundation's bucket data source does not manage or attest these settings;
   this is an operator gate. Both buckets have `force_destroy=false` and
   `prevent_destroy=true`. No bucket import or targeted foundation apply is needed
   on the intended first deployment, where neither bucket exists. If either is
   already present or managed by another state, stop for reviewed adoption/state
   migration rather than creating duplicate ownership.
3. Publish the reviewed cleanup ZIP out of band under `cleanup/<reviewed-id>.zip`
   in `languagewise-047754151573-ap-southeast-2-artifacts`. Record its actual S3
   VersionId, then supply it as `cleanup_bundle_version`. The ZIP must contain
   repo-relative `infra/scripts/demo.mjs`, its dependencies, `infra/demo/`, its
   provider lockfile, and any other files cleanup uses. Do not include local
   `.terraform`, state, plans, `.env`, credentials or secret values. Source download
   permissions are limited to the exact object/version. Do not expire that version.
4. Supply a private, nonsecret operator tfvars file with `cleanup_bundle_key`,
   the actual `cleanup_bundle_version`, and `terraform_sha256`, then review and
   complete foundation setup. Optional `terraform_version` defaults to `1.16.6`;
   the checksum must match its **Linux amd64 ZIP**, not the Windows executable.
   Obtain and verify HashiCorp's signed checksums independently. No checksum, ZIP
   or object version is fabricated by this configuration.
   Do not enable paid demo resources until the actual cleanup job has been tested.
5. Review imports: ECS `languagewise`, ECR `41026release2/languagewise`, and IAM
   user `languagewise-runner` at `/languagewise/`. Imports are declarative and
   deliberate, not recreation. Existing cluster capacity-provider associations are
   not managed here. ECR's existing mutable/AES256/no-scan settings are preserved;
   releases must use immutable image digests. No keys, login profile or exclusive
   user-policy set is managed. The existing `AssumeLanguageWiseValidationRole`
   inline policy and `languagewise-runner-validation` role, trust and permissions
   remain unmanaged and unchanged. The new inline policy adds only assumption of
   the three new routine roles. Any validation-role authorization is a separate,
   manual administrator responsibility, not a Terraform grant or a live change
   performed here.
6. The operator must establish any necessary AWS service-linked roles for ECS,
   RDS, ELB, CloudFront VPC origins and Cloud Map before deployment. Routine roles
   deliberately cannot create them or perform any other IAM mutation. Verify
   organization/account-plan restrictions and quotas administratively.
7. Populate/rotate application values out of band through an approved secure AWS
   process. Terraform manages metadata only, never secret versions or values.
   Confirm the SNS email subscription out of band to
   `arn:aws:sns:ap-southeast-2:047754151573:languagewise-demo-cleanup-failure`.
   Email addresses and subscriptions are not stored here. Exercise an actual
   failure notification and cleanup with the GitHub user's key disabled before Up.

## Consumer Contract

`terraform output -json` exposes **separate plain values** under Terraform's
standard `.value` envelope. Unwrap each entry once. There is no nested `foundation`
output and no JSON-string output. Write the resulting map as the disposable root's
`foundation` variable in a private JSON tfvars file. CodeBuild receives that same
map directly as `FOUNDATION_CONFIG=jsonencode(local.foundation_contract)` and
`TF_VAR_foundation` for Terraform's `foundation` variable.

Exact output keys:

```text
account_id region cluster_arn repository_url control_table_name
artifact_bucket_name cleanup_project_name cleanup_schedule_name
cleanup_schedule_group_name cleanup_alarm_name release_role_arn
deploy_role_arn control_role_arn cleanup_role_arn cleanup_dispatcher_arn
cleanup_dispatcher_role_arn cleanup_dispatcher_alarm_name
cleanup_dispatcher_throttle_alarm_name cleanup_scheduler_alarm_name
cleanup_failure_topic_arn
service_execution_role_arns service_task_role_arns secret_arns
state_bucket_name demo_state_key
```

Service role maps have the 18 Compose service names excluding `shared-db`, plus
`database-initializer`. Secret keys are `db-shared`, `db-mini-games`,
`db-quizzes-courses`, `db-chat-discussion`, `db-quests`, `openrouter-api-key`,
`signing-private-key`, `signing-public-key`.

Demo backend requirements: bucket from `state_bucket_name`, key from
`demo_state_key` (`demo/terraform.tfstate`), region from `region`,
`encrypt=true`, `use_lockfile=true`, **`workspace_key_prefix="demo/workspaces"`**.
Use only Terraform's default workspace; workspace state objects are intentionally
not authorized. Bucket-key listing is allowed so missing state/lock objects return
`NoSuchKey` rather than `AccessDenied`; this reveals names, not object contents.
The workspace prefix remains `demo/workspaces`. Runtime roles have object RW only for the exact demo key and
lock object, DeleteObject only for the lock, and explicit denial of administrative
state objects. They cannot read foundation outputs from remote state: pass the
nonsecret output contract from the administrative setup instead.

Demo resources must use `languagewise-demo-*` names for ECS task-definition
families/services, RDS identifiers/subnet/parameter groups and ALB/target groups;
keep ALB names within AWS's 32-character limit. Tags must include
`Project=languagewise, Layer=demo` on networking, discovery and CloudFront.
The demo provider also retains `Lifecycle=disposable-demo` for residual inventory.
CloudFront's global operations intentionally have no Sydney condition; regional
API permissions do. Project-scoped runtime log groups `/languagewise/demo/<service>`
belong to the demo root, which must give them bounded retention.

Database APIs inject JSON field `connection`; quests PostgREST injects JSON field
`uri`. Use ECS value references `<secret-arn>:connection::` / `<secret-arn>:uri::`.
Signing secrets are **plaintext PEM**, not JSON or paths: inject the private key
only into `shared-backend` as `Auth__SigningKeyPem`; inject the public key into the
six verifying backends as `Auth__VerificationKeyPem`. OpenRouter is injected only
into Garry, quests backend and leaderboard backend. Frontends receive no secrets.

The initializer execution role can inject the RDS-managed master secret from
`arn:aws:secretsmanager:ap-southeast-2:047754151573:secret:rds!db-*`. Its task role
can write/describe only the five database secret containers. Other task roles have
no attached permissions. Deploy/control never receive `GetSecretValue`; RDS-owned
secret metadata creation is allowed for managed master-password provisioning only.
Secret encryption uses AWS-managed keys; a custom KMS key would require a separate
review and precise permissions, not a broad KMS grant.

Routine role ARNs use `/languagewise/languagewise-{release,deploy,control}`.
All three are outputs, including `release_role_arn`. Each trusts exactly the
imported IAM user and
`arn:aws:iam::047754151573:role/managed/AccountFullAccessRole`, with **7,200-second
(two-hour) maximum sessions**. Jobs must explicitly request the required duration
and obey any shorter AWS role-chaining limit. Controllers require static temporary
session credentials, not a refreshing credential provider, so expired worker
credentials remain bounded for lock-recovery proof.
Release/deploy can push ECR and write `releases/*`; control and independent cleanup
can read manifests but not publish them. All use the retained DynamoDB table;
the script owns its `id`-keyed control/lock items, never Terraform.

## Cleanup And Gates

Independent CodeBuild uses its own service identity, `standard:7.0`, Node.js 22,
the pinned source version and a SHA256-verified Terraform ZIP. It invokes exactly
`node infra/scripts/demo.mjs cleanup`, with AWS region and the nonsecret contract
in its environment. The script must translate the contract into the demo root's
`foundation` variable, acquire the durable lock and enforce cleanup-only behavior.
CodeBuild is nonprivileged, limited to one concurrent build and 120 minutes
(30-minute queue timeout); its CloudWatch log retention is seven days.

Scheduler runs every 15 minutes and invokes only the retained Node.js 22 Lambda
dispatcher. Its 128 MB, 15-second, concurrency-one function performs one strongly
consistent read of the exact DynamoDB `control` item, then starts only the exact
cleanup CodeBuild project if `cleanupDue` is true (Down intent, expired lease or
failed startup deadline). No source or environment overrides are supplied. Lambda
cannot mutate the control table, read secrets or access state. CodeBuild cannot
modify its source, schedule, IAM, ECR
images, release artifacts or application secret values. Its runtime policies are
derived from the demo permissions with creation, task registration, RunTask and
RDS secret-metadata write grants removed. Updates needed during destroy remain
allowed; the reviewed script must still enforce cleanup-only sequencing. Native
state locking and the control lock must both be honored.

Automatic lock recovery is not a general force-unlock facility. The controller
accepts only an expired, well-formed bounded lock for one of the exact routine
roles or this cleanup project. Routine roles must still have a maximum session of
at most 7,200 seconds. CodeBuild recovery additionally verifies the exact build
ARN, role, project, timestamps, terminal status and at-most-120-minute timeout.
Legacy, unbounded, unexpired or unverifiable locks require manual recovery.
Removing an orphan native S3 lock also requires the acquired control lock,
durable Down intent, an `OperationTypeApply` lock for the exact demo bucket/key,
creation timestamps within the proven expired worker bounds, and conditional
deletion using its observed ETag. Do not bypass these checks or delete a lock
merely because its timestamp is old.

The CodeBuild failure, Scheduler target-error and dispatcher error/throttle alarms
publish to SNS.
Missing metrics are not treated as healthy execution evidence. Readiness checks
must verify schedule status, project provenance, a confirmed recipient and recent
successful cleanup. No live IAM simulation, import, build, notification or full
Up/Down rehearsal has been performed by these offline checks.

**Cost gate:** a 15-minute schedule makes 2,880 cheap Lambda invocations/control
reads in a 30-day month, not 2,880 unconditional CodeBuild starts. CodeBuild starts
only when cleanup is due; retries while Down/failed still consume billed build
minutes. Review actual duration and rounding against the migration plan's
600-minute cleanup allowance; no free-tier assumption is required.

ECR automatically expires **only untagged images older than one day**. All tagged
images are excluded from automatic expiry. The shared repository holds 19 images
per complete release: a global image-count policy could erase a service's current
digest even if enough other tags remain. An administrator must review storage at
each release and weekly during demo months, target at most **7 GiB** of ECR stored
images (headroom under the plan's **10 GB-month** assumption), and prune only
reviewed obsolete tagged digests. Protect every service/initializer digest in the
active and previous compatible manifests, all live ECS task definitions and any
in-flight release; do not infer safety from a tag count. If those protected images
exceed the target, stop new publication and review image size/budget rather than
delete a protected digest. Routine and cleanup roles receive no image-delete grant.

Bootstrap expires noncurrent state versions after 30 days and aborts incomplete
uploads after one day on both buckets. Current state and all artifact versions
have no automatic expiry. Review combined state, versions and artifacts against
the plan's **5 GB-month S3 allowance**; age retention is not a byte quota. Any
manual artifact pruning must preserve every pinned cleanup source VersionId and
active/previous compatible release manifests. Verify these retained-cost bounds
before enabling paid demo resources.
