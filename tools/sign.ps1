param(
  [Parameter(Mandatory=$true)][string]$File,
  [string]$Subject = 'CN=ManyCopy Dev (Self-Signed)',
  [string]$TimeStampServer = 'http://timestamp.digicert.com',
  [switch]$RecreateCert
)

function Get-OrCreate-CodeSignCert {
  param([string]$Subject,[switch]$Recreate)
  if (-not $Recreate) {
    $existing = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
      Where-Object { $_.Subject -eq $Subject } |
      Sort-Object NotAfter -Descending | Select-Object -First 1
    if ($existing) { return $existing }
  }
  New-SelfSignedCertificate -Type CodeSigning -Subject $Subject -CertStoreLocation Cert:\CurrentUser\My -KeyExportPolicy Exportable
}

if (-not (Test-Path -LiteralPath $File)) { throw "File not found: $File" }
$cert = Get-OrCreate-CodeSignCert -Subject $Subject -Recreate:$RecreateCert
Write-Host "Using certificate: $($cert.Subject)  Thumbprint=$($cert.Thumbprint)  Expires=$($cert.NotAfter)"

try {
  $sig = Set-AuthenticodeSignature -FilePath $File -Certificate $cert -TimestampServer $TimeStampServer -ErrorAction Stop
} catch {
  Write-Warning "Signing failed without timestamp server. Retrying without timestamp... ($_ )"
  $sig = Set-AuthenticodeSignature -FilePath $File -Certificate $cert
}

Write-Host "Signature Status: $($sig.Status) | $($sig.StatusMessage)"
if ($sig.SignerCertificate) {
  Write-Host "Signer: $($sig.SignerCertificate.Subject)"
}