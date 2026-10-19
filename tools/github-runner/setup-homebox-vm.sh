#!/usr/bin/env bash
set -euo pipefail

vm_user=languagewise-runner
vm_dir=/var/lib/languagewise-runner
vm_unit=languagewise-runner-vm.service
firewall_unit=languagewise-runner-firewall.service
firewall_helper=/usr/local/libexec/languagewise-runner-firewall
image_url=https://cloud-images.ubuntu.com/noble/current

if [[ ${EUID} -ne 0 ]]; then
    printf 'Run this script as root on the homebox.\n' >&2
    exit 1
fi

case ${1:-} in
    check)
        systemctl is-active "$firewall_unit" "$vm_unit"
        /usr/sbin/nft list table inet languagewise_runner
        if ! (exec 3<>/dev/tcp/127.0.0.1/2222) 2>/dev/null; then
            printf 'The loopback-only VM SSH forwarding port is not listening.\n' >&2
            exit 1
        fi
        if runuser -u "$vm_user" -- bash -c 'exec 3<>/dev/tcp/127.0.0.1/2222' 2>/dev/null; then
            printf 'FAIL: the VM account can initiate host-loopback connections.\n' >&2
            exit 1
        fi
        runuser -u "$vm_user" -- curl --noproxy '*' --fail --silent --show-error \
            --connect-timeout 5 --max-time 20 https://1.1.1.1/cdn-cgi/trace >/dev/null
        printf 'Host-side checks passed. Guest isolation tests are still required.\n'
        exit 0
        ;;
        repair-access)
                public_key=${2:?Supply the original operator SSH public-key file}
                ssh-keygen -lf "$public_key"
                operator_key=$(awk 'NR == 1 && $1 == "ssh-ed25519" && NF >= 2 { print $1 " " $2 }' "$public_key")
                test -n "$operator_key" || { printf 'An Ed25519 public key is required.\n' >&2; exit 1; }
                test -f "$vm_dir/disk.qcow2"
                test -f "$vm_dir/seed.img"
                test ! -e "$vm_dir/seed.before-access-repair-v3.img"
                systemctl is-active --quiet "$firewall_unit"
                /usr/sbin/nft list table inet languagewise_runner >/dev/null
                umask 077
                work_dir=$(mktemp -d)
                trap 'rm -rf "$work_dir"' EXIT
                isoinfo -i "$vm_dir/seed.img" -J -x /network-config >"$work_dir/network-config"
                test -s "$work_dir/network-config"
                cat >"$work_dir/user-data" <<EOF
#cloud-config
hostname: languagewise-runner
disable_root: true
ssh_pwauth: false
ssh_deletekeys: false
groups: [languagewise-operators]
users: [{name: operator, primary_group: languagewise-operators, shell: /bin/bash, groups: [sudo], sudo: 'ALL=(ALL) NOPASSWD:ALL', lock_passwd: true, ssh_authorized_keys: ['$operator_key']}]
EOF
                cat >"$work_dir/meta-data" <<'EOF'
instance-id: languagewise-runner-access-v3
local-hostname: languagewise-runner
EOF
                cloud-localds --network-config="$work_dir/network-config" "$work_dir/seed.img" "$work_dir/user-data" "$work_dir/meta-data"
                systemctl stop "$vm_unit"
                trap 'systemctl start "$vm_unit"; rm -rf "$work_dir"' EXIT
                cp -p "$vm_dir/seed.img" "$vm_dir/seed.before-access-repair-v3.img"
                install -o "$vm_user" -g "$vm_user" -m 0600 "$work_dir/seed.img" "$vm_dir/seed.img"
                systemctl start "$vm_unit"
                trap 'rm -rf "$work_dir"' EXIT
                printf 'Access repair boot started. The existing disk, host keys, and firewall are retained.\n'
                printf 'Retry operator SSH after cloud-init finishes; do not accept a changed host key.\n'
                exit 0
                ;;
    remove)
        systemctl disable --now "$vm_unit" "$firewall_unit"
        rm -f "/etc/systemd/system/$vm_unit" "/etc/systemd/system/$firewall_unit" "$firewall_helper"
        systemctl daemon-reload
        printf 'VM and its firewall removed. Disks and account retained at %s.\n' "$vm_dir"
        printf 'To erase the VM and credentials permanently, review and run: userdel --remove %s\n' "$vm_user"
        exit 0
        ;;
    install)
        ;;
    *)
        printf 'Usage: %s install OPERATOR_PUBLIC_KEY | repair-access OPERATOR_PUBLIC_KEY | check | remove\n' "$0" >&2
        exit 1
        ;;
