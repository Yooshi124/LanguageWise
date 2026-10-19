# Retained Storage Bootstrap

This is an administrative, local-state root, never part of Demo Down. Terraform is
`~> 1.16.0`, AWS provider `~> 6.0`; the generated lockfile pins the provider build.
The provider permits only account `047754151573` in Sydney (`ap-southeast-2`).
No AWS deployment was performed during implementation. Credential-free checks:

```powershell
terraform -chdir=infra/bootstrap init -backend=false -input=false
terraform -chdir=infra/bootstrap fmt -check
terraform -chdir=infra/bootstrap validate
```

An authorized operator must review this root and provision both buckets before
foundation setup. The state bucket's default name is
`languagewise-047754151573-ap-southeast-2-tfstate`; change
`state_bucket_name` before initial provisioning if that globally unique name is
unavailable. The artifact bucket has the fixed name
`languagewise-047754151573-ap-southeast-2-artifacts`, also consumed by foundation.
Outputs are `state_bucket_name` and `artifact_bucket_name`. This standalone root
has no AWS data sources and needs no foundation outputs or cleanup source inputs.
Keep the local bootstrap state and backups in protected operator
storage, never in source control or release artifacts. Do not discard that state.

Both buckets are versioned, AES256-encrypted, public-blocked and TLS-only, with
`force_destroy=false` and Terraform `prevent_destroy`. Both abort incomplete
multipart uploads after one day. Noncurrent state versions expire after 30 days;
current state is not expired. Keep separately protected recovery copies as needed,
and review total retained state/source/artifact storage against the migration
plan's **5 GB-month S3 allowance**. Age-based retention is not a byte quota.
Artifact versions are deliberately never auto-expired: cleanup must retain its
exact pinned source VersionId even after a newer version is uploaded. Review
unreferenced artifacts manually without deleting any active cleanup source or
current/previous compatible release manifest.
`prevent_destroy` is a Terraform guard, not an account-wide deletion deny or S3
Object Lock. Do not let routine roles administer either bucket.

After provisioning, an operator must verify artifact versioning, encryption,
public-access blocking, TLS-only policy and lifecycle settings. Publish the
reviewed cleanup ZIP to that bucket and record the actual VersionId before
completing foundation setup. Foundation reads the artifact bucket as a data source;
it does not own or duplicate this bucket or its protection resources. For the
intended first deployment neither bucket exists yet, so no bucket import or
targeted foundation apply is required. Stop for a reviewed adoption/state-migration
procedure if a bucket already exists or is owned by another state.

The foundation root uses a separate remote S3 backend. Pass the state bucket name
at initialization, never a credential:

```powershell
terraform -chdir=infra/foundation init -backend-config="bucket=languagewise-047754151573-ap-southeast-2-tfstate"
```

Foundation uses `foundation/terraform.tfstate` and native `use_lockfile=true`.
If an operator previously provisioned foundation with local state, use a reviewed
`init -migrate-state` operation with that original state present, not a new empty
state. Bootstrap can remain local permanently. If explicitly migrating bootstrap,
use an administrative backend with `bootstrap/terraform.tfstate` and native locking;
do not migrate it to the demo key or discard the pre-migration backup.
