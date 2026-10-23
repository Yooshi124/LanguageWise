# AWS MCP/RAG and Reliable Redeployment Plan

Status: implementation verified locally; Down-only foundation/cleanup cutover
verified, 2026-10-22. The owner authorized local changes/tests/commits and scoped AWS changes,
but not pushing, merging or another Up. Resource, lease and budget limits are
unchanged. The original [AWS migration plan](aws-migration-plan.md) is historical
context; this implementation extends its explicit MCP/RAG exclusion.

### Verified Progress

- Native AWS provider 6.69.1, synthetic refreshed state, no AWS calls: the old
   empty custom-health block plans delete/create; omitting it plans no-op. The
   runtime now omits it. The original incident plan remains unavailable.
- Version 2 requires 20 services/21 images. Legacy 19-image manifests remain
   readable and destroyable, including repeated Down, but cannot start a new demo
   or migrate a live legacy stack. No old archive/control pointer was rewritten.
- Private MCP/RAG ports, caller edges, exact injected secrets, Python RAG health
   probe and five Terraform scenarios pass. Controller/foundation/release/gateway
   checks pass; 26 real-container E2E checks and ten RAG unit tests pass.
- The final host-compatible image, isolated offline at 0.5 CPU/1 GiB, cold-started
   in 23.4 seconds and answered five searches in 182-781 ms without OOM. Server
   peak RSS was 312 MiB; peak cgroup memory was 690 MiB (including page cache and
   other processes). These measurements supersede intermediate implementations;
   actual Fargate pressure/headroom and timings remain a live-rehearsal gate,
   not proven by Docker.
   Model cache is 166.5 MiB, index 1.02 MiB and uncompressed image 1.13 GiB.
   ONNX inference is single-threaded; ingestion/reopen retain the original
   serialized embedding identity/configuration. Real collection-query dispatch
   and old-index reopening have regression coverage. CI checks this resource envelope.
- Fresh AWS checks: Down at revision 46, no lock, full residual proof passes,
   regional Fargate quota remains 20 vCPUs. Terraform parallelism two bounds this
   runtime to 11 vCPUs during normal rolling deployment, versus 10 steady-state;
   the initializer is stopped before application startup.
- Preparation created four new roles, two execution policies, six exact MCP-key
   consumer-policy updates and the `languagewise/demo/mcp-api-key` metadata entry.
   The owner populated the raw-plaintext value directly; only current-version
   metadata was checked. Exact execution-key access, no task-role policies and no
   RAG secret access were independently verified.
- Completed the Down-only cutover: two exact PassRole policy updates and four
   inline source/metadata policy updates; 44 positive/negative IAM simulation
   decisions pass. GitHub, Terraform outputs and both CodeBuild contract variables
   now match the 21-role/nine-secret-ARN inventory. All other project settings,
   schedule, role, limits and timeouts were preserved.
- Cleanup archive from implementation commit `e27d053` has SHA256
   `a136f0ab33c8731cc753d98ce692b6c35eabdcff4742c9d25ca597193dc88018`,
   version `bGf.5Uq7XS_8A3.PJ13CNqYxk.74efoa`, 26,154 bytes and AES256 encryption;
   its exact-version download was verified. Native build
   `7457872b-8928-47a2-baf5-b4b5f2f19880` succeeded while already Down. Fresh full
   residual/readiness checks pass; revision 46 and no lock remain unchanged.
   All four alarms are OK/actions enabled; dispatcher invocation started no build.
   This is not proof of nonempty-stack destruction or an active rolling update.
- Final non-applied preview has zero IAM/CodeBuild changes; unrelated ECS cluster
   drift was not applied. Owner PR publication and exact merged-SHA CI/release
   are next while Down. Cost/storage review and explicit approval still precede
   any new Up; no secret value, runtime start, quota or account-plan change occurred.

## 1. Outcome and Boundaries

Deploy the existing MCP and RAG containers privately alongside the AWS demo,
preserve Compose and host-only development, and make a tested image update work
while the demo is Up. Keep the existing public CloudFront gateway, RDS databases,
single-replica Garry, private ALB, restricted task ingress and independent cleanup.
Do not add public MCP/RAG endpoints, NAT, EFS, a hosted vector database or a GPU.

