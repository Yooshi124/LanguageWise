[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$privateKeyPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../signing_private_key.pem'))
$publicKeyPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../signing_public_key.pem'))

if ($PSCmdlet.ShouldProcess((Split-Path $privateKeyPath), 'Generate RSA signing key pair')) {
    $rsa = [System.Security.Cryptography.RSA]::Create(2048)
    try {
        $encoding = [System.Text.UTF8Encoding]::new($false)
        [System.IO.File]::WriteAllText($privateKeyPath, $rsa.ExportRSAPrivateKeyPem(), $encoding)
        [System.IO.File]::WriteAllText($publicKeyPath, $rsa.ExportSubjectPublicKeyInfoPem(), $encoding)
    }
    finally {
        $rsa.Dispose()
    }
}