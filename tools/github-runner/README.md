# Isolated Homebox Runner VM

This VM setup is historical. The selected deployment path uses GitHub-hosted
runners and a dedicated IAM user key in GitHub Secrets. Do not install that key
in the homebox or guest. The existing VM/runner has not been removed.

The setup script prepares an Ubuntu 24.04 VM on the Debian homebox. It does not register a
GitHub runner, authenticate to AWS, or create AWS resources. Application builds
and PR tests remain on GitHub-hosted runners.

## Prerequisites

The host must have QEMU/KVM, cloud-image-utils, curl, Python 3, nftables, and the
non-login `languagewise-runner` account in the `kvm` group. Its home directory is
`/var/lib/languagewise-runner`, owned by that account with mode `0700`.

The VM uses 2 vCPUs, 2 GiB RAM, and a sparse 32 GiB disk. No host filesystem or
Docker socket is mounted. Its SSH forwarding socket listens only on host
`127.0.0.1:2222`; do not forward it through the router.

## Install

Generate a dedicated Ed25519 operator key on your workstation using
`ssh-keygen -t ed25519`, choosing a new filename and entering its passphrase
directly in the terminal. Transfer only its `.pub` file and the setup script to
your ordinary homebox account using SCP. Never transfer the private key.

On the homebox, review the script and run:

```bash
sudo bash setup-homebox-vm.sh install operator.pub
sudo bash setup-homebox-vm.sh check
sudo tail -n 100 /var/lib/languagewise-runner/console.log
```

The script refuses to overwrite existing disks/services or use an occupied SSH
port. It downloads the official Ubuntu image and verifies the checksum fetched
over HTTPS from the same official origin; this is not separate signature
verification. First-boot package installation can take several minutes.

### Repair Initial Operator Access

The original seed omitted a primary group for `operator`. Ubuntu already has a
system group with that name, causing `useradd` to fail with exit code 9 and
leaving the guest without the operator account or authorized key. New installs
use the dedicated `languagewise-operators` group explicitly.

For an affected VM, transfer the updated script and run on the homebox:

```bash
sudo bash setup-homebox-vm.sh repair-access operator.pub
```

Use the original operator public key. The repair briefly stops only this VM,
backs up its current seed as `seed.before-access-repair-v3.img`, and starts it
with a new cloud-init instance ID so account creation runs again. It retains
the existing disk, networking configuration, firewall, and SSH host keys. It
does not download another OS image, enable passwords, or install a runner.
It refuses to overwrite an existing repair backup; inspect a failed repair
before retrying. This v3 repair leaves earlier repair backups intact and uses
a new instance ID so corrected YAML is not skipped after a failed v2 boot.
Allow cloud-init to finish, then retry the same operator SSH
command below with `-o IdentitiesOnly=yes`. Do not accept a changed host key.
Once connected, run `cloud-init status --long` and check the isolation gate.

Compare the guest SSH host fingerprint with the trusted host's console log
before accepting it. From your workstation, substitute your existing homebox
SSH target and dedicated private-key path:

```bash
ssh -J YOUR_HOMEBOX_SSH_TARGET -p 2222 -i YOUR_PRIVATE_KEY -o HostKeyAlias=languagewise-runner operator@127.0.0.1
```

## Isolation Gate

The firewall affects sockets owned by the dedicated VM account only. It never
flushes the host ruleset. It blocks new connections to local host addresses,
private/shared/link-local/reserved IPv4 ranges, all IPv6, and the current home
public IPv4. Public HTTP/HTTPS, public NTP, and DNS to Cloudflare resolvers are
allowed. VM SSH replies are allowed through established connections.

Run these inside the guest before adding credentials or a GitHub runner:

```bash
sudo cloud-init status --wait
curl --fail --max-time 20 https://github.com/robots.txt
curl --silent --show-error --max-time 20 --output /dev/null --write-out 'STS HTTP %{http_code}\n' https://sts.ap-southeast-2.amazonaws.com/
nc -z -w 3 10.0.2.2 2222
nc -z -w 3 192.168.0.1 80
nc -z -w 3 169.254.169.254 80
```

The STS request may return an HTTP error without authentication; a completed TLS
connection with an HTTP response is the connectivity check, not API authorization. All three `nc` checks
must fail. Also test your actual media-service addresses/ports, Docker network
addresses, and home public IPv4; inspect the host nftables reject counters to
distinguish a firewall rejection from an unavailable service. Test that IPv6
cannot reach host/LAN services. Do not register the runner until these pass.

The public-IPv4 exclusion is sampled when the firewall service starts. If your
home public IP changes, stop the VM and restart the firewall before restarting
the VM. This is not protection against arbitrary public reverse proxies or
tunnels to your home services; do not expose those to the guest. Do not grant
the guest VPN access. Host firewall replacement/flushing invalidates isolation;
stop the VM before changing firewall management and recheck it afterwards.

Repository workflow restrictions, contributor approval, branch/workflow review,
temporary AWS login renewal, and independently authenticated expiry cleanup are
separate gates. A label alone does not restrict a runner to a single workflow.

## GitHub Smoke Test

After the repository safeguards and runner service are configured, merge the
manual-only `Runner Smoke Test` workflow into protected `master`. In GitHub
Actions, select that workflow and run it against `master`. It targets
`[self-hosted, linux, x64, languagewise-deploy]`, requires no token permissions,
does not check out code, and does not use AWS credentials. It checks Linux/x64,
non-root execution, and GitHub HTTPS access. Confirm the run succeeds and the
runner returns to Idle before configuring deployment authentication. Local
YAML/shell validation does not replace this GitHub-dispatched test.

## AWS Validation Role

The separately approved, manual IAM bootstrap created
`arn:aws:iam::047754151573:role/languagewise/languagewise-runner-validation`.
[iam/validation-trust.json](iam/validation-trust.json) trusts exactly the existing
workstation role, including its `/managed/` path, and dedicated IAM user
`arn:aws:iam::047754151573:user/languagewise/languagewise-runner`.
[iam/runner-user-permissions.json](iam/runner-user-permissions.json) records the
user's sole inline policy, permitting only assumption of this validation role.
[iam/validation-permissions.json](iam/validation-permissions.json) permits only
describing the existing LanguageWise ECS cluster and ECR repository in Sydney.
The maximum session duration is one hour. This is not a deployment role and
does not permit pulling images, reading secrets, passing roles, or changing AWS
resources. It is not yet managed by Terraform.

The user was created without a console password or access key. Policy simulation
allows assumption of the validation role, denies assumption of the broad
workstation role, and denies direct ECS/ECR reads. Secure key creation, GitHub
Secrets configuration, and a GitHub-hosted validation run are still pending.

Role assumption and both metadata reads passed from Windows and inside the VM.
The VM check received only this role's temporary credentials through trusted
SSH standard input and held them in process memory; no credentials were printed
or saved. No persistent AWS credentials were installed in the guest. Future
checks must likewise transfer only scoped temporary sessions, never the
workstation's administrative credentials or login cache. Enter SSH passwords
directly in the terminal, not chat. This one-off VM check does not configure
AWS access or session renewal for GitHub Actions.

## Stop and Remove

```bash
sudo systemctl stop languagewise-runner-vm.service
sudo bash setup-homebox-vm.sh remove
```

Removal disables the two services and removes only their unit files, helper,
and dedicated nftables table. It retains disks, credentials, and the account.
After reviewing what will be erased, `sudo userdel --remove languagewise-runner`
permanently deletes that account and its VM disks. Unregister any GitHub runner
and revoke its AWS sessions before removal. Shared QEMU packages are retained.
