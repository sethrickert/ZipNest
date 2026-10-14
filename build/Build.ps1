[CmdletBinding()]
param(
    [string]$Version = '0.1.0',
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $repoRoot 'build\.dotnet'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'
$artifacts = Join-Path $repoRoot 'artifacts'
$portableRoot = Join-Path $artifacts 'ZipNest-portable'
$project = Join-Path $repoRoot 'src\ZipNest\ZipNest.csproj'
$tests = Join-Path $repoRoot 'tests\ZipNest.SmokeTests\ZipNest.SmokeTests.csproj'

& (Join-Path $PSScriptRoot 'Generate-Icons.ps1')
& (Join-Path $PSScriptRoot 'Get-SevenZip.ps1')

dotnet restore (Join-Path $repoRoot 'ZipNest.sln') --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
dotnet build $project -c Release --no-restore -p:Version=$Version -p:FileVersion="$Version.0" -p:AssemblyVersion="$Version.0"
if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
dotnet build $tests -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Smoke-test build failed.' }

$sevenZip = Join-Path $repoRoot 'third_party\7zip\7z.exe'
$testExecutable = Join-Path $repoRoot 'tests\ZipNest.SmokeTests\bin\Release\net48\ZipNest.SmokeTests.exe'
& $testExecutable $sevenZip
if ($LASTEXITCODE -ne 0) { throw 'Smoke tests failed.' }

New-Item -ItemType Directory -Force -Path $portableRoot, (Join-Path $portableRoot '7zip') | Out-Null
$appOutput = Join-Path $repoRoot 'src\ZipNest\bin\Release\net48'
Copy-Item -LiteralPath (Join-Path $appOutput 'ZipNest.exe') -Destination $portableRoot -Force
if (Test-Path -LiteralPath (Join-Path $appOutput 'ZipNest.exe.config')) {
    Copy-Item -LiteralPath (Join-Path $appOutput 'ZipNest.exe.config') -Destination $portableRoot -Force
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'third_party\7zip\7z.exe') -Destination (Join-Path $portableRoot '7zip') -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'third_party\7zip\7z.dll') -Destination (Join-Path $portableRoot '7zip') -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'third_party\7zip\License.txt') -Destination (Join-Path $portableRoot '7zip') -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $portableRoot -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'PORTABLE-README.txt') -Destination $portableRoot -Force
New-Item -ItemType File -Force -Path (Join-Path $portableRoot 'ZipNest.portable') | Out-Null

$portableZip = Join-Path $artifacts "ZipNest-$Version-portable-x64.zip"
Compress-Archive -Path (Join-Path $portableRoot '*') -DestinationPath $portableZip -CompressionLevel Optimal -Force

if (-not $SkipInstaller) {
    $iscc = @(
        $env:ISCC_PATH,
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 7\ISCC.exe'
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
    if (-not $iscc) {
        $iscc = & (Join-Path $PSScriptRoot 'Get-InnoSetup.ps1')
    }
    & $iscc "/DAppVersion=$Version" (Join-Path $repoRoot 'installer\ZipNest.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
}

Get-ChildItem -LiteralPath $artifacts -File | Where-Object { $_.Extension -ne '.sha256' } | ForEach-Object {
    $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($_.Name)" | Set-Content -LiteralPath ($_.FullName + '.sha256') -Encoding ascii
}

Write-Host "Build complete: $artifacts"