Apply the owner-approved 2026-10-23 ten-hour lease from the Up request and
twelve-hour admission reservation, with ongoing 20-cycle/240-reserved-hour monthly
bounds. Preserve the one-hour startup deadline, $160 overall budget and separate
OpenRouter allowance. Historical cost estimates are not a revalidated quote for
240 reserved hours. Any further size, quota, timeout or budget
exception needs separate approval. A working plan does not guarantee those limits
will accommodate the additional workloads; measure before enabling them.

The implementation has two gated workstreams: repair discovery/redeployment first,
then integrate MCP/RAG. Use the same repaired provider/runtime baseline for both.
Until the live update gate passes, keep the temporary Down -> merge/release -> Up
procedure. A first successful Up is not evidence that rolling updates are safe.

## 2. Evidence and Open Findings

| Observation | Consequence |
| --- | --- |
| Up `39424991893` on `a5366e4` succeeded; the owner exercised app access, AI and database reads. | Preserve the working basic-AI and database paths while adding features. |
| Release `39426732157` on `d220f4c` hit 18 Cloud Map `DeleteService / ResourceInUse` errors, then the same 18 during rollback. No ECS `UpdateService` occurred in the fully paginated failure window. | Fix infrastructure reapply before blaming the new app image or adding IAM permissions. |
| The merge changed no runtime Terraform; both failures preceded lease expiry and the queued Down request. | Neither that Down request nor lease expiry explains the initial failure. |
| The previous [discovery definition](../infra/demo/services.tf) declared an empty `health_check_custom_config`; provider 6.69.1 reproduces replacement against absent refreshed state. Successful create requests omitted custom-health configuration. | The empty block is removed and an offline native-provider CI regression requires no-op for omission; the original incident plan was not retained. |
| The original saved replacement plan was not retained. Thirty-six successful create audit events refer to only 18 IDs. | Do not claim that duplicate audit events prove replacement, or that the exact replacement attribute is already established. |
| Fallback teardown and queued Down `39430924116` succeeded; the last read-only check proved Down, no lock and no runtime residuals. | Recheck before any future live operation; this dated observation is not current-state authorization. |
| Cleanup build `d89d1c72-0cef-474f-9a49-40806664e7e4` failed while release rollback held its lock. Its exact exception was suppressed. | Lock contention is supported by timing and guard tests, not directly proven by the retained error log. Preserve lock safety while improving diagnostics. |

Current contracts and implementation anchors:

- [Compose](../docker-compose.yml), [MCP](../mcp-server/README.md) and
  [RAG](../rag-server/README.md): both containers run locally; host access is loopback-only.
- [Controller contracts](../infra/scripts/control.mjs),
  [release builder/gate](../infra/scripts/release.mjs),
  [foundation identities](../infra/foundation/identity.tf) and
   [runtime services](../infra/demo/services.tf): version 2 includes private MCP/RAG;
   deployed cleanup now uses the matching contract and retains legacy-release destruction support.
- [MCP JWT loader](../mcp-server/src/LanguageWise.McpServer/Security/UserTokenValidator.cs)
  already supports `Auth__VerificationKeyPem` with file fallback; reuse it.
- [Real-container E2E](../tests/e2e/README.md) already exercises scoped MCP tools,
  docs retrieval, user-specific writes, key/JWT boundaries and outages with a fake
  external text-generation provider. Reuse this suite rather than duplicate it.

## 3. Proposed Runtime Contract

The target is 20 one-replica application services and 21 immutable release images:
the current 18 services, `mcp-server`, `rag-server`, and the existing initializer
image. There is still one RDS instance and one one-off database initializer.
Execution/task role maps must cover all 21 image keys, not just running services.

| Service/caller | AWS configuration | Local compatibility |
| --- | --- | --- |
| MCP | `http://mcp-server.languagewise.internal:8080/mcp`; `/health` on 8080; stateless HTTP. | Preserve Compose port 8200 -> 8080, file secrets, and host defaults. |
| RAG | `http://rag-server.languagewise.internal:8100`; REST `/query` and verified-index `/health`. | Preserve Compose port 8100, named data/cache volumes, and host paths. |
| Garry and five feature backends | Enable MCP; set the private endpoint and inject `Mcp__ApiKey`. | Keep existing `Mcp__ApiKeyPath` fallback and mounted local key. |
| MCP downstream clients | Set every feature API URL to its private backend DNS name; set `Services__Rag` to private RAG. | Keep Compose URLs and host-only defaults unchanged. |
| Shared backend | Enable RAG and set `Services__Rag`; retain its existing Garry/database settings. | Preserve Compose RAG-enabled configuration and host development. |

