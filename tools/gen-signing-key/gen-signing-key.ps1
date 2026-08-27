$rsa = [System.Security.Cryptography.RSA]::Create(2048)
try {
    $pem = $rsa.ExportRSAPrivateKeyPem()
    [System.IO.File]::WriteAllText((Resolve-Path -Path "../../signing_key.pem" -ErrorAction SilentlyContinue ?? "../../signing_key.pem"), $pem)
}
finally {
    $rsa.Dispose()
}