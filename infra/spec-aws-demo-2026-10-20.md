# Spec: AWS Demo Foundation and Controls

**WI / Task**: Terraform foundation and GitHub Demo Up/Down
**Date**: 2026-10-20
**Status**: READY

## Executive Summary

Implement separately owned bootstrap, retained foundation, and disposable runtime Terraform roots, with credential-free CI and trusted GitHub release/demo controls. AWS-native cleanup must use the same control lock and demo state independently of GitHub. No local apply, permission changes, secret-value access, or account upgrade is part of implementation.

## Context & Assumptions

- Owner reports the budget created and approves the cost proposals on 2026-10-20. Record this as owner confirmation, not new API verification.
- Account 047754151573, Sydney, repository Yooshi124/LanguageWise, trusted branch master, GitHub environment aws-demo.
- Existing IAM user/validation role, ECS cluster languagewise, and ECR 41026release2/languagewise require deliberate adoption. Never manage access keys or application-secret values in Terraform.
- Monthly reservation: ten cycles, forty billed hours, two-hour presentation lease plus two-hour transition reservation per cycle. Reservations are not refunded after failure. Repeated Up cannot renew a lease or reseed a running database.
- MCP/RAG excluded. Eighteen application tasks at 512 CPU/1024 MiB, private RDS db.t4g.small PostgreSQL 18/gp3, private ALB and CloudFront VPC origin, public-egress/private-ingress task ENIs. No NAT or paid endpoints.
- Existing application configuration changes needed for actual AWS execution must preserve local defaults.
- Pipeline specialist agents/cw-testing/test-affected skills are unavailable in this workspace; use available agents and repository-native validation rather than CargoWise-only conventions. Prior approved plan supplies research/specification; no new architecture research stage is needed.

## File-by-File Changes

### infra/bootstrap/ — New

**Purpose**: Operator-owned S3 state storage with encryption, versioning, public blocking, TLS-only policy, deletion protection, native S3 lock support.
**Changes**: Pin Terraform/provider; document local-state bootstrap and backend migration. Never include bootstrap in Down.
**Rationale**: A remote backend cannot create its own initial bucket.

### infra/foundation/ — New

**Purpose**: Retain IAM roles, secret metadata, image repository/cluster adoption, DynamoDB control state, code/artifact storage, and independent scheduled cleanup.
**Changes**: Separate administrative foundation from deployment/control roles; exact IAM user trust and ECS PassRole allowlist; secret injection execution roles, no deploy secret reads. Publish cleanup from a pinned, hashed S3 bundle. Existing access keys remain unmanaged.
**Rationale**: Teardown cannot revoke its own execution path or retain runtime cost accidentally.

### infra/demo/ — New

**Purpose**: Own only disposable network, discovery, RDS, ALB/CloudFront and application resources.
**Changes**: Consume nonsecret foundation outputs and immutable image manifest; create infrastructure, run an explicit initializer task, then services; skip final DB snapshot/delete backups. No IAM mutation in the demo root.
**Rationale**: Terraform remains authoritative; application rollout must not compete with imperative ECS mutation.

### infra/scripts/ and infra/tests/ — New

**Purpose**: Shared release/control/cleanup logic and offline regressions.
**Changes**:

    acquire durable exclusive lock with bounded lifetime; never steal from a live writer
    up: validate release and cleanup readiness; reserve monthly allowance atomically
        record desired up and expiry before any paid apply
        apply infrastructure; initialize empty databases; apply services; smoke; report URL
        if failure: preserve expiry eligibility and attempt down
    repeated up: do not extend expiry, charge another reservation, or reinitialize
    down: record desired down first; acquire shared lock; destroy only demo state
        verify residual resources; retain failure marker if cleanup incomplete
    scheduled cleanup: if startup deadline/expiry elapsed or desired down has residuals
        perform identical down operation using AWS service credentials
    merge: validate exact SHA and publish digests; under lock deploy only if desired up
        if down: update release only, never provision
    each phase: check deadline/mode under lock; release lock in finally

**Rationale**: GitHub concurrency alone does not coordinate independent AWS cleanup.

### .github/workflows/aws-*.yml — New

**Purpose**: Credential-free infrastructure checks, exact-SHA release validation/publication, manual Up/Down.
**Changes**: Trusted master-only aws-demo jobs, pinned actions, key-to-scoped-role assumption without OIDC/session tags, common concurrency and durable control lock, no PR credentials, no plaintext plan artifacts. Offline validation precedes credentials.
**Rationale**: Merges while down must not restart paid infrastructure; manual operation needs no workstation.

### infra/README.md, .gitignore, docs/aws-migration-plan.md — New/Modified

**Purpose**: Safe bootstrap/adoption and operator runbook, current implementation status, state/plan ignores.
**Changes**: Explain explicit setup, secure out-of-band secret population, cleanup code publication, variables and two-cycle rehearsal. Preserve user edits and distinguish local checks from live AWS verification.
**Rationale**: Code alone is not proof of deployed/operational infrastructure.

## Edge Cases

| Scenario | Expected Behaviour |
| --- | --- |
| Missing foundation, secrets, or tested release | Fail closed before paid provisioning. |
| Competing GitHub/AWS writers | Only one durable lock owner; no expired-lock theft from an unverified live writer. |
| Failed Up or partial destroy | Expiry remains armed; report failure and retain cleanup eligibility. |
| Monthly quota exhausted | Refuse Up; Down remains available. |
| Merge while down / stale release | Publish only / refuse rollback to an older release. |
| Repeat Up | No lease extension or destructive seed. |
| GitHub key unavailable | AWS service-role cleanup still operates. |
| Terraform secret input | Reject/avoid plaintext; use secret ARNs and out-of-band initialization. |

## Test Plan

- Terraform fmt/init without backend/validate for each root and committed provider locks.
- Node built-in offline tests for control transitions, quotas, locking, repeated Up, expiry, stale release, failure paths, immutable image validation, and disabled-service config.
- YAML/action-pin/trusted-branch checks and script syntax checks without AWS credentials.
- Security review of IAM boundaries, secrets, storage/network exposure, cleanup provenance, and concurrent writers.
- Live apply, two full cycles, disabled-key cleanup, and browser smoke remain explicit operator deployment/rehearsal gates, not claimed local successes.

## Blockers

| # | Question | Why it blocks | Owner |
| --- | --- | --- | --- |