### Networking, identity and secrets

1. Add explicit dependency edges: Garry + five feature backends -> MCP; MCP -> five
   feature backends + RAG; shared backend -> RAG. Authorize destination ports using
   source security groups, not broad VPC/public ingress. These request edges form
   cycles: do not translate all of them into Terraform startup `depends_on` edges.
2. Create one AWS-specific MCP API-key secret in the retained foundation. Generate
   and populate its value outside Terraform; never copy a developer key or expose
   a value in state, release manifests, workflow variables, plans or logs.
3. Grant exact execution-role secret reads to MCP and its six callers. MCP also
   receives the existing signing public key, never its private key or OpenRouter
   key. Ordinary task roles and release/control roles need no new secret reads.
4. Preserve API-key, signed user-token and tool-scope checks, including downstream
   user-JWT forwarding and explicit-consent requirements for preference writes.
   Private networking does not replace application authorization.
5. General RAG queries currently have no application key: restrict ingress to MCP
   and shared. Keep technical retrieval and RAG's own MCP endpoint out of public
   gateway routing. Cloud-hosted agentic-loop/technical clients are out of scope;
   do not distribute the task-generated technical key or add an admin bypass.
6. Keep internal HTTP consistent with the existing VPC design; do not claim it is
   end-to-end encrypted. A new internal-TLS requirement needs a separate design.

### RAG storage, readiness and sizing

- Prefer a rebuildable, per-task index on writable ephemeral storage. The bundled
  corpus is the source of truth; no user database belongs in Chroma. Keep local
  named volumes intact, but do not assume a Compose volume becomes AWS storage.
- Benchmark a genuinely cold image/task: model download, indexing, collection
  verification, startup time, disk, peak RSS and query latency at 0.5 vCPU/1 GiB.
  RAG is CPU-local; it needs no GPU or paid embedding API key. Do not assume it fits.
- Prefer reproducibly baking the validated embedding-model cache into the image
  if this removes unreliable startup downloads at acceptable image/build cost.
  Verify model revision/license and artifact integrity. Never bake generated keys,
  development caches, user data or credentials. Keep startup's index verification.
- If a runtime download remains necessary, explicitly test clean outbound access,
  failure/retry behavior and cold-start deadlines; a warm Compose volume is not
  evidence of a working Fargate cold start. Prove recovery after replacing a task
  with empty storage, not merely restarting a process with its cache intact.
- Define service-specific ports, container health commands and measured startup
  grace. RAG needs its Python HTTP probe on 8100, not the generic curl/.NET probe
  on 8080. Dockerfile health checks and Compose `depends_on` are not automatically
  ECS task health checks or cross-service readiness gates.
- Gate functional readiness in the controller: indexed RAG retrieval, MCP protocol
  exchange and a user-authorized tool, then browser-facing feature checks. Bound
  retries/timeouts; avoid startup cycles between MCP, Garry and feature backends.

## 4. Workstream A: Make Reapply Safe and Observable

1. Reproduce with the locked provider before changing the suspect block. Use a
   synthetic refreshed-state/schema-backed regression to distinguish absent,
   empty and explicit custom-health configuration. A mocked provider test alone
   cannot establish actual refresh behavior. If needed, propose a minimal,
   separately approved live canary with a hard lifetime/cost and cleanup proof.
2. Capture only a sanitized plan summary: resource types, action kinds and fixed
   replacement paths. Keep raw plan/state/diagnostics private and ephemeral; never
   upload them to public Actions artifacts. Reject unexpected discovery, database,
   network or secret replacement in an image-only rollout before applying it.
3. Choose the smallest fix supported by the reproduction. Omit the empty block if
   that preserves the intended and actual ECS registration/health behavior; if
   custom health is required, choose a tested explicit representation compatible
   with the pinned provider. Verify returned health behavior and stable refresh.
   Do not hide drift with broad `ignore_changes`, force-deregister live instances,
   set `force_destroy` to bypass the error, or broaden IAM for `ResourceInUse`.
