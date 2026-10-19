# AWS Demo Infrastructure

Implementation is present; **nothing has been applied to AWS**. Budget creation
and the cost proposals were owner-confirmed on 2026-10-20. This does not constitute
new API verification of alert recipients, tax, or notification delivery.

## Ownership

| Root | State | Survives Down |
| --- | --- | --- |
| bootstrap | Protected operator-local state | State and artifact buckets, all bucket protections |
| foundation | `foundation/terraform.tfstate` in the state bucket | Imported cluster/ECR/user, scoped roles, secret metadata, control table, Lambda/Scheduler/CodeBuild, alarms |
| demo | `demo/terraform.tfstate` in the state bucket | Nothing billed in this root; non-billed inactive ECS task-definition revisions remain |

Only administrative bootstrap/foundation operations may change IAM. Routine
GitHub jobs cannot broaden their permissions, destroy the foundation, read
application secret values, or update cleanup code. The existing validation role
and access key remain outside Terraform; no replacement key is required.

## First Setup: Administrative, Not a GitHub Demo Job

1. Review this implementation and the [bootstrap](bootstrap/README.md) and
   [foundation](foundation/README.md) instructions. Confirm Sydney service/plan
   permissions, service-linked roles, root MFA, and the recipient for cleanup
   failures. Do not change the account plan silently. Review the intentional
   CloudFront ownership-tag exception below before granting routine permissions.
2. Using an authorized workstation session, initialize bootstrap, create a saved
   plan, inspect it and apply exactly that plan. Keep its local state/backups in
   protected storage. The two protected buckets must exist before publishing code:

   ```powershell
   terraform -chdir=infra/bootstrap init
   terraform -chdir=infra/bootstrap plan -out=bootstrap.tfplan
   terraform -chdir=infra/bootstrap apply bootstrap.tfplan
   ```

3. After code is reviewed/committed, package its exact committed cleanup source:

   ```powershell
   node infra/scripts/package-cleanup.mjs
   ```

   This accepts only the demo root's Terraform/lock files and two controller
   modules. It refuses modified packaged paths and uncommitted implementation.
   Upload the reported ZIP to the artifact bucket at its reported
   `cleanup/<sha256>.zip` key using `aws s3api put-object --body <local-zip>`.
   Record the returned **VersionId**, not an invented placeholder. Keep that
   version available for as long as foundation references it. Review the actual
   Linux amd64 Terraform 1.16.6 ZIP checksum against HashiCorp's signed checksums.
4. Supply private, ignored foundation tfvars containing `cleanup_bundle_key`,
   `cleanup_bundle_version`, and `terraform_sha256` (only nonsecret values).
   Initialize foundation's backend, inspect its saved plan/imports, and apply:

   ```powershell
   terraform -chdir=infra/foundation init -backend-config="bucket=languagewise-047754151573-ap-southeast-2-tfstate"
   terraform -chdir=infra/foundation plan -out=foundation.tfplan
   terraform -chdir=infra/foundation apply foundation.tfplan
   ```

   Review adoption of ECS `languagewise`, ECR `41026release2/languagewise`, and
   IAM `languagewise-runner`; do not recreate/delete them. No application tasks,
   network, RDS or CloudFront runtime are created by these two roots.
5. Enter **three** real application values directly in Secrets Manager: the
   dedicated OpenRouter key, a fresh signing private PEM, and matching public PEM.
   Their names are `languagewise/demo/openrouter-api-key`,
   `languagewise/demo/signing-private-key`, and
   `languagewise/demo/signing-public-key`. Do not use workflow inputs, Terraform
   variables, GitHub Secrets, source, or chat for these values. Set OpenRouter's
   separate $10 spending limit. Leave email delivery unconfigured/disabled unless
   separately costed and approved. The initializer populates the five database
   secrets; do not copy local development database passwords into them.
6. Export the **nonsecret** flat output contract and set the `aws-demo`
   environment variable `AWS_FOUNDATION_CONFIG`. For example, in PowerShell:

   ```powershell
   $outputs = terraform -chdir=infra/foundation output -json | ConvertFrom-Json
   $config = @{}
   $outputs.PSObject.Properties | ForEach-Object { $config[$_.Name] = $_.Value.value }
   $config | ConvertTo-Json -Depth 10 -Compress | gh variable set AWS_FOUNDATION_CONFIG --env aws-demo --repo Yooshi124/LanguageWise
   ```

   This contains identifiers/ARNs only. Preserve the existing environment AWS
   access-key secrets and its **master-only** deployment rule. Require reviewed
   workflow/infrastructure changes and passing CI on protected master.
7. Confirm an SNS subscription to the cleanup-failure topic. Start the cleanup
   CodeBuild project administratively once and verify success with an empty/down
   demo. Test actual failure notification delivery. Up checks the schedule,
   dispatcher, alarms, confirmed subscription, recent successful build, service
   trusts, and content-addressed versioned cleanup source before paid provisioning.
8. Merge the implementation. The `AWS Release` workflow waits for successful
   CI of that exact SHA, publishes all nineteen immutable image digests and records
   the release. It must **not** create runtime resources while mode is Down.
   A manual release dispatch reuses an already-published SHA instead of changing
   its release identity. Required workflows must be enabled and succeed; missing
   runs fail closed. MCP CI remains a source-quality gate, not an MCP deployment.

## Demo Buttons

In GitHub Actions choose **AWS Demo Up / Down**, select branch **master** and
operation `up` or `down`. Default is Down. No workstation login or homebox is
needed. Both this workflow and automatic release use concurrency group
`languagewise-aws-demo` with cancellation disabled, plus the DynamoDB control
lock and Terraform S3 lock.

