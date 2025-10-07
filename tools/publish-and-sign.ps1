param(
  [string]$Configuration = 'Release',
  [string]$Runtime = 'win-x64',
  [switch]$CleanFirst = $true,
  [string]$Subject = 'CN=ManyCopy Dev (Self-Signed)'
)

$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
Set-Location -LiteralPath $root

if ($CleanFirst) {
  Remove-Item -Recurse -Force -ErrorAction SilentlyContinue .\bin,.\obj,.\ManyCopy.Core\bin,.\ManyCopy.Core\obj
}

$pubArgs = @('publish','ManyCopy.csproj','-c',$Configuration,'-r',$Runtime,'-p:SelfContained=true','-p:PublishSingleFile=true','-p:PublishTrimmed=false','-v','minimal')
& dotnet @pubArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }

$pubDir = Join-Path $root "bin/$Configuration/net8.0-windows/$Runtime/publish"
$exe = Join-Path $pubDir 'ManyCopy.exe'
if (-not (Test-Path $exe)) { throw "Publish output not found: $exe" }

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'tools/sign.ps1') -File $exe -Subject $Subject

Get-Item $exe | Select-Object FullName,Length,LastWriteTime