4. Check both startup applies, unchanged refreshed reapply, and an image-only
   update. Discovery names/IDs/ARNs must remain stable for the latter two. The
   update may replace task definitions and roll ECS tasks, not discovery or RDS.
5. Extend safe diagnostic classification to discovery resources and `ResourceInUse`
   with summary-only/detail-only/redaction tests. Preserve unknown fail-closed
   behavior and suppress raw messages, values and resource identifiers.
6. Distinguish a verified healthy active lock from an actual failed teardown in
   cleanup reporting. First reproduce the overlap; do not assume the historical
   exception. A busy worker may defer only with durable Down intent, a verified
   bounded owner and a scheduled retry. Preserve the existing lock/CAS rules,
   termination proof, expiry, dispatcher and alarms. Do not convert unknown errors
   into success, steal a live lock or simply silence failed-build notifications.

Exit gate: demonstrated stable provider refresh and successful live image update,
plus rollback/Down safety. Offline tests remain necessary but are not live proof.

## 5. Workstream B: Extend AWS Without Breaking Local Development

1. Reuse the existing Dockerfiles and tests. Add explicit image-build cases for
   `mcp-server` and `rag-server`; the current frontend/backend folder inference is
   not valid for these names. Build Linux/amd64, scan/review dependencies, check
   non-root execution and resource usage, and deploy exact ECR digests.
2. Update all inventory authorities together: controller service/image lists and
   validation, Terraform foundation/demo variables and locals, secret consumers,
   IAM PassRole/resource scopes, health/ports, release building and smoke tests.
   Add contract tests for the exact 20-service/21-image set and reject extras.
3. Add `rag-server.yml` and `ai-e2e.yml` to the exact-SHA publication gate; audit the
   full workflow dependency closure, including Garry. Verify workflow events,
   branch/path filtering, failure and superseded-run handling. Never replace these
   checks with a generic completed/green repository status.
4. Keep development and deployment configuration separate. File-backed keys and
   host/Compose DNS remain valid locally; AWS supplies PEM/key values through ECS
   secrets and private URLs through environment settings. Add regression coverage
   for both paths and for explicit MCP/RAG-disabled operation.
5. Preserve RAG's public/general vs technical-content separation, result bounds
   and relevance behavior. Test reindexing after a corpus/image change, empty or
   missing corpus, unavailable model download and insufficient retrieval context.
6. Exercise per-user authorization and tool scopes end to end. A caller outage
   must produce a bounded safe failure without a retry/tool recursion storm.
   Check streamed tool/results and docs answers through the real HTTPS gateway;
   unit tests or a root HTML 200 are not sufficient.

## 6. Cutover, Cost and Approval Gates

### Down-only contract migration

Do not merge a new validator and let it race an old foundation contract. Plan a
reviewed maintenance window with authenticated release/Up dispatches paused or
blocked by an explicit readiness gate. Keep independent expiry and alarms enabled.

1. Verify Down, absent lock and complete residual cleanup. Preserve existing local
   Compose workloads/volumes. Review the actual current IAM, quota, source pins and
   account plan without fetching application secret values.
2. Design a versioned release/control compatibility path. Existing immutable
   manifests and persisted control metadata contain 19 images. Merely changing the
   expected count to 21 can break `readRelease`, previous-release comparison,
   repeated Down and cleanup before the new manifest is published. Test migration
   from real-shaped legacy metadata, retain historical immutable objects and
   sequence ordering, and require the new contract for new Up operations. Any
   control-pointer migration needs reviewed conditional writes and verified Down;
   do not manufacture Down or accept arbitrary incomplete manifests.
3. Prepare reviewed foundation changes for both role pairs and secret metadata.
   Provision the MCP value through the secure operator process. Inventory current
   IAM scopes and policy sizes; grant no routine IAM writes. Preview and approve
   exact changes; exclude the previously observed unrelated ECS cluster drift.
4. Package committed/reviewed compatible cleanup source, upload create-only with
   checksum/version proof, and update source permissions/pins. Coordinate the
   CodeBuild source and embedded foundation configuration in the same project
   update: old/new validators, secret maps and role maps must agree. Preserve all
   other project settings, time bounds, trust and non-source policy statements.
