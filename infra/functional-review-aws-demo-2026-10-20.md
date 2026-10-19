# Functional Review: AWS Demo Controls

**WI / Task**: Terraform foundation and GitHub Demo Up/Down
**Date**: 2026-10-20
**Spec**: [Approved implementation contract](spec-aws-demo-2026-10-20.md)
**Implementation**: [Operator runbook](README.md); local changes only, no AWS apply.

## How This Change Solves the Requirements

Three separate roots keep retained administrative infrastructure outside the
disposable state. `DemoController.execute` combines revision-checked mode changes,
monthly reservation, an exclusive durable lock, and exact saved Terraform plans.
First Up runs the initializer before enabling services; repeat Up does not reseed.
Down records cancellation before waiting for the writer. Expiry is dispatched by
AWS Lambda into source-pinned CodeBuild, independently of GitHub credentials.

`release.mjs` gates publication on exact-SHA CI and records nineteen immutable
digests. Publishing while Down does not provision. Same-SHA redispatch reuses the
original manifest. Rollout failures attempt the previous compatible release.
App PEM inputs, restricted forwarded headers, disabled RAG, configurable nginx DNS,
and verified RDS certificate packaging make the task configuration executable
without replacing local Compose defaults.

## Scope of Changes

| Surface | Change Type | Purpose |
| --- | --- | --- |
| bootstrap/foundation/demo roots and provider lockfiles | New | State isolation, retained controls, disposable eighteen-task runtime |
| controller, release, expiry, source packaging, initializer and tests | New | Durable lifecycle, independent cleanup, immutable release/seed execution |
| three AWS GitHub workflows | New | Credential-free checks, post-merge release, manual controls |
| initializer/CA-wrapper Dockerfiles | New | Explicit database bootstrap and verified RDS TLS |
| six frontend nginx/Dockerfile pairs | Modified | ECS DNS/HTTPS with preserved local defaults |
| auth loaders, shared RAG/proxy configuration and regression tests | Modified | PEM injection, disabled outbound RAG, trusted scheme and Secure cookies |
| root ignores, migration plan and infrastructure runbooks | New/Modified | No committed state/plans/secrets; owner approval and accurate operational status |

### Deliberate Exclusions

No AWS apply or IAM grant, account upgrade, key rotation, secret-value retrieval,
commit/push, domain purchase, MCP/RAG deployment, paid email configuration, or
claim of live notification/deployment success. Existing validation credentials
and user-edited setup/homebox notes are preserved.

## Unseen Impacts

| Area | Impact | Severity | Mitigation |
| --- | --- | --- | --- |
| CloudFront initial tags | Can tag existing completely unowned resources in this account | Administrative review | Explicit runbook boundary; fixed tags/keys, no takeover of owned resources; cleanup excluded |
| ECS task definitions | Non-billed revisions survive Down | Operational | No wildcard deregistration privilege; only paid resources must be absent |
| Native lock recovery | Safety bounds can delay teardown or require manual intervention | Operational | Fixed worker/session/lock bounds, conditional recovery, failure notifications and live rehearsal |
| Schema changes | Image rollback cannot undo SQL | Operational | Fresh-demo initializer only; compatible live migrations remain a deliberate gate |

## Verification

| Check | Result |
| --- | --- |
| Terraform | All three roots fmt/validate PASS; 3 mocked runtime scenarios PASS |
| Control/release/expiry/contracts | 109 Node tests PASS after final idempotent-Down regressions |
| Initializer | 11 Python tests PASS; real container image build PASS |
| Affected APIs | 618 tests PASS across eight projects using an isolated local PostgreSQL fixture; fixture removed |
| Frontend packaging | All six images build; nginx validates ECS and local-default settings |
| Workflow syntax | Checksum-verified actionlint 1.7.7 PASS for all three new workflows |
| Architectural/security review | PASS after remediation, within the documented pre-admin-apply boundary |
| Release/mechanical review | Findings remediated; scoped contracts and workflow lint PASS |
| Remediation applied | Yes: IAM scopes, lock recovery/evidence, missing-object access, manifest retry/SHA binding, canonical smoke/federation checks |
| Live AWS | NOT RUN: authorization, imports, RDS privileges, notifications, disabled-key cleanup, two Up/Down cycles and authenticated browser flows remain gates |
