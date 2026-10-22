# AWS Deployment Plan

Status (2026-10-20): the owner confirms the budget is created and approves the cost proposals. Terraform bootstrap/foundation/disposable-demo roots, GitHub release and Demo Up/Down workflows, and independent expiry controls are implemented locally. PostgreSQL conversion and GitHub-hosted read-only authentication are already verified. **No Terraform apply, new deployment permissions, or AWS application resources have been created.** See the [infrastructure setup and rehearsal runbook](../infra/README.md).

## 1. Recommendation and Decisions

Use Terraform to manage AWS infrastructure and ECS task definitions, with GitHub Actions building images and deploying the tested commit after a PR merges into `master`. Existing workflows in this repository already target `master`, not `main`.

Confirmed decisions:

| Item | Decision |
| --- | --- |
| Region | **Asia Pacific (Sydney), `ap-southeast-2`**. |
| Budget | **Maximum $100 USD/month**, for demonstrations only. Do not assume free-tier credits when costing. |
| Compute and storage | **ECS Fargate plus RDS PostgreSQL**. SQLite-to-PostgreSQL conversion is migration step 1. |
| Task size | **0.5 vCPU and 1 GiB RAM** per application task, initially one container and one replica per service. |
| Public address | Browser-trusted **HTTPS** is required; an AWS-generated URL is acceptable. No domain purchase is required for the proposed CloudFront address. |
| Initial data | Start with **seed data**. No migration of existing local users or progress. |
| MCP and RAG | **Out of scope for now**. A teammate will containerize them; adding them to AWS is a later integration step. |
| Workflow runners | **GitHub-hosted runners** for CI, deployment after merge, and manual Demo Up/Down. The homebox is not required. |
| AWS authentication | **Dedicated IAM user access key in GitHub Secrets**, used only to assume narrowly scoped AWS roles. No personal/root credentials, OIDC prerequisite, or manually renewed workstation sessions. |
| Demo controls | Provide **Demo Up** and **Demo Down** buttons through GitHub Actions, plus automatic expiry to avoid forgotten running resources. |

Use **ECS on Fargate for application containers and RDS PostgreSQL for persistent relational data**. Keep Garry at one steady-state replica because its cross-feature rate limiter is in memory. Basic OpenRouter-backed AI stays in scope; live MCP tools and RAG-dependent features do not.

This is not a direct translation of Compose. The four former SQLite database APIs now use PostgreSQL locally with their HTTP contracts preserved. AWS deployment still requires the remaining infrastructure and configuration work below. RDS is not a container and does not use the requested task sizing.

**Important:** 0.5 vCPU/1 GiB is a valid Linux Fargate task size (`cpu = 512`, `memory = 1024`), not a guarantee that every workload fits. It applies to the whole task, including sidecars. RDS has its own instance sizing and billing. Any task-size exception requires approval after testing; future MCP/RAG tasks need their own sizing and cost review.

### HTTPS Without Buying a Domain

A domain is a readable address such as `demo.example.com`; HTTPS additionally requires a certificate valid for that address. We cannot obtain a normal public ACM certificate for AWS's shared `*.elb.amazonaws.com` ALB hostname, so merely switching an ALB URL to `https://` is not sufficient.

Instead, use **CloudFront's default address**, for example `https://d123example.cloudfront.net`. AWS supplies the browser-trusted certificate for that hostname. No domain registration, Route 53 hosted zone, DNS edits, or custom ACM certificate are required. CloudFront forwards requests to an internal ALB through a VPC origin, then to the shared frontend gateway. Sydney supports CloudFront VPC origins.

The browser connection is HTTPS. The proposed CloudFront-to-ALB hop is HTTP over private VPC-origin connectivity, not end-to-end TLS; do not describe it as encrypted on every hop. Requiring TLS at the ALB as well would need a separate certificate/origin design. Full demo teardown can change the CloudFront URL; Demo Up must always report the current URL.

## 2. Current Deployment Inventory

The root Compose file defines **19 containers**:

| Group | Count | AWS treatment |
| --- | ---: | --- |
| Shared and five feature frontends | 6 | Initially retain nginx containers and the shared same-origin gateway. |
| Shared and five feature backends, plus Garry | 7 | Private ECS services, initially one task each. |
| Shared, mini-games, quizzes/courses, and chat PostgreSQL database APIs | 4 | Retain private API services; point their PostgreSQL connections at RDS. |
| Shared PostgreSQL server hosting all five databases | 1 | Replace with RDS PostgreSQL; review PostgreSQL 18 compatibility and regional availability. |
| Quests PostgREST database API | 1 | Retain privately, pointed at the quests database on RDS. |

Two additional runtime dependencies are outside Compose and **excluded from this deployment**:

- **MCP:** currently a host-run .NET service on port 8200. A teammate owns its containerization; do not provision its service, image repository, MCP key, or downstream configuration now.
- **RAG:** currently a host-run Python/Chroma service on port 8100. A teammate owns its containerization; do not provision its service, image repository, model cache, or index now.

With PostgreSQL moved to RDS and MCP/RAG excluded, the initial design has **18 application tasks plus one RDS instance**. Set `Mcp__Enabled=false` explicitly on all relevant tasks and omit MCP secrets. Add or verify an explicit RAG-disabled mode: the shared backend currently registers a RAG client even when no server exists, so simply removing an address is insufficient. Hide/disable unavailable docs-search and tool controls, or return a clear, immediate unavailable response without host-address attempts/timeouts. Basic Garry completions must still work. Keep these AWS changes independent of teammates' local Compose setup.

Future MCP/RAG integration is a separate change and must revisit runtime sizing, demo controls, tests, and the $100 budget. Frontend consolidation or S3 static hosting is also deferred.

## 3. Cost and Free-Tier Gate

**Do this before provisioning. An AWS free-tier account does not make this stack free.** Eligibility depends on account creation date, plan, credits, region, and service. Check remaining credits and expiration in Billing; confirm whether the account's plan permits every required service and whether upgrading enables paid usage.

Cost this deployment in **Sydney (`ap-southeast-2`)**, not US East. Eighteen tasks reserve 9 vCPUs and 18 GiB RAM while running. Continuous operation is incompatible with the $100/month limit; demonstrations must be time-limited and followed by full runtime teardown.

**Current admission policy, approved 2026-10-23:** 20 starts and 240 reserved hours
per calendar month, ongoing. Each new start reserves twelve hours without refunds
and has a ten-hour lease from the Up request; existing usage and lease deadlines
are preserved. This is controller accounting, not measured billing. The one-hour
startup deadline and $160 planning budget remain unchanged. The original 40-hour
estimate below is historical and is not a revalidated quote for a 240-hour month.

### Verified Rates and Original Monthly Scenario

**Read-only review dated 2026-10-19:** prices below were retrieved through AWS Price List `GetProducts` in `us-east-1`, selecting **Sydney products**, not US East rates. CloudFront uses the Australia pricing geography; Route 53 hosted-zone billing was checked against its official pricing page. No credits, promotional free tier, CloudFront free transfer allowance, or CodeBuild free minutes are deducted. This is an API-backed planning estimate, **not an invoice, approved spend, or a saved AWS Pricing Calculator estimate**.