**Up** selects the latest tested manifest, verifies cleanup readiness and required
non-database secrets, then reserves four billed hours atomically. It creates
infrastructure with application services disabled, runs the one-off initializer,
checks its exit code and published database-secret metadata, then starts eighteen
services. It waits for steady state and checks HTTPS, canonical routes and five
federation entries. The run reports the current CloudFront URL and expiry.

The two-hour lease starts **at the Up request**, including startup; it is not two
hours after readiness. Startup has a one-hour deadline. Each new cycle reserves
four hours against ten cycles/forty hours per UTC calendar month. Failed cycles
retain their reservation; this is a conservative admission allowance, not an
invoice meter or hard billing cap. Repeated Up does not extend the lease, consume
another reservation, or reseed. There is no implicit renewal/extension button.

**Down** records Down intent first, stops initialization if necessary, and applies
an exact destroy plan for **only** the demo root. RDS data/backups are deleted
without a final snapshot. The next Up recreates fresh seeds. State, images,
secrets, control infrastructure and scoped identities survive. Non-billed ECS
task-definition revisions intentionally remain: routine roles have no
account-wide deregistration grant. Down independently checks for paid/network
residuals and reports failure rather than declaring an incomplete teardown safe.

Merges while Down only publish the next tested release. While Up, a tested
release rolls services without reseeding; smoke failure attempts the previous
compatible manifest, and failed recovery requests Down. This is not an automatic
database-schema rollback or a substitute for compatible migration design.

## Independent Expiry and Recovery

Scheduler invokes a retained Lambda every fifteen minutes. Lambda performs a
small strongly consistent control read and starts CodeBuild **only when cleanup
is due**, avoiding 2,880 idle CodeBuild starts per month. The source-pinned cleanup
job runs under its own AWS role, not the GitHub key or repository token. The cheap
dispatcher/control requests fit the scenario's existing $1 control allowance;
the 600 CodeBuild-minute allowance remains an assumption to measure.

All workers have bounded runtimes. Routine role sessions are at most 7,200 seconds,
GitHub jobs at most 100 minutes, controllers 90 minutes, CodeBuild 120 minutes.
The durable lock has a fixed, non-renewable 150-minute recovery deadline. Only
verified independent CodeBuild cleanup may recover an expired bounded lock, after
Down intent and worker/credential checks; previous CodeBuild workers must be
positively terminal. Native state-lock recovery also requires the exact path,
bounded creation time, known operation, and conditional ETag deletion.

Cleanup is not guaranteed within fifteen minutes: lock safety, queueing, AWS
failures and resource deletion take longer. Legacy/malformed locks and unprovable
termination fail closed and require operator investigation. Never force-unlock
while a writer might still run. Quiesce/revoke any older longer-duration role
sessions before relying on the new bounds. Do not change role duration or worker
timeouts independently of the recovery proof. Failure alarms need a real receiver.

After changing runtime/controller code, publish a new reviewed cleanup ZIP and
apply foundation with its real new key/version **before** enabling the changed
runtime. An old source bundle is not automatically compatible with new Terraform
state. Only administrative foundation setup can change this provenance.

## Security Boundaries Requiring Review

- CloudFront `CreateDistributionWithTags` also requires `TagResource`. The initial
  grant is restricted to this account's resources with **neither Project nor Layer
  set**, the fixed LanguageWise request tags, and the three allowed tag keys.
  This also permits initially tagging an existing completely unowned distribution
  or VPC origin; IAM does not expose a create-only TagResource condition here.
  Treat untagged account CloudFront resources as inside this bootstrap boundary,
  not as protected other projects. Already-owned resources cannot be taken over
  through this initial grant. Independent cleanup does not receive it. Do not
  apply this role design to a shared account with unrelated untagged CloudFront
  resources without a different administrative provisioning boundary.
- Public task IPv4 provides outbound connectivity only; ingress is restricted by
  caller security groups. The ALB/database are private. Browser HTTPS terminates
  at CloudFront; the private origin hop is HTTP. Shared trusts forwarded scheme
  only from the configured demo network, with gateway-only backend ingress.
- Secret values never enter Terraform. Database passwords are generated in the
  initializer; the RDS-managed master secret is injected directly by ECS. The
  initializer can publish only the five database credential secrets. Four .NET
  database images and PostgREST carry the RDS CA bundle and verify TLS.
- Image pruning expires only untagged images automatically. Review tagged
  releases manually, retaining all nineteen digests for current/previous
  compatible manifests. Keep ECR within 10 GB and source/state/artifacts within
  5 GB; versioning/lifecycle are not byte quotas. No paid scanning, NAT, resolver
  endpoint, premium insight or customer-managed encryption key is added.

## Verification and Live Gate

Local checks: backend-disabled Terraform initialization/validation, mocked runtime
plans, Node control/release/expiry/contract tests, Python initializer tests,
actionlint, focused .NET auth/RAG tests, initializer image build, and all six
frontend image/nginx checks. These do not prove live AWS permissions or operation.

Before using this for a presentation, perform two complete Up/Down cycles. Verify
seed accounts/login, Secure cookies, all remote pages, durable writes during task
replacement, authenticated Garry streaming, and clean disabled MCP/RAG responses.
Exercise failed Up, an unhealthy release/rollback, expired lease, a merge while
Down, a merge while Up, and independent cleanup with the GitHub key disabled.
Confirm all paid residuals are gone, measure transition time/idle charges, and
recost after the first two cycles. Do not claim these live rehearsals passed on
the strength of mocked plans or static contracts.
