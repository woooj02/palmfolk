[CmdletBinding()]
param(
    [ValidateSet('Release', 'Developer')][string]$Configuration = 'Release',
    [string]$Unity = 'E:/Unity/Editors/6000.0.66f2/Editor/Unity.exe',
    [string]$Sdk = 'E:/Unity/Editors/6000.0.66f2/Editor/Data/PlaybackEngines/AndroidPlayer/SDK',
    [switch]$TableMode,
    [switch]$SceneSnap
)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $project
$environmentNames = @('PALMFOLK_SOURCE_REVISION', 'PALMFOLK_BUILD_CONFIGURATION', 'PALMFOLK_TABLE_MODE', 'PALMFOLK_SCENE_SNAP')
$previous = @{}
foreach ($name in $environmentNames) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    if ($SceneSnap -and -not $TableMode) { throw 'Scene snap requires Table mode.' }
    if (-not (Test-Path -LiteralPath $Unity)) { throw 'Pinned Unity editor is not installed. Ask the owner.' }
    git diff --quiet HEAD --
    if ($LASTEXITCODE -ne 0) { throw 'Commit or restore tracked changes before stamping a build revision.' }
    $untracked = @(git ls-files --others --exclude-standard -- Assets Packages ProjectSettings)
    if ($LASTEXITCODE -ne 0 -or $untracked.Count -gt 0) { throw 'Commit the reviewed project setup or remove untracked project assets before stamping a build. Do not package unstamped source/defaults.' }
    $revision = git rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or $revision -notmatch '^[0-9a-f]{40}$') { throw 'Cannot read source revision.' }
    $env:PALMFOLK_SOURCE_REVISION = $revision
    $env:PALMFOLK_BUILD_CONFIGURATION = $Configuration
    $env:PALMFOLK_TABLE_MODE = [string][bool]$TableMode
    $env:PALMFOLK_SCENE_SNAP = [string][bool]$SceneSnap
    New-Item -ItemType Directory -Path Logs, Builds -Force | Out-Null
    $log = "$project/Logs/build-$Configuration.log"
    $process = Start-Process $Unity -WindowStyle Hidden -Wait -PassThru -ArgumentList @(
        '-batchmode', '-quit', '-nographics', '-buildTarget', 'Android', '-projectPath', ('"' + $project + '"'),
        '-logFile', ('"' + $log + '"'), '-executeMethod', 'Palmfolk.Editor.OwnerBuild.Build')
    if ($process.ExitCode -ne 0 -or (Select-String -LiteralPath $log -Pattern 'error CS\d+|Crash!!!' -Quiet)) {
        throw "Unity build failed. See $log"
    }
    $apk = "$project/Builds/Palmfolk-$Configuration.apk"
    & "$PSScriptRoot/Test-Slice0Apk.ps1" -Apk $apk -Sdk $Sdk -TableMode:$TableMode -SceneSnap:$SceneSnap
    $report = "$project/Builds/Palmfolk-$Configuration.json"
    $evidence = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if ($evidence.revision -ne $revision -or $evidence.configuration -ne $Configuration) { throw 'Build report does not match requested source/configuration.' }
    $evidence | Add-Member -NotePropertyName apkSha256 -NotePropertyValue (Get-FileHash -LiteralPath $apk -Algorithm SHA256).Hash
    $evidence | Add-Member -NotePropertyName verification -NotePropertyValue 'Unity editor build and APK manifest only; headset unverified'
    $evidence | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $report -Encoding UTF8
    Write-Output "Build: $apk; evidence: $report. No signing-in, installation, upload or headset action performed."
} finally {
    foreach ($name in $environmentNames) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
    Pop-Location
}