Historical starting scenario from the 2026-10-19 review:

- **10 Up/Down cycles and at most 40 billed stack-hours/month**, not 40 hours of presentation time. Count startup, teardown, partial-hour billing, and time while any paid runtime resource survives. A two-hour lease does not mean only two billable hours; allow up to four billed hours per cycle and reduce the cycle count if measured transitions exceed that.
- 18 Linux/x86 Fargate tasks at 0.5 vCPU/1 GiB, plus **36 aggregate extra task-hours** for rolling replacements/migrations at that size. Their public IPv4 addresses are included. Use only the included 20 GiB ephemeral storage per task; extra storage changes the estimate.
- RDS single-AZ PostgreSQL on **`db.t4g.small` (2 GiB RAM), 20 GiB gp3**. The orderable-options API confirms PostgreSQL 18 versions including 18.6 on this configuration in Sydney, with a 20 GiB minimum. Performance, connections, and actual provisioning permissions remain separate checks.
- One internal ALB averaging at most one used LCU; CloudFront pay-as-you-go with **20 GB viewer transfer, 100,000 HTTPS requests, and 2 GB sent to origin**, priced for Australia. Different traffic volumes/geographies require recosting.
- Retain no final/manual snapshots or automated backups after Down; no excess backup storage above the active database allowance is modelled. Ten private DNS namespace recreations are conservatively charged as ten full hosted-zone months, even though zones deleted within 12 hours can avoid that fee.
- Bound logs, metrics, image retention, state versions, secret count, and cleanup duration to the quantities below. Basic GitHub-hosted runners are used in the existing public repository; no paid larger runners are included.

| Item | Quantity and verified unit rate / explicit allowance | USD/month |
| --- | --- | ---: |
| Application Fargate | 40 h x (9 vCPU x $0.04856 + 18 GiB x $0.00532) | 21.31 |
| Task public IPv4 | 18 x 40 h x $0.005/address-hour | 3.60 |
| RDS instance and gp3 | 40 h x $0.051 + 20 GiB x $0.138/GB-month x 40/730 | 2.19 |
| Internal ALB and capacity | 40 h x ($0.0252 + 1 LCU x $0.008) | 1.33 |
| Rollout/migration reserve | 36 extra task-hours x (0.5 vCPU x $0.04856 + 1 GiB x $0.00532 + $0.005 IPv4) | 1.25 |
| RDS surplus CPU-credit reserve | Up to 80 vCPU-hours x $0.075; a conservative reserve, not a forecast of actual credits | 6.00 |
| CloudFront traffic and requests | 20 GB x $0.114 + 100,000 x $0.00000125 + 2 GB to origin x $0.08 | 2.57 |
| Private hosted zones | 10 creations x $0.50; no domain purchase or public DNS zone required | 5.00 |
| Cloud Map | $1 allowance for resource registrations/API discovery; catalog rates are $0.10/resource-month and $1/million calls | 1.00 |
| Other data transfer | $2 allowance for cross-AZ and non-CloudFront internet traffic; not a measured traffic quote | 2.00 |
| Logs and basic monitoring | 2 GB ingested x $0.67, 2 GB-month stored x $0.033, 5 GB scanned x $0.0067, 10 alarms x $0.10, 5 custom metrics x $0.30 | 3.94 |
| Retained ECR images | 10 GB-month x $0.10 | 1.00 |
| Secrets Manager | 10 secret-months x $0.40 + 10,000 API requests x $0.000005 | 4.05 |
| S3 state/control/artifacts | 5 GB-month x $0.025 + 10,000 writes/lists x $0.0000055 + 100,000 reads x $0.00000044 | 0.22 |
| Independent cleanup | 600 CodeBuild Linux small minutes x $0.005; no reserved capacity or Docker server | 3.00 |
| Other control-plane operations | $1 allowance for scheduler/notification/control requests, including small billing-query costs | 1.00 |
| **AWS total** | Calculated from unrounded quantities using a 730-hour planning month for prorating | **59.46** |

The steady-state core (tasks, IPv4, RDS/storage, ALB and one LCU) is approximately **$0.7108/hour**, before traffic, monitoring, CPU credits, rollout, and retained costs. The full scenario is about $59.46, not `$0.7108 x presentation hours`. Running just that core continuously for 730 hours would cost **$518.87**, before the remaining items; unattended continuous operation is not affordable.

Useful catalog audit anchors: Fargate CPU/memory SKUs `MMQG9GMCKV4JUBKW` / `RWBFR84TYF5JT44P`; RDS instance/gp3/CPU credits `E4MYB7J8FG6M2YVM` / `XVCEBKZQD46QGPYP` / `8VW5XG9NR7GJRS87`; ALB/LCU `M84CYHYUVJ4USFHD` / `6CFSD9EDEA5XS33F`; public IPv4 `JZP2PURP9VVQ96GH`; CloudFront viewer transfer/HTTPS `8JNNPNBQD9DSHV5K` / `XXEA4SCG9TSQCDF2`; CodeBuild Linux small `T9PF2U65YNNQY9ND`. Retrieved catalog effective dates range from 2025 to October 2026; retrieval date, product geography, units, and tier matter more than assuming every rate changed this month.

### Spending Envelope and Remaining Gates

| Allocation | USD/month before tax | Control |
| --- | ---: | --- |
| AWS scenario | About 60 | Includes runtime, retained resources, cleanup, and the allowances above; measure actual idle/storage costs after first Down. |
| External OpenRouter | At most 10 | Separate provider spending limit; an allowance, not a verified model-token forecast. It is not counted by AWS Budgets. |
| Contingency | 20 | Covers deviations in traffic, billing granularity, cleanup time, and usage. Do not treat it as additional routine demo hours. |
| Planning envelope | **90** | With illustrative 10% tax on the entire envelope, $99 fits within the $100 limit. Actual tax/fees are unverified. |

Using the calculated AWS total rather than rounding to $60 gives **$89.46 before tax** including OpenRouter and contingency, or **$98.40 at an illustrative 10% tax**. This is narrow headroom, not an assumed tax rate: confirm billing tax, currency conversion/payment fees, and other account use before approval. Reduce runtime, provider allowance, or optional monitoring if those exceed this envelope. No NAT gateways, paid interface endpoints, Route 53 Resolver endpoints, premium CloudFront plan, customer-managed KMS keys, paid Container Insights/database insights, WAF, or optional SMTP service fees are included. Adding them requires recosting. Use AWS-managed encryption/S3-managed state encryption where suitable; do not remove security controls to meet the estimate.

**Existing account spend:** Cost Explorer returned estimated October 1-18 usage before credits of approximately **$0.000016** (S3), with other returned usage rows at zero. Credits were shown separately, not used to justify the estimate. This excludes October 19, has normal billing lag, and is not a guarantee of no unposted or future charges.

**Budget status (2026-10-20):** the owner reports the budget created after confirming all eight actual/forecast thresholds and approves the proposals. This supersedes the earlier missing API evidence for implementation approval, not for claims of API verification: read-only `budgets describe-budgets` returned exit code 0 but **no captured JSON body**. The assistant did not create/change a budget; notification delivery and recipient health still need operator rehearsal.