5. Update the nonsecret GitHub foundation contract, including both new role-map
   entries and MCP secret ARN. Verify BOM/whitespace handling and exact contract
   equality with CodeBuild. Run native cleanup/readiness checks while Down and
   verify that the dispatcher starts no unnecessary build.
6. Publish the exact tested 21-image release while Down, verify its SHA/digests and
   compatibility migration, then reopen the workflow gate. Only after all gates
   pass, obtain authorization for a costed live rehearsal. Updating documentation
   or merging a PR is not permission to start another Up.

### Cost and capacity

- At the existing target size, 20 services reserve 10 vCPUs/20 GiB. A simultaneous
  100% rolling overlap would consume 20 vCPUs before any extra task or workload;
  the last verified regional quota was 20. Recheck it and budget rollout/initializer
  headroom explicitly. Prefer bounded rollout concurrency; do not raise quota or
  task sizes silently. Container memory usage is a measured gate, particularly RAG.
- Using historical Sydney rates from the original plan, two extra 0.5-vCPU/1-GiB
  tasks plus public IPv4 for 40 hours add approximately
  `2 * 40 * (0.5 * 0.04856 + 0.00532 + 0.005) = USD 2.77` in steady-task cost.
  This is not a new price quote or a complete budget approval. Recost current
  micro RDS, additional rollout overlap, image/model storage and pulls, startup,
  logs, the MCP secret, transfer, cleanup, provider calls, tax and account usage.
- Retain the OpenRouter limit and tool-call bounds. Extra retrieval/tool rounds
  can increase generation usage even though embeddings run locally. Budget alerts
  are not hard caps. Obtain acceptance of the revised envelope before live work.

## 7. Verification and Delivery Order

| Stage | Required evidence before proceeding |
| --- | --- |
| A: discovery repair PR | Reproduced drift or explicitly unresolved blocker; stable refreshed plans; unchanged/image-only update regression; diagnostic redaction and lock-safety checks. No paid canary without approval. |
| B: local MCP/RAG + cloud-contract PR | Existing isolated tests and `pwsh tests/e2e/Invoke-E2E.ps1` pass; Compose and host modes retain key/path behavior; both images build; cold-start/resource measurements recorded; versioned inventory/migration tests pass. |
| C: foundation/cleanup cutover | Reviewed exact IAM/config delta, new secret current without value exposure, immutable cleanup provenance, native Down verification and exact-SHA release publication while Down. |
| D: approved live rehearsal | Fresh 20-service Up; five feature tool scopes, docs retrieval and user boundaries pass through the public gateway; real OpenRouter smoke within allowance; recorded cold-start/RSS/readiness timings. |
| E: active-demo redeploy | Apply a new tested image/corpus release, not a no-op; stable discovery identities/RDS and user data, no initializer rerun, functioning MCP/RAG and browser flows. Test bounded rollback without wiping the active database. |
| F: teardown/recovery | Explicit Down and expiry/worker-overlap rehearsals verify cleanup/no lock and no paid/network residuals; reconstruct RAG on the next fresh task; all alarms remain armed and alert delivery/state is checked. |

Reuse the existing foundation/controller/release tests, Terraform tests, MCP/RAG
unit suites and real-container E2E helpers. Extend fixtures for 20/21 identities,
legacy state, real readiness and unsafe replacements; do not create a parallel
test framework. Keep offline/mock evidence distinct from native AWS verification.

After successful gates, update the [AWS runbook](../infra/README.md),
[demo contract](../infra/demo/README.md),
[image contract](../infra/images/README.md),
[foundation runbook](../infra/foundation/README.md) and service/root documentation
to remove obsolete exclusions and record actual limits, source pins and rehearsal
results. Retire the no-merges-while-Up workaround only after Stage E passes.

## 8. Remaining Operator Gates

- Accept the measured MCP/RAG sizing and revised whole-account spending envelope;
  decide whether deterministic baked model caching is viable on the CI runners.
- The discovery trigger is reproduced locally and the smallest fix is tested.
   Confirm actual live reapply behavior; keep cleanup contention failure/alarms
   intact rather than treating an unverified busy writer as successful teardown.
- Review the legacy release/control migration and coordinated foundation/cleanup
  maintenance window; assign the repository/environment administrator and secure
  MCP-secret population owner.
- Approve the scope, spend and reservation usage of any live canary and final
  rehearsal, separately from permission to implement or merge code changes.
