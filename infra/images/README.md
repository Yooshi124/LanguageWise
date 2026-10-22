# Demo Image Build Contracts

Build Linux/x86_64 images. Publish every final image into the foundation ECR
repository and supply immutable `repository_url@sha256:...` values in the release
manifest. Intermediate image tags must never appear in that manifest.

## Application Images

The twenty Compose names other than `shared-db` are the application image
keys. Keep the existing per-directory Dockerfile/build contexts for the six
frontends, seven backends and four .NET database APIs, with these additions:

- Build `mcp-server` from its own directory and `rag-server` from its own
  directory; these are not inferred frontend/backend/database contexts. RAG
  bakes Chroma's pinned default model cache, not indexes or generated keys.
  `node infra/scripts/measure-rag.mjs IMAGE` verifies offline cold start and
  retrieval at 0.5 CPU/1 GiB, without development volumes. The RAG CI gate runs it.
- Wrap each .NET database API image with
  `infra/images/database-api/Dockerfile`. Use repository-root build context and
  `APPLICATION_IMAGE` set to that API's just-built image (or immutable digest).
  Publish the wrapped result for `shared-db-api`, `mini-games-service-db`,
  `quizzes-courses-service-db` and `chat-discussion-service-db`. This adds the
  CA bundle while retaining the application's entrypoint, user and schema files.
  The intermediate image must be available to the selected builder.
- Build `quests-achievements-notifications-service-db` using repository-root
  context and `infra/images/postgrest/Dockerfile`, not the unmodified upstream
  image. This keeps PostgREST v12.2.3 and adds the same RDS bundle without relying
  on a shell or curl in the final image.
- Alternatively, the app owner may add the identical trusted bundle path to
  the four existing .NET Dockerfiles, removing the need for the wrapper phase.
  Do not omit certificate packaging while still setting VerifyFull connections.

## Initializer

The twenty-first image key is `database-initializer`. Version 2 release manifests
contain all twenty-one digests; legacy nineteen-image archives remain immutable
and are accepted for read/cleanup, never new Up or live migration.

- Build context: **repository root**.
- Dockerfile: `infra/images/database-initializer/Dockerfile`.
- Runtime: PostgreSQL 18.6 client, Python standard library and AWS CLI.
- The image copies the five owners' checked-in SQL only. It does not copy local
  environment files, signing keys or development database volumes.
- AWS's trusted global RDS bundle is downloaded over verified HTTPS at image
  build time. It is not downloaded or disabled at application startup.
- The ECS task definition injects the master secret. Never add a master secret,
  application password or OpenRouter key as a Docker build argument or layer.
- The initializer image is not a PostgreSQL server; its entrypoint is Python.

These Dockerfiles are packaging only. No AWS push, build or runtime operation
is performed by Terraform validation.