**Owner action before provisioning:** open Billing and Cost Management > Budgets in account `047754151573`. Verify/create an all-services, all-regions monthly USD cost budget, with the $100 outer account warning limit and actual and forecast alerts at **$25/$50/$75/$90**. Check tax inclusion, exclude credits/refunds from the gross-cost view, verify recipients/notification status, and record the settings without sharing private addresses in chat. The $100 AWS warning is not permission to spend $100 on AWS in addition to OpenRouter; the nominal AWS target is about $60. Ordinary budget alerts are not a hard cap and can lag.

The account's new AWS Settings project **spend-limit** feature is separate from ordinary notifications and requires a **Paid Plan**. The current recorded plan is `FREE`; do not upgrade or claim a project hard cap exists merely to complete this review. Owner confirmation of available Billing controls is needed if the Budgets console is also unavailable. Preserve the independent expiry/teardown requirement regardless of notifications.

**Approval gate:** the owner approves the proposals on 2026-10-20, including the 40 billed hours/10-cycle starting scenario. Implementation may proceed; do not ask for the same approval again. No saved AWS Pricing Calculator artifact or new tax/recipient evidence is claimed. Administrative bootstrap review, secure application-secret population, confirmed cleanup notifications and live AWS rehearsals remain required before presenting. The implemented controller reserves four hours per new cycle, refuses exhausted monthly allowances, and does not implicitly renew a lease. Observe and recost after the first two full cycles.

Avoid NAT gateways and collections of paid interface endpoints for this small demo. Use public-subnet Fargate ENIs for outbound access, with tightly restricted inbound security groups; those IPv4 addresses are billed while allocated. RDS and the ALB remain private. If this tradeoff is unacceptable, recost private-task egress before proceeding rather than silently adding NAT.

**Your steps:** enable root MFA and use a non-root administrative identity. Complete the owner checks and approve the cost scenario above. Budgets/alerts are not hard spending caps and billing data can lag; automatic expiry and teardown are the primary safeguards. Credits may offset charges but must not justify an otherwise unaffordable design.

## 4. Step-by-Step Implementation

### Step 1: Convert SQLite to PostgreSQL Locally

**Local status:** implemented and verified against PostgreSQL 18. Compose uses one server with five separately owned databases and restricted logins. Real-engine database/API tests, clean-volume seeding, non-destructive repeat startup, and persistence through application and PostgreSQL container replacement have been checked. Mini-games has no static seed and creates games through its existing API. Local setup and test commands are in the root README; AWS provisioning remains out of scope for this completed local change.

**Implementation:** before AWS provisioning, port the shared, mini-games, quizzes/courses, and chat database APIs to PostgreSQL. Preserve their HTTP routes and DTOs so feature backends keep the same contracts. Update local development/CI database setup as needed without breaking teammates' workflows.

1. Replace SQLite-specific connections, SQL, schema initializers, and seeds with PostgreSQL-compatible equivalents and versioned migrations.
2. Test IDs/sequences, uniqueness, timestamps, transactions, case sensitivity, and repeatable seed initialization; do not assume SQL is portable.
3. Use a local PostgreSQL instance with separate databases/roles for the five data owners, including quests, to rehearse the RDS layout.
4. Start each database from empty storage and seed it. Existing local data will **not** be exported/imported. Reapplying migrations/seeds must not duplicate rows or wipe active-demo changes.
5. Run database/API contract tests and the core signup/login, progress, forum, and achievements flows against PostgreSQL.

**Your steps:** review the seed/demo accounts and confirm they contain no real personal data. No AWS console setup or database export is required for this step.

**Exit check:** all five data owners work against PostgreSQL from a clean seeded environment, and data survives application container replacement.

### Step 2: Inventory the Existing AWS Resources

**You provide:** account ID, ECS cluster name/ARN, ECR repository name/URI, and GitHub repository owner/name. Confirm the existing resources are in `ap-southeast-2`; resources in another region need a deliberate adoption/recreation decision. These are identifiers, not secret keys. Do not send passwords, private keys, or API tokens in chat.

**Identifiers supplied (2026-10-19):** the AWS account, ECS cluster, and ECR repository have now been verified through read-only AWS calls. GitHub repository identity/ownership is user-provided; repository permissions have not been inspected.

