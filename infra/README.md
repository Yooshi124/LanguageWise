# AWS Demo Infrastructure

## Current Monthly Startup Policy

Owner-approved on 2026-10-23: the ongoing admission guard is **20 starts and
240 reserved hours per calendar month**, not a one-month exception. Existing usage
is preserved; the limit change does not reset or refund reservations. Every new
start reserves twelve hours, even if startup fails or the demo is stopped early.
Reservations are not actual AWS charges or credit usage.

New starts have a ten-hour lease from the Up request; existing leases are not
extended. The one-hour startup deadline and independent cleanup remain unchanged.
The $160 planning budget is unchanged; AWS budget alerts are not
modified by this code change. Historical cost estimates below are not a fresh
quote for a 240-hour month. Publishing this change does not itself launch Up.

## SMTP Setup: Cutover Complete

On 2026-10-23, the owner populated `languagewise/demo/smtp-credentials` directly
in `ap-southeast-2`; its `AWSCURRENT` metadata was verified without retrieving
credential values. Only the notifications backend's ECS execution role can read
this secret. The approved Down and scoped foundation cutover are complete;
application publication and end-to-end email verification remain pending.

For future rotation, enter the values directly in AWS Secrets Manager as JSON:

```json
{
   "username": "<sender Gmail address>",
   "password": "<rotated Gmail app password>"
}
```

Use a Gmail app password, not the Google account password. Do not post either
value in chat or place them in Terraform, source, GitHub variables or logs.
`Smtp__Host` and `Smtp__Port` are not secrets; the application defaults to
`smtp.gmail.com:587` with STARTTLS. ECS injects only `Smtp__Username` and
`Smtp__Password` from the corresponding JSON fields.

