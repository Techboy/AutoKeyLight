# Builds the three Windows packages:
#   CamOnKeyLight-win-x64.zip
#   AutoKeyLight-win-x64.zip          (accepted by the v0.6.0 updater)
#   CamOnKeyLight-Setup.exe
# Checksums are written beside the zips and the setup program.
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$RepoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $RepoRoot

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot "dist"
}

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)]
        [string]$File,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$Step
    )

    & $File @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

function Add-Checksum {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $name = Split-Path $Path -Leaf
    $hash = (Get-FileHash -Algorithm SHA256 $Path).Hash.ToLowerInvariant()
    $sum = "$Path.sha256"
    [System.IO.File]::WriteAllText($sum, "$hash  $name`n")
}

$work = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { Join-Path $RepoRoot "artifacts" }
$publish = Join-Path $work "camonkeylight-publish"
$legacy = Join-Path $work "camonkeylight-legacy"
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
if (Test-Path $legacy) { Remove-Item $legacy -Recurse -Force }

$publishArgs = @(
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:Version=$Version",
    "-p:DebugType=none",
    "-p:DebugSymbols=false"
)

Invoke-Native "dotnet" (@("publish", "CamOnKeyLight/CamOnKeyLight.csproj") + $publishArgs + @("-o", $publish)) "Publish CamOnKeyLight"
Invoke-Native "dotnet" (@("publish", "CamOnKeyLight/CamOnKeyLight.Legacy.csproj") + $publishArgs + @("-o", $legacy)) "Publish legacy AutoKeyLight"

Copy-Item (Join-Path $RepoRoot "LICENSE") (Join-Path $publish "LICENSE")
Copy-Item (Join-Path $RepoRoot "README.md") (Join-Path $publish "README.md")
Copy-Item (Join-Path $RepoRoot "LICENSE") (Join-Path $legacy "LICENSE")
Copy-Item (Join-Path $RepoRoot "README.md") (Join-Path $legacy "README.md")

# v0.6.0 rejects every program exe except AutoKeyLight.exe. The real
# CamOnKeyLight host is carried under a .dll name that those rules allow.
foreach ($name in @("CamOnKeyLight.dll", "CamOnKeyLight.deps.json", "CamOnKeyLight.runtimeconfig.json")) {
    Copy-Item (Join-Path $publish $name) (Join-Path $legacy $name)
}
Copy-Item (Join-Path $publish "CamOnKeyLight.exe") (Join-Path $legacy "CamOnKeyLight.apphost.dll")
$stray = Join-Path $legacy "CamOnKeyLight.exe"
if (Test-Path $stray) { Remove-Item $stray -Force }
if (Test-Path $stray) { throw "The legacy package still contains CamOnKeyLight.exe." }
foreach ($required in @("AutoKeyLight.exe", "CamOnKeyLight.dll", "CamOnKeyLight.deps.json", "CamOnKeyLight.runtimeconfig.json", "CamOnKeyLight.apphost.dll")) {
    if (!(Test-Path (Join-Path $legacy $required))) {
        throw "The legacy package is missing $required."
    }
}

$env:AUTOKEYLIGHT_PUBLISH_DIR = $publish
$env:AUTOKEYLIGHT_LEGACY_PUBLISH_DIR = $legacy
Invoke-Native "dotnet" @(
    "test", "CamOnKeyLight.Core.Tests/CamOnKeyLight.Core.Tests.csproj",
    "-c", "Release",
    "--filter", "PublishedOutputMatchesPackageRules|LegacyPublishedOutputMatchesPackageRules"
) "Package rule tests"

Add-Type -AssemblyName System.IO.Compression.FileSystem
$currentZip = Join-Path $OutputDir "CamOnKeyLight-win-x64.zip"
$legacyZip = Join-Path $OutputDir "AutoKeyLight-win-x64.zip"
if (Test-Path $currentZip) { Remove-Item $currentZip -Force }
if (Test-Path $legacyZip) { Remove-Item $legacyZip -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($publish, $currentZip)
[System.IO.Compression.ZipFile]::CreateFromDirectory($legacy, $legacyZip)
Add-Checksum $currentZip
Add-Checksum $legacyZip

Write-Host "LEGACY ZIP FILES"
$legacyArchive = [System.IO.Compression.ZipFile]::OpenRead($legacyZip)
try {
    foreach ($entry in ($legacyArchive.Entries | Sort-Object FullName)) {
        Write-Host ("{0}`t{1}" -f $entry.FullName, $entry.Length)
    }
}
finally {
    $legacyArchive.Dispose()
}

$innoSetup = Join-Path $work "innosetup-6.4.3.exe"
$innoDir = Join-Path $work "innosetup"
if (!(Test-Path (Join-Path $innoDir "ISCC.exe"))) {
    Invoke-WebRequest -Uri "https://github.com/jrsoftware/issrc/releases/download/is-6_4_3/innosetup-6.4.3.exe" -OutFile $innoSetup
    $installer = Start-Process -FilePath $innoSetup -ArgumentList "/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/DIR=$innoDir" -Wait -PassThru
    if ($installer.ExitCode -ne 0) {
        throw "The Inno Setup installer failed with exit code $($installer.ExitCode)."
    }
}

Invoke-Native (Join-Path $innoDir "ISCC.exe") @(
    "/DMyAppVersion=$Version",
    "/DPublishDir=$publish",
    "installer\CamOnKeyLight.iss"
) "ISCC"

$builtSetup = Join-Path $RepoRoot "installer\CamOnKeyLight-Setup.exe"
if (!(Test-Path $builtSetup)) { throw "The per-user installer was not created." }
$setup = Join-Path $OutputDir "CamOnKeyLight-Setup.exe"
Copy-Item $builtSetup $setup -Force
Add-Checksum $setup

Get-Item $currentZip, $legacyZip, $setup | ForEach-Object {
    Write-Host ("{0} {1} bytes" -f $_.Name, $_.Length)
}