| Item | Value |
| --- | --- |
| AWS account | `047754151573` |
| Region | `ap-southeast-2` |
| ECS cluster | `languagewise` |
| ECS cluster ARN | `arn:aws:ecs:ap-southeast-2:047754151573:cluster/languagewise` |
| ECR repository | `41026release2/languagewise` |
| ECR repository URI | `047754151573.dkr.ecr.ap-southeast-2.amazonaws.com/41026release2/languagewise` |
| GitHub repository | `Yooshi124/LanguageWise` ([repository](https://github.com/Yooshi124/LanguageWise)) |

**GitHub coordination:** the repository is owned by a teammate. Coordinate repository/environment administration, GitHub Secrets, Actions permissions, deployment variables, and branch protection with the owner or an authorized administrator. Use a GitHub environment named `aws-demo`, restricted to protected `master`. With an IAM user key, AWS authenticates the dedicated user, not the GitHub repository/branch; GitHub restrictions protect when secrets are released but do not constrain a stolen key. Confirm the available environment protection features before bootstrap.

**Read-only inventory (2026-10-19):**

| Check | Observed result / consequence |
| --- | --- |
| Local tools and identity | AWS CLI v2.38.1 and Terraform are available. STS confirms account `047754151573` through an assumed `AccountFullAccessRole`, not root. The earlier CLI lookup used a stale terminal PATH. |
| ECS cluster | Active, with zero running/pending tasks, active services, or registered container instances. `FARGATE` and `FARGATE_SPOT` capacity providers are associated; no default capacity-provider strategy is set. Container Insights is disabled. |
| ECR repository | Zero images; AES256 encryption; mutable tags; scan-on-push disabled. No settings were changed. Preserve immutable deployed digests regardless of the current tag setting. |
| Existing networking | Only the default VPC, `vpc-0387d00b69d6b923e` (`172.31.0.0/16`), was returned, with three subnets across Sydney AZs `a`, `b`, and `c`. Each enables automatic public IPv4 assignment. Route tables and security-group rules were not audited; do not treat these subnets as deployment-ready. |
| Existing regional runtime resources | No RDS instances, ALB/NLB load balancers, NAT gateways, or Elastic IP allocations were returned in Sydney. This is not an account-wide billing audit. |
| CloudFront | Global distribution inventory contains zero distributions. |
| Fargate On-Demand quota | The account owner increased regional quota `L-3032A538` from **6 to 20 vCPUs**; a subsequent read-only AWS call verified the applied value of **20**. This covers the **9-vCPU** steady-state requirement and up to 18 for full rolling overlap plus migration headroom, subject to other regional workloads. |
| IAM/OIDC access | `iam:ListOpenIDConnectProviders` is explicitly denied by an AWS Organizations Service Control Policy. Existing OIDC configuration could not be inventoried. Creating and assuming the metadata-only validation role succeeded; provider/deployment-role permissions remain unverified. The source role's name does not establish unrestricted permissions. |
| Dedicated IAM user/key check | User `arn:aws:iam::047754151573:user/languagewise/languagewise-runner` exists and can assume only the read-only validation role. On 2026-10-19, one access key was created and its pair installed in `aws-demo` GitHub environment secrets through process memory/stdin, without printing values or writing local files. The GitHub-hosted role assumption and scoped reads passed. OIDC is not required. |
| Budget safeguards | Owner confirms budget creation on 2026-10-20 after the eight-threshold clarification. Earlier CLI responses contained no JSON body; no new API verification or notification-delivery success is claimed. |
| Costs | Owner approves the section 3 $59.46/month AWS scenario and usage proposals on 2026-10-20. Tax/fees, saved calculator artifact, measured transition/idle costs and provider enforcement remain operator checks, not locally verified billing facts. |

**Adoption boundary:** the empty cluster and ECR repository are candidates for deliberate adoption, not yet Terraform-managed resources. Leave the shared default VPC outside the disposable demo state; the proposed dedicated demo network still requires approval. No AWS resources or Terraform state were changed by this inspection.

**Authentication decision (2026-10-19):** use a dedicated IAM user access key stored in GitHub Secrets and GitHub-hosted runners. The IAM user will only be permitted to assume designated release/demo-control roles, which supply temporary credentials to jobs. Merges into protected `master` and authorized manual Up/Down runs must not require the owner's AWS login, a homebox connection, or credentials typed into a running job. The account plan API confirms `FREE` / `ACTIVE`; changing the account plan or enabling OIDC is not a prerequisite for this selected path.

**Superseded homebox setup:** the Debian host/Ubuntu VM and registered self-hosted runner were prepared and the credential-free Runner Smoke Test passed. That diagnostic workflow was retired on 2026-10-20. The setup is not required for IAM-user-key authentication on GitHub-hosted runners. No persistent AWS credentials were installed there. Do not install the new key in the homebox/VM or target it from deployment workflows. The [VM notes](../tools/github-runner/README.md) remain historical setup/removal guidance; unregistering the runner and stopping/removing the VM are separate operator actions, not performed by this plan update.

**IAM validation bootstrap (2026-10-19):** with explicit approval, created `arn:aws:iam::047754151573:role/languagewise/languagewise-runner-validation`. It now trusts exactly the existing workstation role `arn:aws:iam::047754151573:role/managed/AccountFullAccessRole` and dedicated user `arn:aws:iam::047754151573:user/languagewise/languagewise-runner`, has a one-hour maximum session, and permits only `ecs:DescribeClusters` and `ecr:DescribeRepositories` on the known LanguageWise resources in Sydney. The [trust policy](../tools/github-runner/iam/validation-trust.json) and [permissions policy](../tools/github-runner/iam/validation-permissions.json) record this manual bootstrap; it is not Terraform-managed. Earlier role/metadata checks succeeded from Windows and inside the isolated VM using temporary credentials in process memory. No workstation administrative credentials were transferred, and no persistent AWS credentials were installed in the VM. The later GitHub-hosted run below verifies the selected authentication path. No deployment, deletion, application-secret access, image pull/push, or role-passing permissions were granted.

**Dedicated user bootstrap (2026-10-19):** created `languagewise-runner` under `/languagewise/`, without a console password. Its sole inline policy, `AssumeLanguageWiseValidationRole`, is recorded in the [user permissions policy](../tools/github-runner/iam/runner-user-permissions.json) and allows only `sts:AssumeRole` for the validation role above. Live role readback confirms both trusted principals. IAM simulation allows the user to assume the validation role, denies assumption of the broad workstation role, and denies direct ECS/ECR metadata reads. After confirming zero existing keys and no environment secrets, one key was generated and both `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` were installed directly in the protected `aws-demo` environment through GitHub CLI standard input. No key values were printed, committed, or saved locally. Rotate by **2027-01-17** or at handover/exposure, whichever comes first. These bootstrap resources must be deliberately imported into Terraform rather than recreated later; never import or manage the key value in Terraform state.

**Next gate:** implementation and cost approval are recorded. Review/apply the protected two-bucket bootstrap, publish the exact committed cleanup bundle, then review/import/apply foundation and scoped routine roles using the [runbook](../infra/README.md). Keep the validation role unchanged. Populate real secrets directly in AWS, set the nonsecret GitHub foundation variable, test cleanup/notifications, then rehearse Up/Down. No application infrastructure has been provisioned by local implementation.

**GitHub validation completed:** the admin created `aws-demo`; API checks confirm a single custom deployment rule for branch `master`, with no required reviewers or wait timer. `Amber-Cahill` has repository `WRITE` and successfully populated its two environment secrets. The former read-only validation workflow exactly matched the locally validated version before dispatch, including the official AWS credentials action pinned to v6.3.0's commit. [Run 39247442861](https://github.com/Yooshi124/LanguageWise/actions/runs/39247442861) passed on 2026-10-19, verifying role identity and both Sydney metadata reads. The earlier missing-secrets failure is resolved. The diagnostic workflow was retired on 2026-10-20; its removal leaves the AWS credentials and validation role unchanged. The [operator/admin walkthrough](aws-github-setup.md) retains setup history and rotation guidance; current deployment/Up/Down uses the infrastructure runbook instead.

**Implementation:** inspect the cluster's capacity providers, VPC/subnets, permissions, Fargate quotas, and the existing ECR repository. Merely creating a cluster does not create running capacity or task definitions. Check the regional Fargate on-demand vCPU quota: eighteen tasks require 9 vCPUs at steady state, plus migration/rollout headroom. Request an increase before deployment if necessary.

Decide which existing resources Terraform will own. Import adopted resources into state rather than recreating them or accidentally managing them twice. One existing ECR repository can technically hold many tagged images; prefer one repository per custom image for clear permissions and lifecycle rules. Do not delete the existing repository or cluster without checking their use.

**Exit check:** agreed inventory, resource ownership, region, architecture, and affordable cost estimate.

### Step 3: Bootstrap Terraform and GitHub Authentication

**Implementation:** add an `infra/` layout with separately owned foundation and disposable demo configurations/states. The retained foundation contains state storage, the dedicated IAM user and scoped roles/policies, ECR, application-secret metadata, a release/mode record, cleanup automation, and any adopted empty ECS cluster. Manage the access key value outside Terraform. The demo state owns the disposable VPC/network resources, CloudFront/VPC origin, ALB, RDS, discovery, and application services. Explicitly document cross-state outputs so destroying the demo cannot destroy the foundation. Pin Terraform/provider versions and commit the provider lock file. Keep local Compose development working independently.

1. Bootstrap a private, encrypted, versioned S3 Terraform state bucket with public access blocked and native S3 state locking (`use_lockfile = true`, using a supporting Terraform version). The bucket must exist before the application configuration can use it as a backend.
2. Keep bootstrap and application state separate. Protect state and backups: marking a value `sensitive` only hides display; it does not remove it from state.
3. Create the dedicated programmatic-only IAM user, with no console password or administrative policies. Its only identity-policy permission is `sts:AssumeRole` for explicitly named release/demo-control roles. Those roles trust the exact user ARN and grant only the required ECR, state/control, and demo-resource permissions. Do not allow routine deployment jobs to broaden their own IAM permissions or destroy the retained foundation; keep administrative foundation changes separate.
4. Generate the user's access key through an approved secure operator process and enter it directly into GitHub environment secrets `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` in `aws-demo`. Store region and role ARNs as non-secret environment variables, for example `AWS_REGION`, `AWS_DEPLOY_ROLE_ARN`, and `AWS_CONTROL_ROLE_ARN`. Do not paste credentials into chat, source, workflow inputs, logs, artifacts, or Terraform. Do not create an `aws_iam_access_key` resource that would persist the key in state. Do not copy personal/root keys, workstation login caches, or temporary sessions into GitHub Secrets.
5. Run AWS jobs on GitHub-hosted runners. Use a reviewed, commit-pinned `aws-actions/configure-aws-credentials` action with the two GitHub secrets and `role-to-assume` to obtain short-lived role credentials; include the run ID in the role session name for audit correlation. Configure the role duration and refresh credentials deliberately for long jobs, including teardown. No `id-token: write` permission or OIDC provider is needed. All three entry points (merge deployment, Demo Up, and Demo Down) use this authentication without interactive input or a teammate's AWS identity.
6. Restrict `aws-demo` to protected `master`, require reviewed workflow/infrastructure changes, and expose the key only to trusted AWS jobs. PR tests and Terraform validation run without AWS credentials; do not give untrusted PRs authenticated planning access or execute PR code with `pull_request_target` secrets. Restrict `iam:PassRole` to the ECS roles actually used. Anyone authorized to modify/run a secret-bearing workflow can potentially expose its key; masking is not a security boundary.
7. Rotate the key at least every 90 days and at project handover, and revoke it immediately on suspected exposure. Create a replacement, update both GitHub secrets, validate a trusted run, then deactivate/delete the old key. Record an owner for rotation and incident response. Where necessary, revoke affected role sessions as well: disabling the source key alone does not invalidate already-issued STS sessions. Demo Down retains the IAM identity/roles and GitHub Secrets for the next run; project retirement explicitly removes them.

**Your steps:** use your existing short-lived workstation login only for reviewed administrative bootstrap. Coordinate with the repository owner to configure `aws-demo`, its two AWS secrets and non-secret variables, and branch protection/rulesets requiring PRs and passing checks on `master` while preventing routine direct pushes. Do not require manual environment approval for routine post-merge deployment. Authorized teammates use GitHub permissions to run Up/Down; they do not need your AWS credentials or their own AWS login. Enter the secret values directly in GitHub, never chat.

**Exit check:** a trusted GitHub-hosted workflow assumes the expected role and verifies `aws sts get-caller-identity` without a homebox, workstation session, or interactive prompt. Terraform can safely initialize/lock remote state. Validate the secret setup and a key-rotation rehearsal without displaying credentials. Read-only validation must not be mistaken for deployed application infrastructure.

### Step 4: Provision RDS and Seed the Demo

**Implementation:** apply the PostgreSQL work completed in step 1 to the disposable AWS demo environment.

1. Provision one small **single-AZ** RDS PostgreSQL instance, not five instances, with separate databases and restricted roles for the five owners. Select an engine/version and instance class available in Sydney; test whether a burstable class such as `db.t4g.micro` has sufficient connections/memory, and monitor CPU credits. Do not promise it fits before testing. Keep it private in a database subnet group spanning two Availability Zones, restrict ingress, verify TLS, and encrypt storage. A two-zone subnet group is not a paid Multi-AZ database deployment.
2. Run the versioned schema/seed process for all owners, including quests' PostgREST roles/grants. The current quests PostgreSQL 18 image initializes through `/docker-entrypoint-initdb.d`; RDS will not execute that directory automatically. Rotate the hard-coded development database credentials.
3. Run migrations/seeds as an explicit one-off ECS task inside the VPC; GitHub starts it and checks its exit status without direct access to private RDS. Seed fresh databases on Demo Up, not every container restart or live deployment.
4. Use backward-compatible schema changes during an active demo. A container rollback does not undo a schema change.
5. Make this **demo-only** RDS instance disposable: normal Demo Down deletes it and its demo-created data, skips a final snapshot, and removes automated backups according to the explicit cleanup policy. Configure deletion protection accordingly and scope destructive permissions to this demo. Preserve seed/migration sources, not a growing collection of paid snapshots. Any preservation/export is a separate explicit operation.

**Why not simply mount the Compose volumes?** Fargate's ordinary task storage is ephemeral, and Compose named volumes are not AWS storage resources. ECS-managed EBS volumes for service tasks do not provide a simple persistent reattachment of the same database disk across service replacements. EFS is network storage, not a safe drop-in assumption for SQLite locking/WAL; SQLite WAL requires processes on the same host. Do not base production persistence on it without a specifically validated design.

**Your steps:** acknowledge that full Demo Down resets demo-created users/progress and that the next Demo Up starts from seeds. Approve the measured RDS size/cost; no local-data migration is planned.

**Exit check:** data survives application/task replacement while the demo is active, API contract tests pass, and a fresh environment can be recreated from migrations/seeds after Demo Down. No database lives only in a task's writable layer.

### Step 5: Define Networking, HTTPS, and Service Discovery

**Implementation:** Terraform creates a VPC with DNS enabled, public egress subnets for tasks, private subnets for an **internal** Application Load Balancer and RDS, security groups, and ECS network configuration in Sydney. The ALB spans two Availability Zones. Add CloudFront with the default certificate/address and a VPC origin pointing at the ALB. Start with one task per service; this is not high availability just because the ALB spans two zones.

- Expose only CloudFront to browsers. Allow the CloudFront VPC-origin service-managed security group to reach the internal ALB, and only the ALB to reach the shared gateway. Use an `ip` target group for Fargate and `/health` as the gateway health check. Meet VPC-origin prerequisites, including an attached internet gateway and available private-subnet IPv4 addresses. Do not recreate Compose's host port mappings as internet-facing ports.
- Keep backend, database API, PostgREST, and Garry ingress restricted to their actual callers over private IPs, even though task ENIs have public addresses for egress. In particular, Garry's service-supplied rules rely on trusted internal callers, so do not broadly allow every task to call every port.
- Use Cloud Map private DNS names, for example `shared-backend.languagewise.internal`, in application URLs and nginx upstreams. Configure nginx to use the VPC DNS resolver; its current `127.0.0.11` resolver is Docker-specific and will not work on ECS. Preserve runtime re-resolution when task IPs change.
- Remove `host.docker.internal` dependencies from the AWS configuration. Explicitly disable MCP and RAG-dependent functionality until the teammate's services are ready; configure the remaining internal endpoints for AWS.
- Use public-subnet task ENIs with public IPs for image pulls, AWS APIs, OpenRouter, and optional SMTP. Deny internet inbound access through security groups. Avoid NAT and paid interface endpoints in the initial budget; include per-address charges and enough subnet IPs for rollout/migration tasks.
- Require browser HTTPS using CloudFront's default certificate and redirect policy. No custom domain, Route 53, or ACM setup is required. Preserve the external HTTPS scheme across the CloudFront/HTTP-origin/nginx chain; the current nginx `$scheme` would otherwise report HTTP. Trust forwarded headers only from the controlled proxy chain and verify secure authentication cookies.
- Initially disable CloudFront caching for the whole app. Allow all required HTTP methods, forward cookies, query strings, and authorization headers correctly, and prevent caching of login responses, personalized APIs, federation manifests, or SSE. Static-asset caching can be optimized later. Preserve nginx rewrites, remote URLs, SPA fallback, and SSE no-buffering behavior; test CloudFront/ALB/nginx streaming timeouts together.

**Your steps:** open the HTTPS URL printed by Demo Up. No domain purchase or DNS changes are needed. Approve the public-task-egress/private-ingress tradeoff before implementation.

**Exit check:** HTTPS reaches the gateway, all five remotes load from the same origin, internal names resolve after a task replacement, and internal services/databases cannot be reached from the internet.

### Step 6: Configure Images, Disabled Features, and Secrets

**Implementation:** keep existing Dockerfiles/build contexts for the in-scope custom images. Do not add MCP/RAG Dockerfiles or deployment resources in this migration. Explicitly configure and test their disabled states, independently of local development settings. Pin/version third-party images such as PostgREST. Keep CPU architecture consistent between builds and task definitions; use Linux/x86_64 initially unless ARM compatibility and pricing are deliberately tested.

Store OpenRouter, database, optional SMTP, and signing secrets in Secrets Manager or SSM Parameter Store, with least-privilege access. No MCP secret is needed while MCP is disabled. Publish the signing public key as configuration, but keep the private signing key restricted to the shared authentication backend. Use separate AWS-environment credentials rather than copying development secrets indiscriminately.

**OpenRouter key: use AWS Secrets Manager, not GitHub Secrets, for the deployed app.**

1. Terraform creates a secret resource named `languagewise/demo/openrouter-api-key` in `ap-southeast-2` in the retained foundation. Terraform manages its metadata/ARN only, not its value; do not create a Terraform-managed secret version or read the value through a Terraform data source.
2. **Your step:** after bootstrap, enter the API key directly through the AWS Secrets Manager console as a plaintext secret value. Do not paste it into chat, commit it, or supply it as a workflow input. Use a dedicated demo key with an OpenRouter spending limit within the monthly budget.
3. ECS task definitions reference the secret ARN in their `secrets` configuration under the environment variable name `OpenRouter__ApiKey`. Inject it only into the current consumers: `garry-ai-service-backend`, `quests-achievements-notifications-service-backend` (email generation), and `leaderboard-analytics-service-backend`. Other containers and browser bundles must not receive it.
4. Grant the consuming tasks' execution roles `secretsmanager:GetSecretValue` only for that secret, plus narrowly scoped `kms:Decrypt` if a customer-managed encryption key is used. ECS retrieves the value when starting the containers; application task roles do not need secret-read access merely for this injection.
5. GitHub Actions uses the dedicated IAM user credentials in GitHub Secrets to assume its deployment role, and passes only the OpenRouter secret ARN into the deployment. Its deployment role does not need to read the OpenRouter key. The AWS authentication key belongs in GitHub Secrets; the OpenRouter application key does not. Do not duplicate the OpenRouter key there or fetch/echo it in build/deploy jobs.
6. Demo Down retains the secret so the next Demo Up needs no key re-entry. Include Secrets Manager storage/API charges in the retained-foundation budget. Rotation means updating the value securely in AWS and replacing/redeploying affected tasks; existing containers do not pick up changes automatically. Verify an authenticated AI request without logging the key.

The app currently reads signing keys from file paths under `/run/secrets`. ECS secret injection supplies environment values, not Compose secret files. Either adapt the apps to supported secret configuration or add a carefully permissioned startup mechanism to materialize files without logging values. A secret-writing sidecar would share the task's resource budget. Never bake secrets into images or pass plaintext secrets through Terraform variables/resources that persist them in state. Create secret containers/ARNs with Terraform and populate values through a secure out-of-band process.

**Your steps:** enter the real values directly in AWS through an approved secure method; do not commit `.env`, PEM files, Terraform state, or secret-bearing plan files. Confirm SMTP sending credentials, or leave email intentionally disabled. If using SES instead of Gmail, account for sandbox verification and production-access requirements.

**Exit check:** every service starts with only the credentials it needs, and no secret is present in images, source, logs, or Terraform artifacts. Secret rotation includes a new deployment where required; injected secrets do not update running containers automatically.

### Step 7: Create ECS Task Definitions and Services in Terraform

**Implementation:** define a map of services with image digest, port, environment, secret ARNs, health check, and allowed dependencies. Generate task definitions/services from that map while retaining service-specific settings.

For every application task, configure:

- Fargate compatibility, `awsvpc` networking, Linux runtime/architecture, `cpu = 512`, and `memory = 1024`.
- An execution role for ECR pulls, logs, and injected secrets, plus a separate minimal task role only where runtime AWS access is needed.
- CloudWatch logs with bounded retention, explicit ECS container health checks, and service startup grace periods. ECS does not automatically adopt a Dockerfile `HEALTHCHECK`; port the actual probes into the task definition. Garry's Compose-level health check also needs to be declared explicitly.
- Desired count 1 only while the demo is enabled, deployment timeout, and deployment circuit breaker with rollback for each service. A normal rolling deployment needs room for a second task and enough quota/subnet IPs during rollout. Normal Demo Down removes the runtime resources rather than merely setting desired count to zero.
- RDS connections and internal DNS endpoints rather than Compose service names, host aliases, or startup ordering.

Compose `depends_on` does not provide cross-service ordering in ECS. Bring up the data tier first and implement retry/backoff for temporary downstream unavailability. Liveness should not create circular dependency failures; use readiness and deployment checks to establish usable dependencies.

**Exit check:** a manually triggered initial deployment reaches steady state and a deliberate unhealthy application release triggers rollback. Keep a manifest mapping each service to its deployed immutable image digest. Garry's in-memory rate limit resets on restart and can temporarily be split across tasks during a rolling deployment; strict continuity requires distributed state or an explicitly accepted stop-before-start outage.

### Step 8: Add the Merge-to-AWS GitHub Workflow

**Implementation:** add one authoritative deployment workflow, rather than allowing separate feature workflows to race on shared Terraform state.

1. **PR checks:** formatting/validation, application tests, frontend builds, existing security checks, and credential-free Terraform validation. Do not give PR jobs the dedicated AWS key or deploy PR code. Authenticated Terraform plans belong in trusted post-merge/manual jobs. Keep secret-bearing plan artifacts private and short-lived; never use privileged `pull_request_target` to execute untrusted PR code.
2. **Trigger:** `push` to protected `master` after merge, plus `workflow_dispatch` for a controlled first deployment/recovery. Branch protection is what makes a push trigger mean "after PR merge". Do not use `pull_request: closed` to deploy an arbitrary PR head. Check the retained demo-mode record under the same deployment lock: if the demo is **up**, deploy automatically; if **down**, publish/store the tested release manifest but do not recreate runtime infrastructure. Demo Up deploys the latest successfully tested release. This intentionally prevents merges from waking an offline demo and spending money.
3. **Validate the exact release SHA:** run or reuse trusted, SHA-matched test/build/security jobs as explicit dependencies. Existing independent `push` workflows are not automatically gates for the new deployment. Prefer reusable CI jobs or make deploy depend on its own required checks.
4. **Build/publish:** first provision/import ECR repositories through the infrastructure preparation phase so the initial push has a destination. Build custom images from the existing directory-specific contexts, tag with the full commit SHA, push to their ECR repositories, and record immutable digests. Never deploy mutable `latest` tags. Initially build a complete release; optimize changed-service selection later.
5. **Authenticate/plan:** on a GitHub-hosted runner in `aws-demo`, use the dedicated IAM user secrets to assume the deployment role, then run Terraform init/validate/plan with the exact image manifest and enforce the agreed change checks. Authenticate before any earlier AWS operation, including the demo-mode lookup, ECR preparation, or image push. Keep provider/app configuration owned by Terraform; do not also mutate task definitions using a competing ECS deploy action.
6. **Apply:** apply that exact saved plan from the same release run, run compatible migrations in the agreed order, then roll services and wait for steady state. Split infrastructure preparation/migration/service rollout into deliberate phases if a schema prerequisite prevents a single apply.
7. **Smoke test while up:** HTTPS gateway, signup/login, all feature pages/remotes, a durable write/read, course progress/achievements, and an authenticated basic streamed assistant response. Verify MCP/RAG-dependent features are disabled cleanly, without requests to a developer's machine. MCP tool/RAG success tests are deferred until those services join the deployment. Do not print tokens or user data in logs.
8. **Report/recover:** publish release SHA, service health, and URL. ECS circuit breakers handle unhealthy task rollouts, not all functional smoke-test failures: explicitly redeploy the previous compatible image manifest if release smoke tests fail, and investigate schema compatibility before rollback.

Use minimal GitHub token permissions, normally `permissions: contents: read`; do not request `id-token: write` for this key-based AWS authentication. Scope AWS secrets to the trusted jobs in `aws-demo`, not every build/test job. Pin third-party actions to reviewed commit SHAs. Serialize deployments per environment with concurrency and `cancel-in-progress: false` so a newer merge cannot cancel an in-flight Terraform apply. Ensure queued runs cannot deploy an older release over a newer one.

**Your steps:** configure the demo environment without a manual approval gate so deployment after merge is fully automatic. Required PR review/checks and protected `master` remain the control. A future production environment may use a different approval policy.

**Exit check:** merge a harmless PR, observe a single tested release reach AWS, confirm the displayed SHA/manifest, and demonstrate recovery to the previous release.

### Step 9: Add One-Click Demo Up/Down and Automatic Expiry

**Implementation status:** the [AWS Demo Up / Down workflow](../.github/workflows/aws-demo.yml) now supplies a `up`/`down` dropdown on GitHub-hosted runners against protected `master`, using `aws-demo` secrets and the scoped control role. It defaults to Down. The [AWS Release workflow](../.github/workflows/aws-release.yml) serializes merge deployment with those controls. They are implemented but cannot operate until administrative bootstrap and configuration are completed; no live run is claimed. Up gets two hours from the request, including startup, with a four-hour monthly reservation. Repeated Up never extends it; no extension control is implemented.

| Control | Required behavior |
| --- | --- |
| **Demo Up** | Select the latest tested image manifest; record a startup deadline/lease before provisioning; apply the demo Terraform state; create RDS; run migrations/seeds; start the 18 services; wait for CloudFront/ECS readiness and smoke tests; report the HTTPS URL and expiry time. Repeated Up must not reseed/wipe a running demo. |
| **Demo Down** | Mark the desired mode down first; destroy **only the disposable demo state**, including CloudFront/VPC origin, ALB, RDS, ECS services and task ENIs, discovery, and demo networking. Delete demo data/backups according to policy. Retain ECR, state, application secrets, IAM identity/roles, GitHub AWS secrets, cleanup automation, release record, and any adopted empty cluster. Report residual resources and any failed deletions. |
| **Automatic expiry** | Use retained AWS-native scheduled cleanup with its own service roles to run the same serialized Down operation when the lease expires or startup fails. It must not depend on GitHub availability/secrets, the dedicated user's key, a workstation session, or the homebox. Make cleanup idempotent, retry failures, and alert if it cannot finish. A schedule alone is not a guarantee of timely deletion. |

Use the same GitHub environment concurrency group for merge deployment and manual Up/Down. AWS-native cleanup cannot join a GitHub concurrency group; all writers must also share a durable control/lease lock and the Terraform state lock. Check expiry and desired mode while holding the control lock before every provisioning phase, with bounded startup/apply deadlines so a stalled job cannot indefinitely block cleanup. A merge must not undo Down, and cleanup must not race an apply. Clear/retain lease state deliberately after failures so partially created resources are still eligible for cleanup. After teardown, independently check for surviving RDS instances/backups, load balancers, public IPs, endpoints/NAT, and tasks; do not report success just because ECS desired count is zero.

The implemented independent path is EventBridge Scheduler invoking a retained Lambda every fifteen minutes; Lambda reads control state and starts the retained CodeBuild cleanup job **only when due**, avoiding billed idle build starts. CodeBuild receives temporary AWS service-role credentials to read/lock/destroy only the demo state. Fixed lock/worker/session bounds allow verified AWS-native orphan recovery after 150 minutes; malformed/unprovable locks require operator intervention. Teardown/queueing may exceed reservations, so this is not a hard billing cap. See the [runbook's recovery proof and limits](../infra/README.md). Arm and verify the source-pinned cleanup path and confirmed failure notification before paid Up; rehearse with the GitHub key disabled.

**Timing/data tradeoff:** easy means a button, not instant startup. Allow tens of minutes or longer for RDS, CloudFront/VPC-origin creation, and service health; VPC-origin deployment alone can take about 15 minutes. Destruction can also take time. Rehearse and measure it, start well before presentations, and count transition time in the budget. A full teardown may produce a different URL and always resets demo-created data to seeds on recreation.

Do not use RDS stop/start as the default Down implementation: stopped instances still charge for storage/backups and automatically restart after seven days; ALBs cannot simply be paused either. A faster, data-preserving pause can be added later only as a separately costed mode, not labeled "fully down".

**Your steps:** in GitHub Actions, select **Demo Up**, wait for a successful run, and open its reported HTTPS URL. After presenting, select **Demo Down** and check cleanup success. Never assume closing the browser stops AWS charges. Demo data reset is intentional; export anything important explicitly before Down.

**Exit check:** complete two Up/Down cycles; prove seed recreation, no duplicate initialization, cleanup after a failed Up, lease expiry, a merge while down that does not wake AWS, and a merge while up that deploys. Confirm the account's measured idle charges fit the foundation allowance.

### Step 10: Rehearse the Demo and Establish Operations

**Implementation:** rehearse seed-based recreation, compatible release rollback, task replacement, and automated expiry. Check real-browser federation/streaming and disabled MCP/RAG behavior. Add bounded-cost alarms for unhealthy targets, task crashes/OOM, error rates, RDS storage/connections/CPU credits, and spending. Apply short log retention and ECR lifecycle limits while preserving the current and previous compatible releases.

**Your steps:** verify seeded user workflows, record who receives alerts, and choose a demo/shutdown owner. Check the current URL from the successful Up run rather than an old bookmark. Retained images, state, logs, and secrets still have small ongoing costs; review the bill and actual runtime hours weekly during demonstration months.

**Exit check:** a repeatable seeded deployment, tested rollback and full teardown/recreation, successful automatic expiry, and a measured cost profile under the $100 allocation. One replica per service and single-AZ RDS are demonstration budget choices with downtime risk, not production availability guarantees.

## 5. Risks and Launch Gates

| Issue | Evidence / consequence | Required action |
| --- | --- | --- |
| Cost exceeds $100/month | 18 tasks plus RDS/networking cannot run continuously within this budget. | Sydney calculator estimate, bounded demo hours, automatic expiry, full runtime destruction, and idle-cost verification. |
| SQLite persistence does not map to Fargate | Four .NET database APIs use SQLite and `/data` named volumes. | Convert locally to PostgreSQL as step 1, then seed RDS. |
| Lost quests schema/seeds | Current PostgreSQL 18 Docker image runs initialization SQL; RDS does not consume the image. | Run tested schema/role/data migrations explicitly. |
| MCP/RAG excluded for now | Both run outside Compose and use host addresses; teammate owns packaging. | Explicitly disable dependent features, avoid startup requirements/timeouts, and defer integration. |
| Broken internal DNS and remotes | nginx uses Docker's `127.0.0.11` and Compose upstream names. | AWS DNS configuration plus replacement/remote-loading smoke tests. |
| Missing signing files | Applications expect Compose-mounted signing files. | Implement/test cloud secret delivery before first app start; no MCP key is provisioned yet. |
| Resource exhaustion / unavailable capacity | 18 steady-state tasks need 9 vCPUs, plus migrations/rollout capacity; RDS is shared. | Test task/RDS sizing and connection limits; request Sydney quota headroom. |
| Authentication or streaming breaks | CloudFront adds method/header/cookie/cache and HTTPS-forwarding requirements. | HTTPS login, all feature APIs, personalized-data isolation, basic AI streaming, and disabled-feature tests before launch. |
| "Down" still costs money | Zero tasks leaves ALB/RDS bills; stopped RDS restarts after seven days. | Destroy the disposable demo state, verify deletions, and budget retained foundation resources. |
| Demo cannot start instantly | RDS and CloudFront/VPC-origin provisioning take time; complete teardown may change URL. | Measure warmup/cleanup, start ahead of presentations, and report the current URL. |
| Demo data disappears after Down | RDS is intentionally deleted; seeds are the recovery source. | Document reset behavior; export explicitly before Down if needed. |
| Test workflows do not gate deploy | Existing service workflows independently run on `master`. | Make exact-SHA release checks explicit deployment dependencies. |
| Long-lived deployment key exposure | The selected GitHub Secrets key can assume deployment/control roles until revoked; a stolen key is not bound to `master` or the repository. | Dedicated assume-role-only user, scoped roles, trusted GitHub-hosted jobs/environment restrictions, reviewed pinned actions, key rotation/revocation, and independent AWS cleanup. No PR access to the key. |
| Broken rollback after schema change | Task rollback cannot reverse database changes. | Backward-compatible migrations, backups, and a documented recovery decision. |
| Scaling changes behavior | Garry's shared rate limit is in memory. | Keep one replica until distributed state is implemented. |
| Email/AI unavailable | External OpenRouter/SMTP access needs egress, credentials, and provider allowance. | Test real outbound access and quotas without exposing credentials. |

## 6. Suggested Delivery Order

1. **First: convert SQLite to PostgreSQL locally**, preserving API contracts and starting from seeds. MCP/RAG containerization is not a prerequisite.
2. Confirm the Sydney cost estimate fits $100/month at the agreed demo-hour allowance; obtain existing AWS/GitHub identifiers.
3. Bootstrap retained state/IAM/ECR/secrets/controls and adopt existing resources deliberately. Securely add the dedicated user's key to GitHub Secrets and prove role assumption on a GitHub-hosted runner; no homebox or OIDC dependency.
4. Prove one CloudFront HTTPS-to-private-ALB-to-gateway/backend path on ECS at 0.5 vCPU/1 GiB, with seeded RDS.
5. Add the remaining 18-task deployment, discovery, secrets, basic AI, and explicit MCP/RAG-disabled behavior.
6. Add Up/Down, automatic expiry, and merge automation that respects offline mode; rehearse rollback and two full seeded recreation cycles before presenting.

Do not begin by manually creating all task definitions in the console. Once the design decisions are settled, Terraform should create and revise them consistently.

## References

- [Fargate task CPU and memory combinations](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task_definition_parameters.html)
- [Fargate pricing](https://aws.amazon.com/fargate/pricing/) and [AWS Pricing Calculator](https://calculator.aws/)
- [AWS Free Tier](https://aws.amazon.com/free/) and [Fargate service quotas](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-quotas.html)
- [AWS Price List GetProducts API](https://docs.aws.amazon.com/aws-cost-management/latest/APIReference/API_pricing_GetProducts.html)
- [RDS PostgreSQL pricing](https://aws.amazon.com/rds/postgresql/pricing/), [CloudFront pricing](https://aws.amazon.com/cloudfront/pricing/), and [Route 53 hosted-zone billing](https://aws.amazon.com/route53/pricing/)
- [Creating AWS Budgets](https://docs.aws.amazon.com/cost-management/latest/userguide/budgets-create.html) and [AWS Settings project spend limits / Paid Plan prerequisite](https://docs.aws.amazon.com/accounts/latest/reference/create-spend-limit.html)
- [ECS service discovery](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-discovery.html)
- [ECS container health checks](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/healthcheck.html)
- [ECS EBS volume considerations](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/ebs-volumes.html)
- [SQLite WAL limitations](https://sqlite.org/wal.html)
- [AWS credentials action: access keys and role assumption](https://github.com/aws-actions/configure-aws-credentials)
- [GitHub Actions secrets](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-secrets)
- [IAM access key management](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_access-keys.html)
- [Terraform S3 backend and state locking](https://developer.hashicorp.com/terraform/language/backend/s3)
- [CloudFront default-domain HTTPS and AWS-provided certificate](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/using-https-viewers-to-cloudfront.html)
- [CloudFront VPC origins, prerequisites, and supported regions](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/private-content-vpc-origins.html)
- [RDS stop/start charges and seven-day automatic restart](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_StopInstance.html)