esac

public_key=${2:?Supply an SSH public-key file, never a private key}
for tool in qemu-system-x86_64 qemu-img cloud-localds curl sha256sum ssh-keygen runuser python3; do
    command -v "$tool" >/dev/null
done
test -x /usr/sbin/nft
test -c /dev/kvm
test -d "$vm_dir"
test "$(stat -c %U "$vm_dir")" = "$vm_user"
test "$(stat -c %a "$vm_dir")" = 700
test "$(getent passwd "$vm_user" | cut -d: -f7)" = /usr/sbin/nologin
runuser -u "$vm_user" -- test -r /dev/kvm
runuser -u "$vm_user" -- test -w /dev/kvm
ssh-keygen -lf "$public_key"
operator_key=$(awk 'NR == 1 && $1 == "ssh-ed25519" && NF >= 2 { print $1 " " $2 }' "$public_key")
test -n "$operator_key" || { printf 'An Ed25519 public key is required.\n' >&2; exit 1; }
for existing in "$vm_dir/disk.qcow2" "/etc/systemd/system/$vm_unit" "/etc/systemd/system/$firewall_unit" "$firewall_helper"; do
    if [[ -e $existing ]]; then
        printf 'Refusing to overwrite existing installation: %s\n' "$existing" >&2
        exit 1
    fi
done
if /usr/sbin/nft list table inet languagewise_runner >/dev/null 2>&1; then
    printf 'The languagewise_runner firewall table already exists; inspect it first.\n' >&2
    exit 1
fi
if (exec 3<>/dev/tcp/127.0.0.1/2222) 2>/dev/null; then
    printf 'Port 2222 is already occupied; no existing service will be replaced.\n' >&2
    exit 1
fi

umask 077
work_dir=$(mktemp -d)
trap 'rm -rf "$work_dir"' EXIT
curl --fail --location --proto '=https' --tlsv1.2 --retry 3 \
    "$image_url/SHA256SUMS" -o "$work_dir/SHA256SUMS"
curl --fail --location --proto '=https' --tlsv1.2 --retry 3 \
    "$image_url/noble-server-cloudimg-amd64.img" -o "$work_dir/noble-server-cloudimg-amd64.img"
(
    cd "$work_dir"
    grep -E '^[0-9a-f]{64} [ *]noble-server-cloudimg-amd64\.img$' SHA256SUMS >image.sha256
    sha256sum --check image.sha256
)

cat >"$work_dir/user-data" <<EOF
#cloud-config
hostname: languagewise-runner
manage_etc_hosts: true
disable_root: true
ssh_pwauth: false
groups: [languagewise-operators]
users: [{name: operator, primary_group: languagewise-operators, shell: /bin/bash, groups: [sudo], sudo: 'ALL=(ALL) NOPASSWD:ALL', lock_passwd: true, ssh_authorized_keys: ['$operator_key']}]
package_update: true
packages:
  - ca-certificates
  - curl
  - git
  - jq
  - openssh-server
  - unzip
  - netcat-openbsd
write_files:
  - path: /etc/ssh/sshd_config.d/00-languagewise.conf
    permissions: '0644'
    content: |
      PasswordAuthentication no
      KbdInteractiveAuthentication no
      PermitRootLogin no
runcmd:
  - [systemctl, restart, ssh]
final_message: LanguageWise guest initialization completed after \$UPTIME seconds
EOF
cat >"$work_dir/meta-data" <<'EOF'
instance-id: languagewise-runner-v1
local-hostname: languagewise-runner
EOF
cat >"$work_dir/network-config" <<'EOF'
version: 2
ethernets:
  runner:
    match:
      macaddress: '52:54:00:4c:57:01'
    dhcp4: true
    dhcp6: false
    accept-ra: false
    dhcp4-overrides:
      use-dns: false
    nameservers:
      addresses: [1.1.1.1, 1.0.0.1]
EOF
qemu-img convert -f qcow2 -O qcow2 "$work_dir/noble-server-cloudimg-amd64.img" "$vm_dir/disk.qcow2"
qemu-img resize "$vm_dir/disk.qcow2" 32G
cloud-localds --network-config="$work_dir/network-config" "$vm_dir/seed.img" "$work_dir/user-data" "$work_dir/meta-data"
chown "$vm_user:$vm_user" "$vm_dir/disk.qcow2" "$vm_dir/seed.img"
chmod 0600 "$vm_dir/disk.qcow2" "$vm_dir/seed.img"