Approved Down
[39894774435](https://github.com/Yooshi124/LanguageWise/actions/runs/39894774435)
succeeded. Local implementation commit `b6f0c3156e388180795584534a64512c921a8fa0`
was packaged into immutable cleanup source:

- Key: `cleanup/a29f7c16cfa691f90efedc6a3ba5bf8facecf968118996ec7141a2545b5931ab.zip`
- Version: `jVSV1sZGOE8ec_2xeF_tIzZbFbyaTZ0t`
- Native verification build: `languagewise-demo-cleanup:f34cc56f-8115-4c1b-8508-33521a3de8a1`, all phases succeeded.

The four routine/cleanup inline policies now reference the exact new source
version and include SMTP metadata access, not credential-value access. CodeBuild
source/version and both embedded foundation contracts were updated together;
GitHub's `AWS_FOUNDATION_CONFIG` and refreshed Terraform outputs match the same
ten-secret contract. State reconciliation used a refresh-only plan with zero AWS
resource mutations. A final non-applied preview has no IAM/CodeBuild changes;
unrelated ECS cluster drift was not applied. All 18 final resource-specific IAM
checks passed; the earlier 159 Node tests and nine mock Terraform tests passed.

Fresh independent checks verified Down/Down, no writer lock or disposable runtime
residuals, cleanup readiness, and all four alarms OK with actions enabled. The
native build exercised the already-Down path, not a nonempty teardown or email
delivery. No credential values were retrieved, push, merge or new Up performed.

**Publication is next:** publish the local implementation and runbook changes
through the normal reviewed process while Down. Require fresh successful CI and
the exact merged-SHA AWS Release before a separately approved new Up. SMTP
injection and actual delivery must then be verified; secret population and this
foundation cutover alone do not deploy the application.

## Earlier Cold-Start Follow-up: 2026-10-22

Timed Up [39695904779](https://github.com/Yooshi124/LanguageWise/actions/runs/39695904779)
used the exact successful 21-image release `bd673cf22b96523de7674ddecb4c5a8ca830760f`.
It failed in the Terraform ECS-service apply at 00:34:09 UTC, before HTTPS smoke
checks and before the one-hour startup deadline. The old diagnostic retained only
`resource_type=aws_ecs_service; code=unknown; category=provider-or-unknown`;
the exact provider rejection or waiter failure cannot be recovered from that log.
Automatic rollback verified Down at 01:06:20 UTC. Independent read-only checks
confirm Down revision 51, no writer lock and no disposable resource residuals.
Monthly reservations remain nine cycles and 36 hours of the unchanged 10/40 limits.

Cold-start optimization now uses a verified-empty-runtime gate, Terraform
parallelism 10 and controller-owned aggregate readiness rather than serialized
per-service waiters. Initialization precedes the five database APIs and RAG;
their ready tasks precede dependent application services. Live updates, rollbacks
and destruction retain parallelism 2 and the default Terraform waiters.
Readiness requires current-revision task health, with the documented PostgREST
exception. Phase timings and fixed failure categories do not disclose raw errors.
See the [cold-start contract](demo/README.md#cold-startup).

The PR follow-up adds named, bounded service diagnostics and rate-limited,
redacted stopped-task reason codes, so an incomplete deployment no longer hides
all other healthy tasks behind a zero counter. Release publication can also
reuse the complete previous immutable image set for strictly allowlisted
runtime-only changes, with archive equality, ancestor-diff and ECR availability
proof. Current-SHA checks, complete manifests and fresh release sequences remain
mandatory; any unproven or image-affecting change uses the full build. See the
[reuse contract](images/README.md#runtime-only-release-reuse). The initial
publisher/control-code change deliberately rebuilds all images. These follow-ups
are locally tested, not proof of a successful hosted publication or cold start.

The failed run took about 17.5 minutes to reach initialization, then spread twenty
service creations over 21.5 minutes. Quizzes backend DNS/health failures preceded
its database service creation. Staging fixes that observed ordering; it does not
prove it was the original Terraform failure's sole cause. A faster live startup
and a successful active-demo image update remain unverified. No further Up is
authorized, and no push, merge, size, quota, timeout or lease change was made.
The owner revised the monthly planning budget to USD 160; credits offset charges,
not gross costs. AWS Budget alerts and the OpenRouter limit were not changed.
The existing pinned cleanup bundle remains unchanged; resource addresses and
cleanup inputs remain compatible, including partial dependency-stage teardown.

## Earlier Follow-up: 2026-10-22

See the [MCP/RAG and reliable redeployment plan](../docs/aws-mcp-rag-and-redeployment-plan.md)
for implementation evidence, continued local development and the live-update
repair. Up `39424991893` succeeded and the owner verified app access,
AI and database reads. The subsequent release `39426732157` failed on in-use Cloud
Map service deletion; fallback teardown and queued Down `39430924116` succeeded.
Fresh checks verify Down at revision 46, no lock and no runtime residuals. Native
provider 6.69.1 reproduces the empty custom-health block replacement offline;
omitting it produces no-op. The original incident plan remains unavailable.
The private 20-service/21-image integration passes local contracts, Terraform
tests and 26 real-container E2E checks. RAG fits 0.5 CPU/1 GiB offline after
bounding ONNX threads and baking its model cache; no size or limit was raised.
Authorized AWS preparation created four roles, two execution policies and an
MCP secret metadata entry, and updated six exact secret-consumer policies. The
owner populated `languagewise/demo/mcp-api-key` directly; only its current-version
metadata was verified, never its value. The Down-only cleanup cutover is complete:
GitHub, Terraform outputs and both CodeBuild contract variables match 21 roles
and nine secret ARNs. Two PassRole policies add only the four new role ARNs;
four inline policies update exact source pins and MCP metadata access. All 44
positive/negative IAM simulation decisions pass, including secret-value denials.

Cleanup uses reviewed implementation commit `e27d053`, content-addressed object
`cleanup/a136f0ab33c8731cc753d98ce692b6c35eabdcff4742c9d25ca597193dc88018.zip`,
S3 version `bGf.5Uq7XS_8A3.PJ13CNqYxk.74efoa` (26,154 bytes, AES256; exact-version
download checksum verified). Native cleanup build
`7457872b-8928-47a2-baf5-b4b5f2f19880` succeeded while already Down. Fresh independent
readiness/residual checks pass, revision 46 and no lock remain unchanged, all four
alarms are OK with actions enabled, and the dispatcher starts no build while Down.
This verifies the already-Down path, not destruction of a newly populated stack.
The final non-applied preview has zero IAM/CodeBuild changes; unrelated ECS cluster
drift was not applied. Role, schedule, timeouts, quotas and budget limits were not
changed. No application secret value was read and no Demo Up was started.

Owner publication is next: push the local commits through a normal PR, require
passing CI, merge, then wait for the exact merged-SHA 21-image release while Down.
No push or merge was performed by the assistant. Do not run Up until that release,
cleanup readiness and the remaining cost/storage gates have been checked and a
new timed demo is explicitly approved. The cleanup archive remains pinned to
`e27d053`; later documentation-only commits do not change its implementation.
Keep the temporary Down-before-merge procedure until a real active-demo image
update succeeds. Notes below retain earlier rollout history.

## Earlier Rollout History

**Earlier Demo Up failed before readiness; application readiness was unverified.**
Run `39412985687` on merged/tested/released `b3e0b6d` passed RDS creation and
database initialization, then failed in the services apply. CloudTrail records
18 `ecs:ListServiceDeployments` denials for the control role at 11:29:12-14 UTC.
The pinned provider's steady-state waiter then needs `ecs:DescribeServiceDeployments`;
both reads were missing from the IAM grants. With owner approval, four managed
policies were updated from `v1` to `v2`, adding only these reads on
Sydney/account/demo-service and demo-deployment ARN scopes. Exact readback and
112 positive/negative IAM simulations pass. A fresh targeted preview shows no
remaining IAM or cleanup-source changes, only the known unrelated ECS cluster
drift. That drift was not applied; the temporary plan was deleted.

**Cleanup recovered: native worker and fresh independent checks verify Down.**
Automatic rollback, recovery Down `39417298976` and independent workers failed
verification because the tag index retains inactive ECS metadata. At diagnosis,
control was `desiredMode=down`, `status=stopping`, with no lock. RDS instances,
running tasks and demo VPC inventory were empty; all 18 tagged ECS
service ARNs describe as `INACTIVE` with zero desired/running/pending tasks.
The tag index retains these records even though `ListServices` is empty. The
verifier now requires complete exact-ARN inactive or `MISSING` proof in batches
of ten, rejecting active, nonzero, incomplete, duplicated, malformed and foreign
records. With owner approval, cleanup-only commit `610e02d` was packaged and pinned
without pushing. Native build `4e1245f1-ec69-4bd4-ad30-792464d3508a` succeeded and
normally persisted verified Down. Fresh readiness and full residual checks pass,
with no lock; the dispatcher starts no build while Down. All four alarms are OK
with actions enabled. Reservations remain 7 cycles and 28 hours of the 10/40
monthly limits. All 126 focused tests pass; Terraform validation, touched-file
formatting and editor diagnostics are clean. No further Up, manual Down marking,
alarm disabling, account-plan change or push was performed. Foundation policy,
regression and runbook changes remain uncommitted for owner publication.

Earlier run `39404244548` on `c452d5e` reported `Terraform infrastructure apply failed;
resource_type=aws_db_instance; code=unknown` and then verified automatic Down.
At that time control metadata confirmed Down with no lock. RDS CloudTrail lookups in
Sydney and `us-east-1`, and RDS native instance events, contain no create failure
message for this attempt, so its exact original rejection remains unavailable.

**Confirmed account-plan blocker:** `freetier get-account-plan-state` reports
`FREE` / `ACTIVE`, while the failed run requested `db.t4g.small`. AWS documents free-plan
RDS support for `db.t3.micro` and `db.t4g.micro`, not `db.t4g.small`:
[AWS RDS free-plan limits](https://aws.amazon.com/rds/free/). Regional catalog
availability does not establish account-plan eligibility. On 2026-10-20 the owner
approved changing this root to `db.t4g.micro` and starting one timed demo after
validation. The merged resize preserves PostgreSQL 18.6, 20 GiB gp3, encryption,
private access and managed credentials. Sydney's orderable-options API confirms
that combination in all three regional zones; Terraform validation passes.
The latest run proves creation and initialization, not application readiness.
Exact-SHA CI/release and cleanup-source readiness passed before that attempt.
The account plan, quotas, lease and cleanup limits remain unchanged. Budget alerts
are not a hard spending cap.

The diagnostics follow-up merged in `b3e0b6d` scans both `summary` and `detail` for fixed,
allowlisted error codes. The pinned provider's `sdkdiag.AppendErrorf` uses the
summary field; the original synthetic fixture incorrectly put its error in
detail only. Both locations now have redaction regressions. Raw messages remain
suppressed. This parser correction is not in the pinned cleanup
archive used by the failed runs, but is included in the replacement below; it is
not itself a fix for the account-plan incompatibility.
Before the resize, all 120 focused foundation/controller/release tests passed. Editor diagnostics and
`git diff --check` are clean. Fresh read-only cleanup readiness and full residual
verification also pass, with control Down and no lock. No commit, publication,
repin, Up, resize or account-plan change was made in that earlier investigation.

Earlier run `39393527248` on `9d4be81` failed after creating its private-DNS namespace,
18 discovery services and CloudFront resources. No initializer `RunTask` or RDS
`CreateDBInstance` event was found. Complete provisioning-window CloudTrail lookups
in Sydney and `us-east-1` did not identify a new authorization denial. The configured
RDS version, instance class and gp3 size are available in Sydney; that check does
not prove database creation will succeed. Investigation inconclusive: the original
Terraform diagnostic was discarded, so the new startup cause is not confirmed.
Automatic rollback completed; fresh cleanup readiness, full residual verification,
persisted Down and absent lock checks passed. No new Up or IAM change was made.

The initial controller repair, originally committed as `be71f29`, is present in
the latest run. It requests Terraform JSON plan/apply output
and reports the failing infrastructure/services/destroy phase plus allowlisted
resource types, error codes and validation categories before rollback. Unknown or
malformed diagnostics remain suppressed, along with all raw output, summaries,
details, resource names, credentials and state. Initial validation covered 119 focused tests, including
redaction and unchanged timeout/lock/rollback regressions. The production parser
also accepted an actual offline Terraform JSON error without exposing raw output.
The replacement immutable cleanup archive from committed source `610e02d` is now
published and pinned, including the merged micro resize and summary parser plus
the inactive-ECS verification repair:

- Key: `cleanup/d931e8b5e46229724216b1e297b46b6d194a5539830612d368e5b003f8134b37.zip`
- VersionId: `IpFvxdJzfJUXZcU3ufum8xxFuFO9r6ww`; 25,227 bytes, AES256;
   local, server and downloaded SHA256 verified; create-only upload.
- CodeBuild and all four exact-source inline policies match; non-source settings
   and statements were preserved. The ignored foundation variables match as well.
- Native cleanup build `4e1245f1-ec69-4bd4-ad30-792464d3508a` succeeded through
   download, install and execution from `stopping`, then persisted verified Down
   and released its lock. This proves delivery and native cleanup finalization,
   not application startup or destruction of a newly provisioned nonempty stack.
- A targeted foundation preview has no IAM or CodeBuild changes. Its sole
   unrelated ECS change was not applied, and the saved preview was deleted.

Cleanup readiness and full residual checks pass after the replacement archive was
pinned and its native worker completed. The prior `df6edf9...` archive lacked the
micro resize, summary parser follow-up and inactive-ECS proof. Publish/review the
remaining source changes and require exact-SHA CI/release and fresh cleanup
readiness before another explicitly approved cycle. No additional Up is authorized.

The preceding run `39375711048` on `cc71e05` was denied `route53:CreateHostedZone`.
That dependency is allowed only through Cloud Map delegation at the global Route 53
endpoint. A scoped-role DNS create/delete test passed, direct DNS reads stayed
denied, and the probe was removed. The latest run's successful namespace creation
confirms that this was not a repeat of the prior DNS failure.
Budget creation and the cost proposals
were owner-confirmed on 2026-10-20. No secret values were retrieved or fresh billing
audit performed.

## Current Progress: 2026-10-20

- **Steps 2-4 complete:** protected buckets and foundation are applied. The reviewed
   cleanup ZIP is uploaded with its real S3 VersionId, and the Terraform 1.16.6
   Linux amd64 checksum was verified against HashiCorp's signed manifest.
- **Steps 5-6 complete, owner-confirmed:** the three application secrets are saved,
   OpenRouter's separate $10 key limit is set, and the nonsecret foundation contract
   is configured as `AWS_FOUNDATION_CONFIG` in GitHub's `aws-demo` environment.
- **Step 7 verified again:** the replacement native cleanup build above succeeded,
   persisted Down and released its lock. Fresh residual/readiness checks pass,
   the dispatcher starts no build, and all four armed alarms are OK.
   Earlier native cleanup build
   `5c176bce-ac93-4b5b-a264-be9fcba5eb86` succeeded, verified Down and released its
   lock. Reinvoking the dispatcher while Down started no build. The three required
   service-linked roles are now foundation-owned. Runtime policy `v6` includes the
   owner-approved Cloud Map tagging exception described below; live scoped-role
   tests allowed intended demo tags and denied foreign project request tags.
   Runtime `v6` and cleanup `v3` also include the restricted VPC security-group
   read. Cleanup commit `a45ada2` follows bounded tag pagination while retaining
   repeated-token, malformed-page and unknown-resource rejection. Its native
   worker verified Down; the dispatcher then started no new build and all four
   alarms were OK with actions enabled. Never set Down manually.
   DNS lifecycle policies `languagewise-{demo,cleanup}-resources-6` and their
   three attachments are imported into foundation state with the expected tags.
   A targeted preview shows no IAM changes; the unrelated ECS difference was
   not applied. Cleanup receives no hosted-zone creation grant.
- The earlier Terraform and security-hook fixes for
   [PR #70](https://github.com/Yooshi124/LanguageWise/pull/70) passed local checks.
   The cleanup permission fix is also represented in Terraform with regression
   coverage. Ensure outstanding fixes are committed/reviewed and their latest
   GitHub CI checks pass; do not infer current merge status from this document.
- **Step 8 publication is not runtime readiness:** the latest Up on `b3e0b6d`
   passed RDS and initialization but hit the proven ECS deployment-read denial.
   Full application startup and HTTPS smoke checks have not passed.
   Review the scoped IAM and inactive-metadata repairs above, preserve the immutable cleanup-source
   process and require exact-SHA CI/release success before an explicitly approved
   cycle. Failed cycles retain their original reservation. Do not apply an old
   saved foundation plan or assume these repairs prove application readiness.

The cleanup IAM repair was applied separately to avoid an unrelated ECS Exec
configuration reset in a foundation plan. Review that remaining plan difference
before a future foundation apply; do not apply an old saved plan indiscriminately.

## Failure Review And Prediction

| Repair | Purpose | Earlier Check |
| --- | --- | --- |
| Linux/Windows provider hashes | Permit readonly Terraform initialization on both platforms | Clean Linux and Windows CI initialization |
| Foundation JSON trimming | Accept BOM-prefixed Windows configuration | BOM input fixture |
| Explicit S3 get/put flags | Supply required streaming CLI arguments and file paths | Actual CLI transfer contract test |
| Current-object and pinned-version S3 reads | Allow CodeBuild download while preserving immutable source provenance | Source-download smoke test |
| AWS-owned managed prefix-list read | Read CloudFront's origin-facing network list | IAM resource-owner and region checks |
| Cloud Map request-tag grant | Use supported wildcard scope and document accepted retagging capability | Official authorization reference and positive/negative live tests |
| Retained service-linked roles | Provide ELB, RDS and CloudFront VPC-origin prerequisites without routine IAM writes | Role inventory before provisioning |
| CodeBuild `buildComplete` metadata | Validate actual running/completed workers | Real API response fixture rather than invented fields |
| CodeBuild project-scoped metadata read | Authorize `BatchGetBuilds` against its actual IAM resource type | Service authorization reference and native identity canary |
| Exact-ID stale-tag confirmation | Distinguish deleted subnet/rule records from live resources without ignoring unknown types | Eventual-consistency integration fixtures; timing still requires live evidence |
| Complete bounded tag pagination | Check every page instead of failing after inventory grows beyond 100 items | Multi-page, repeated-token and malformed-page fixtures |
| Owned-VPC security-group read | Support the pinned provider's `GetSecurityGroupsForVpc` call | Scoped network canary using the pinned provider |
| Delegated Route 53 lifecycle | Let Cloud Map create, inspect and delete its private DNS zone without direct DNS access | Private-DNS lifecycle canary, not an HTTP namespace test |
| Allowlisted Terraform failure diagnostics | Distinguish the failed phase and safe error metadata from successful rollback without exposing raw output | Native JSON diagnostic fixtures with secret sentinels, malformed events, timeout/rollback regressions and an actual offline Terraform error |
| Provider summary-field classification | Extract only known codes from the provider SDK's actual error field without printing the message | Summary-only and detail-only redaction fixtures based on the pinned provider helper |
| Approved RDS micro resize | Replace the free-plan-incompatible small class without changing storage, networking or credentials | Account-plan metadata, documented RDS plan limits, exact engine/class/storage availability, Terraform validation and a class contract regression |
| ECS deployment reads | Allow the pinned provider's list-then-describe steady-state waiter without adding deployment writes | Actual waiter code, exact CloudTrail denial, official IAM resource types, scoped regression and 112 positive/negative role simulations |
| Inactive ECS exact-ARN proof | Distinguish retained inactive service tags from running resources without ignoring unknown records | Native `INACTIVE`/zero-count response fixtures, complete batched proofs, active/malformed/foreign negative cases and successful native cleanup finalization |

Most configuration, permission and response-shape fixes were predictable. In
particular, the Route 53 dependency should have been checked earlier: an HTTP
namespace does not exercise private DNS, and Sydney-only CloudTrail lookup misses
global Route 53 events recorded in `us-east-1`. Passing regex/unit contracts or a
single-action IAM simulation is not proof that dependent AWS calls will succeed.
An ECS create check must also complete the pinned provider's steady-state waiter:
service creation alone does not exercise its deployment list/describe calls.
Cleanup fixtures must represent inactive ECS records as well as deleted EC2 tags.

Before another full cycle, review the pinned provider's API/dependency surface,
validate actual supported IAM resource/condition keys and test creation, reads and
deletion with the exact principals. Use representative fixtures for pagination and
worker metadata. Keep tests tiered: offline checks, read-only account readiness,
small scoped lifecycle canaries, then one explicitly approved end-to-end cycle.
Inspect both Sydney and global-service event regions on failures.
CloudTrail is not a substitute for the controller's own failure diagnostics: a
missing create event does not establish why Terraform failed. Preserve safe phase
and diagnostic metadata before rollback instead of inferring a permission defect
from the final exit code or spending another reservation on an unchanged retry.

This does not predict every operational failure. Application initialization,
secret consumption and all-service HTTPS readiness remain unverified by the DNS
canary. Live quotas, throttling, provisioning delays and eventual-consistency timing
cannot be guaranteed by static checks. Treat these as untested boundaries, not
proven additional bugs; do not preemptively grant administrator access or unrelated
actions. No further Demo Up was started for this repair.

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
   Before any Up, use an administrative session to run
   `aws freetier get-account-plan-state --region us-east-1`. The approved
   `db.t4g.micro` design follows the owner's approved FREE-plan resize; remaining
   credits and catalog availability alone do not prove all plan/configuration
   restrictions have been satisfied. Verify initialization and application health.
2. **Completed for the current setup.** Retain these commands for reference; do
   not recreate the buckets or discard the protected local state. For a fresh
   setup, using an authorized workstation session, initialize bootstrap, create a saved
   plan, inspect it and apply exactly that plan. Keep its local state/backups in
   protected storage. The two protected buckets must exist before publishing code:

   ```powershell
   terraform -chdir=infra/bootstrap init
   terraform -chdir=infra/bootstrap plan -out=bootstrap.tfplan
   terraform -chdir=infra/bootstrap apply bootstrap.tfplan
   ```

3. **Completed for the current setup.** For a new reviewed cleanup revision,
   package its exact committed source:

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
4. **Completed for the current setup.** For initial setup or a reviewed update,
   supply private, ignored foundation tfvars containing `cleanup_bundle_key`,
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
5. **Completed, owner-confirmed.** For setup or rotation, enter **three** real
   application values directly in Secrets Manager: the
   dedicated OpenRouter key, a fresh signing private PEM, and matching public PEM.
   Their names are `languagewise/demo/openrouter-api-key`,
   `languagewise/demo/signing-private-key`, and
   `languagewise/demo/signing-public-key`. Do not use workflow inputs, Terraform
   variables, GitHub Secrets, source, or chat for these values. Set OpenRouter's
   separate $10 spending limit. SMTP credentials and their foundation cutover are
   complete; follow [SMTP publication status](#smtp-setup-cutover-complete)
   before activating and verifying delivery.
   The initializer populates the five database
   secrets; do not copy local development database passwords into them.
6. **Completed, owner-confirmed.** After foundation output changes, export the
   **nonsecret** flat output contract and set the `aws-demo`
   environment variable `AWS_FOUNDATION_CONFIG`. For example, in PowerShell:

   ```powershell
   $outputs = terraform -chdir=infra/foundation output -json | ConvertFrom-Json
   $config = @{}
   $outputs.PSObject.Properties | ForEach-Object { $config[$_.Name] = $_.Value.value }
    $previousOutputEncoding = $OutputEncoding
    try {
       $OutputEncoding = [System.Text.UTF8Encoding]::new($false)
       $config | ConvertTo-Json -Depth 10 -Compress | gh variable set AWS_FOUNDATION_CONFIG --env aws-demo --repo Yooshi124/LanguageWise
       if ($LASTEXITCODE -ne 0) { throw 'Foundation variable export failed.' }
    } finally {
       $OutputEncoding = $previousOutputEncoding
    }
   ```

    UTF-8 without a byte-order mark avoids a leading invisible character in native
    CLI input from Windows PowerShell. If Check foundation contract reports an
    unexpected token before the opening JSON brace, repeat this export and retry
    AWS Release; updating the nonsecret variable does not start the demo.

   This contains identifiers/ARNs only. Preserve the existing environment AWS
   access-key secrets and its **master-only** deployment rule. Require reviewed
   workflow/infrastructure changes and passing CI on protected master.
7. **Completed, owner-confirmed.** For setup/reverification, confirm an SNS
   subscription to the cleanup-failure topic. Start the cleanup
   CodeBuild project administratively once and verify success with an empty/down
   demo. Test actual failure notification delivery. Up checks the schedule,
   dispatcher, alarms, confirmed subscription, recent successful build, service
   trusts, and content-addressed versioned cleanup source before paid provisioning.
8. **Current step:** merge the reviewed implementation and outstanding fixes into
   protected `master` only after their latest CI passes. If already merged, check
   the resulting release rather than merging again. The `AWS Release` workflow waits for successful
   CI of that exact SHA, publishes all twenty-one immutable image digests and records
   the release. It must **not** create runtime resources while mode is Down.
   A manual release dispatch reuses an already-published SHA instead of changing
   its release identity. Required workflows must be enabled and succeed; missing
   runs fail closed. MCP, RAG and real-container AI E2E are mandatory gates;
   AWS infrastructure CI also runs Garry's .NET tests.

## Demo Buttons

In GitHub Actions choose **AWS Demo Up / Down**, select branch **master** and
operation `up` or `down`. Default is Down. No workstation login or homebox is
needed. Both this workflow and automatic release use concurrency group
`languagewise-aws-demo` with cancellation disabled, plus the DynamoDB control
lock and Terraform S3 lock.

**Up** selects the latest tested manifest, verifies cleanup readiness and required
non-database secrets, then reserves twelve hours atomically. It creates
infrastructure with application services disabled, runs the one-off initializer,
checks its exit code and published database-secret metadata, then starts twenty
services. It waits for steady state and checks HTTPS, canonical routes and five
federation entries. The run reports the current CloudFront URL and expiry.

The ten-hour lease starts **at the Up request**, including startup; it is not ten
hours after readiness. Startup has a one-hour deadline. Each new cycle reserves
twelve hours against twenty cycles/240 reserved hours per UTC calendar month. Failed cycles
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

- Route 53 dependencies require `aws:CalledVia` to include
   `servicediscovery.amazonaws.com` and the global `us-east-1` endpoint. Creation
   and name lookup require wildcard resource scope; zone lifecycle actions use
   hosted-zone ARNs. Direct Route 53 access remains denied. The real private-DNS
   lifecycle test verified creation and deletion through the routine role, but did
   not launch application services. Independent cleanup has lifecycle access only,
   not hosted-zone creation.
- Cloud Map `TagResource` requires wildcard resource scope and does not support
   existing-resource ownership conditions. Its grant requires Sydney, the fixed
   Project/Layer request tags and only Project/Layer/Lifecycle tag keys, but can
   retag other Cloud Map resources in this account. The owner approved this
   exception on 2026-10-20 because this account is exclusively for LanguageWise.
   Independent cleanup does not receive the grant. Do not reuse this permission
   design in a shared account without a different provisioning boundary.
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
   releases manually, retaining all twenty-one digests for current/previous
  compatible manifests. Keep ECR within 10 GB and source/state/artifacts within
  5 GB; versioning/lifecycle are not byte quotas. No paid scanning, NAT, resolver
  endpoint, premium insight or customer-managed encryption key is added.

## Verification and Live Gate

Local checks: clean Linux backend-disabled Terraform initialization/validation
with read-only lockfiles, three mocked runtime scenarios, 109 Node
control/release/expiry/contract tests, eleven Python initializer tests, actionlint,
618 affected API tests, initializer image build, and all six frontend image/nginx
checks. The repaired Ruff and secret-scan checks also pass on the affected files.
These do not prove current GitHub CI status, live AWS permissions or operation.

Before using this for a presentation, perform two complete Up/Down cycles. Verify
seed accounts/login, Secure cookies, all remote pages, durable writes during task
replacement, authenticated Garry streaming, private feature tools/docs retrieval,
and authorized-user boundaries. Preserve disabled-service behavior for local CI.
Exercise failed Up, an unhealthy release/rollback, expired lease, a merge while
Down, a merge while Up, and independent cleanup with the GitHub key disabled.
Confirm all paid residuals are gone, measure transition time/idle charges, and
recost after the first two cycles. Do not claim these live rehearsals passed on
the strength of mocked plans or static contracts.
