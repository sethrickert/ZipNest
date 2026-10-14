[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$cache = Join-Path $repoRoot 'build\.cache'
$destination = Join-Path $repoRoot 'third_party\7zip'
$bootstrapPath = Join-Path $cache '7zr.exe'
$packagePath = Join-Path $cache '7z2604-x64.exe'
$expandedPath = Join-Path $cache '7zip-26.04'

$bootstrapUri = 'https://www.7-zip.org/a/7zr.exe'
$bootstrapSha256 = '256FECA8E274E5DA655E2A284FABAFD9F554365EB164862089DACD4E8276D282'
$packageUri = 'https://www.7-zip.org/a/7z2604-x64.exe'
$packageSha256 = 'D54BF805F9F3704D1E8DB2FA3498AE7EF2DF0312B40B558E7C71C734430A665D'

New-Item -ItemType Directory -Force -Path $cache, $destination, $expandedPath | Out-Null

function Get-VerifiedFile([string]$Uri, [string]$Path, [string]$ExpectedHash) {
    if (-not (Test-Path -LiteralPath $Path)) {
        Invoke-WebRequest -UseBasicParsing -Uri $Uri -OutFile $Path
    }
    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    if ($actual -ne $ExpectedHash) {
        throw "Hash verification failed for $Path. Expected $ExpectedHash; received $actual."
    }
}

Get-VerifiedFile $bootstrapUri $bootstrapPath $bootstrapSha256
Get-VerifiedFile $packageUri $packagePath $packageSha256

& $bootstrapPath x -y "-o$expandedPath" $packagePath | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not extract the verified 7-Zip package.' }

Copy-Item -LiteralPath (Join-Path $expandedPath '7z.exe') -Destination (Join-Path $destination '7z.exe') -Force
Copy-Item -LiteralPath (Join-Path $expandedPath '7z.dll') -Destination (Join-Path $destination '7z.dll') -Force
Copy-Item -LiteralPath (Join-Path $expandedPath 'License.txt') -Destination (Join-Path $destination 'License.txt') -Force

Write-Host 'Verified 7-Zip 26.04 x64 is ready.'
