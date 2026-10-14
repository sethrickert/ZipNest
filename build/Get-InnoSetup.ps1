[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$cache = Join-Path $repoRoot 'build\.cache'
$installer = Join-Path $cache 'innosetup-6.7.3.exe'
$destination = Join-Path $cache 'InnoSetup-6.7.3-portable'
$iscc = Join-Path $destination 'ISCC.exe'
$uri = 'https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe'
$expectedSha256 = '9C73C3BAE7ED48D44112A0F48E66742C00090BDB5BEF71D9D3C056C66E97B732'

New-Item -ItemType Directory -Force -Path $cache | Out-Null
if (-not (Test-Path -LiteralPath $installer)) {
    Invoke-WebRequest -UseBasicParsing -Uri $uri -OutFile $installer
}
$actual = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash
if ($actual -ne $expectedSha256) {
    throw "Hash verification failed for Inno Setup. Expected $expectedSha256; received $actual."
}

if (-not (Test-Path -LiteralPath $iscc)) {
    $arguments = '/PORTABLE=1 /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CURRENTUSER /DIR="' + $destination + '"'
    $process = Start-Process -FilePath $installer -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $iscc)) {
        throw "The verified Inno Setup package could not prepare its portable compiler (exit code $($process.ExitCode))."
    }
}

Write-Output $iscc
