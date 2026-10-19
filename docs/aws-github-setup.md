# GitHub AWS Authentication Setup

This connects GitHub-hosted runners to the existing read-only AWS validation
role. It does not provision the application, grant deployment permissions, or
implement Demo Up/Down yet. No AWS key belongs on the homebox.

## Current State

- AWS account: `047754151573`; region: `ap-southeast-2`.
- Dedicated user: `arn:aws:iam::047754151573:user/languagewise/languagewise-runner`.
- Its only permission is to assume
  `arn:aws:iam::047754151573:role/languagewise/languagewise-runner-validation`.
- That role only describes the existing LanguageWise ECS cluster and ECR repository.
- No access key has been generated or installed in GitHub.
- The signed-in GitHub account, `Amber-Cahill`, has repository `WRITE`, not `ADMIN`.
- The `aws-demo` environment lookup returned 404; it is absent or not visible to
  this account. An administrator must confirm/configure it before key creation.
- [The validation workflow](../.github/workflows/aws-validation.yml) is prepared
  locally, not yet merged or run on GitHub.

## 1. GitHub Admin: Configure the Environment

In [repository environments](https://github.com/Yooshi124/LanguageWise/settings/environments):

1. Create or open an environment named exactly `aws-demo`.
2. Under deployment branches and tags, choose **Selected branches and tags**.
   Add a **Branch** rule for exactly `master`; do not allow wildcard branches or tags.
3. Leave required reviewers and wait timers disabled for this demo environment.
   Future deployment after merge must not wait for another manual approval.
4. Confirm protected `master` requires reviewed PRs and passing checks. Workflow
   and infrastructure changes must be reviewed; avoid routine direct pushes.
5. Under Actions settings, permit GitHub-hosted runners and the official
   `aws-actions/configure-aws-credentials` action. This workflow pins v6.3.0 to
   commit `e1253824e5c10ff9df46874f81ed3ec929e19cfd`.
6. Confirm who can add **environment secrets**. If Amber's existing access cannot
   do that, the admin must perform the secret-entry step; sharing a personal AWS
   login or granting broad AWS permissions is not necessary.

Do this before running the workflow. GitHub can automatically create a missing
environment referenced by a workflow, but that does not configure its protections.
Do not put the AWS key in repository-wide secrets as a workaround.

## 2. Team: Merge the Validation Workflow

Merge [aws-validation.yml](../.github/workflows/aws-validation.yml) through the
normal reviewed PR process into `master`. The assistant has not committed,
pushed, or merged these local changes.

The workflow is named **AWS Read-Only Validation**. It runs only through
`workflow_dispatch`, only in the expected repository on `master`, and only on a
GitHub-hosted Ubuntu runner. It checks missing secrets without printing their
values, assumes the exact validation role for 15 minutes, and checks identity
and the two permitted metadata reads. It does not check out repository code,
run PR code, request a GitHub OIDC token, or change AWS resources.

The credentials action skips session tags because the IAM user is intentionally
not allowed `sts:TagSession`. Account, region, and validation role are fixed in
this workflow, so no GitHub variables are needed for this first test.

## 3. AWS Operator: Create and Deliver One Key

Do not generate the key until the protected environment and a secure secret-entry
path are ready. Never send key values through chat, email, a workflow input, or
a command-line argument.

**Assisted handoff:** after step 1, report that `aws-demo` is ready. The assistant
can recheck access and attempt an approved AWS CLI-to-GitHub CLI transfer, keeping
key values in memory and sending them to `gh secret set` through standard input.
If secret writes are unavailable, do not leave a newly generated key active;
revoke it and use the manual operator/admin path below instead. An existing key
must not be overwritten or deleted without checking its owner/use.

**Manual operator/admin path:**

1. Sign in to AWS using the existing administrative identity, not the new user.
2. Open IAM > Users > `languagewise-runner` > Security credentials > Access keys
   > Create access key. Select the application-outside-AWS use case if offered.
   Create one key for this dedicated user, not a root/personal key.
3. Directly enter its two values as **environment secrets** in `aws-demo`:

   | Secret name | Value |
   | --- | --- |
   | `AWS_ACCESS_KEY_ID` | The dedicated user's access key ID. |
   | `AWS_SECRET_ACCESS_KEY` | The matching secret access key. |

   There is no `AWS_SESSION_TOKEN` for this long-lived source key. The workflow
   obtains a temporary session token itself when it assumes the role.
4. If using GitHub CLI, run these locally and enter each value at its prompt,
   directly in the terminal, not in chat or the command itself:

   ```powershell
   gh secret set AWS_ACCESS_KEY_ID --repo Yooshi124/LanguageWise --env aws-demo
   gh secret set AWS_SECRET_ACCESS_KEY --repo Yooshi124/LanguageWise --env aws-demo
   ```

   If permission is denied, have the admin enter the values directly. Any transfer
   between the AWS operator and admin must use an approved password manager or
   equivalent secure sharing, not this conversation.
5. Keep a record of the key owner and rotation deadline in your password manager.
   AWS shows the secret value only at creation. If the secret is lost or secure
   delivery fails, deactivate/delete the new key and create a replacement later;
   do not create repeated active keys or grant broader permissions to fix delivery.

## 4. Team: Run and Verify

In [repository Actions](https://github.com/Yooshi124/LanguageWise/actions), choose
**AWS Read-Only Validation** > **Run workflow** > branch `master` > **Run workflow**.

Expect the role identity to contain
`assumed-role/languagewise-runner-validation/`, followed by a success message for
the Sydney cluster and repository. Credentials must not appear in output. Share
the run URL or error text for troubleshooting, not secret values.

If secrets are missing, check their exact names and environment. If authentication
fails, check that both values belong to the same dedicated user's active key.
Do not widen IAM permissions to troubleshoot an expired/incorrect key. If IAM
reports a session-tagging error, verify the merged workflow still sets
`role-skip-session-tagging: true`.

A successful run proves GitHub-to-AWS authentication only. The migration plan's
budget, Terraform state, scoped deployment/control roles, application deployment,
Demo Up/Down, and independent AWS expiry cleanup remain subsequent work.

Rotate the source key at least every 90 days and at handover. On exposure, disable
it immediately, replace the GitHub secrets, and consider revoking issued role
sessions; disabling a source key does not invalidate existing STS sessions.
