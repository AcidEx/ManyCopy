param(
  [Parameter(Mandatory=$true)][string]$PublishDirectory,
  [Parameter(Mandatory=$true)][string]$OutputDirectory,
  [Parameter(Mandatory=$true)][string]$Version,
  [switch]$Unsigned
)

$ErrorActionPreference = 'Stop'

$executable = Join-Path $PublishDirectory 'ManyCopy.exe'
if (-not (Test-Path -LiteralPath $executable)) {
  throw "Published executable not found: $executable"
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$unsignedSuffix = if ($Unsigned) { '-UNSIGNED' } else { '' }
$packageName = "ManyCopy-$Version-win-x64$unsignedSuffix.zip"
$packagePath = Join-Path $OutputDirectory $packageName
$checksumPath = Join-Path $PublishDirectory 'ManyCopy.exe.sha256'
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $executable).Hash.ToLowerInvariant()

Set-Content -LiteralPath $checksumPath -Value "$hash *ManyCopy.exe" -Encoding ascii
Compress-Archive -LiteralPath $executable, $checksumPath -DestinationPath $packagePath -Force

[pscustomobject]@{
  Name = $packageName
  Path = $packagePath
  ExecutableSha256 = $hash
  Signed = -not $Unsigned
}
