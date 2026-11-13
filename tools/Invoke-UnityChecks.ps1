[CmdletBinding()]
param(
    [ValidateSet('Compile', 'EditMode')][string]$Mode = 'Compile',
    [string]$Unity = 'E:/Unity/Editors/6000.0.66f2/Editor/Unity.exe',
    [int]$TimeoutSeconds = 1800,
    [ValidateRange(0, 128)][int]$JobWorkers = 0,
    [switch]$Development
)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not (Test-Path -LiteralPath $Unity)) { throw "Pinned Unity editor not found: $Unity. Installation is owner-only." }
if ($TimeoutSeconds -lt 1) { throw 'Timeout must be positive.' }
New-Item -ItemType Directory -Path "$project/Logs" -Force | Out-Null
$log = "$project/Logs/unity-$Mode.log"
$result = "$project/Logs/editmode-results.xml"
if ($Mode -eq 'EditMode' -and (Test-Path -LiteralPath $result)) { Remove-Item -LiteralPath $result -Force }
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'), '-logFile', ('"' + $log + '"'))
$workerArguments = @()
if ($JobWorkers -gt 0) { $workerArguments = @('-job-worker-count', [string]$JobWorkers) }
if ($Mode -eq 'Compile') { $arguments += @('-quit', '-executeMethod', 'Palmfolk.Editor.ValidationDefines.Verify') }
else { $arguments += @('-runTests', '-testPlatform', 'EditMode', '-testResults', ('"' + $result + '"')) }
function Run-Editor([string[]]$EditorArguments, [string]$EditorLog) {
    $process = Start-Process -FilePath $Unity -WindowStyle Hidden -PassThru -ArgumentList ($EditorArguments + $workerArguments)
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    # Only stop this child process, never an editor belonging to another worktree.
        Stop-Process -Id $process.Id -Force
        throw "Unity timed out. See $EditorLog"
    }
    if ($process.ExitCode -ne 0) { throw "Unity exited $($process.ExitCode). See $EditorLog" }
}
$settingsPath = "$project/ProjectSettings/ProjectSettings.asset"
$settingsExisted = Test-Path -LiteralPath $settingsPath
$previousSettings = if ($settingsExisted) { [IO.File]::ReadAllBytes($settingsPath) } else { $null }
$previousGate = $env:PALMFOLK_CHECK_DEVELOPMENT
try {
    $env:PALMFOLK_CHECK_DEVELOPMENT = if ($Development) { 'true' } else { 'false' }
    Run-Editor -EditorArguments @('-batchmode', '-quit', '-nographics', '-projectPath', ('"' + $project + '"'), '-logFile', ('"' + "$project/Logs/define-setup.log" + '"'), '-executeMethod', 'Palmfolk.Editor.ValidationDefines.Configure') -EditorLog "$project/Logs/define-setup.log"
    Run-Editor -EditorArguments $arguments -EditorLog $log
} finally {
    $env:PALMFOLK_CHECK_DEVELOPMENT = $previousGate
    if ($settingsExisted) { [IO.File]::WriteAllBytes($settingsPath, $previousSettings) }
    elseif (Test-Path -LiteralPath $settingsPath) { Remove-Item -LiteralPath $settingsPath -Force }
}
if (-not (Test-Path -LiteralPath $log)) { throw 'Unity did not produce its log.' }
if (Select-String -LiteralPath $log -Pattern 'error CS\d+|Scripts have compiler errors|Crash!!!' -Quiet) { throw "Unity compilation/import failed. See $log" }
if ($Mode -eq 'EditMode') {
    if (-not (Test-Path -LiteralPath $result)) { throw 'Unity produced no test result XML.' }
    [xml]$tests = Get-Content -LiteralPath $result -Raw
    $run = $tests.'test-run'
    if (-not $run -or [int]$run.total -le 0 -or $run.result -ne 'Passed' -or [int]$run.failed -ne 0) {
        throw "Unity tests did not all pass. See $result"
    }
    Write-Output "PASS: Unity editor Edit Mode, $($run.total) tests. Results: $result"
} else { Write-Output "PASS: Unity editor batch compile, exit 0, no error CS lines. Log: $log" }
Write-Output 'No simulator or headset behavior is verified by this check.'