install -d -m 0755 /usr/local/libexec
cat >"$firewall_helper" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
vm_uid=$(id -u languagewise-runner)
wan_ip=$(curl --noproxy '*' --fail --silent --show-error --connect-timeout 10 --max-time 30 https://checkip.amazonaws.com)
python3 -c 'import ipaddress, sys; address = ipaddress.IPv4Address(sys.argv[1]); sys.exit(not address.is_global)' "$wan_ip"
rules=$(mktemp)
trap 'rm -f "$rules"' EXIT
cat >"$rules" <<RULES
table inet languagewise_runner {
    chain output {
        type filter hook output priority -10; policy accept;
        meta skuid $vm_uid jump vm_egress
    }
    chain vm_egress {
        ct state established,related counter accept
        fib daddr type local counter reject
        meta nfproto ipv6 counter reject
        ip daddr { 0.0.0.0/8, 10.0.0.0/8, 100.64.0.0/10, 127.0.0.0/8, 169.254.0.0/16, 172.16.0.0/12, 192.0.0.0/24, 192.0.2.0/24, 192.168.0.0/16, 198.18.0.0/15, 198.51.100.0/24, 203.0.113.0/24, 224.0.0.0/4, 240.0.0.0/4, $wan_ip } counter reject
        ip daddr { 1.1.1.1, 1.0.0.1 } udp dport 53 counter accept
        ip daddr { 1.1.1.1, 1.0.0.1 } tcp dport 53 counter accept
        tcp dport { 80, 443 } counter accept
        udp dport 123 counter accept
        counter reject
    }
}
RULES
/usr/sbin/nft --check --file "$rules"
/usr/sbin/nft --file "$rules"
printf 'VM egress firewall installed; current home public IPv4 is blocked.\n'
EOF
chmod 0755 "$firewall_helper"

cat >"/etc/systemd/system/$firewall_unit" <<EOF
[Unit]
Description=LanguageWise VM account egress isolation
Wants=network-online.target
After=network-online.target
Before=$vm_unit

[Service]
Type=oneshot
RemainAfterExit=yes
ExecStart=$firewall_helper
ExecStop=/usr/sbin/nft delete table inet languagewise_runner

[Install]
WantedBy=multi-user.target
EOF
cat >"/etc/systemd/system/$vm_unit" <<EOF
[Unit]
Description=Isolated LanguageWise deployment VM
BindsTo=$firewall_unit
After=$firewall_unit

[Service]
User=$vm_user
Group=$vm_user
SupplementaryGroups=kvm
ExecStart=/usr/bin/qemu-system-x86_64 -name languagewise-runner -machine q35,accel=kvm -cpu host -smp 2 -m 2048 -nodefaults -display none -monitor none -serial file:$vm_dir/console.log -drive file=$vm_dir/disk.qcow2,if=virtio,format=qcow2 -drive file=$vm_dir/seed.img,media=cdrom,readonly=on -netdev user,id=net0,ipv6=off,hostfwd=tcp:127.0.0.1:2222-:22 -device virtio-net-pci,netdev=net0,mac=52:54:00:4c:57:01 -sandbox on,obsolete=deny,elevateprivileges=deny,spawn=deny,resourcecontrol=deny
Restart=on-failure
RestartSec=10
TimeoutStopSec=90
KillSignal=SIGTERM
CPUQuota=200%
MemoryMax=2560M
MemorySwapMax=512M
UMask=0077
NoNewPrivileges=yes
CapabilityBoundingSet=
ProtectSystem=strict
ProtectHome=yes
ReadWritePaths=$vm_dir
PrivateTmp=yes
DevicePolicy=closed
DeviceAllow=/dev/kvm rw
ProtectKernelTunables=yes
ProtectKernelModules=yes
ProtectKernelLogs=yes
ProtectControlGroups=yes
RestrictSUIDSGID=yes
RestrictAddressFamilies=AF_UNIX AF_INET AF_NETLINK
LockPersonality=yes
RestrictRealtime=yes

[Install]
WantedBy=multi-user.target
EOF

systemd-analyze verify "/etc/systemd/system/$firewall_unit" "/etc/systemd/system/$vm_unit"
systemctl daemon-reload
systemctl enable --now "$firewall_unit"
systemctl enable --now "$vm_unit"
printf 'VM started. Cloud-init package installation may take several minutes.\n'
printf 'Inspect initialization and SSH host fingerprints: sudo tail -n 100 %s/console.log\n' "$vm_dir"
printf 'Run this script with check, then perform guest isolation tests before registering a runner.\n'
