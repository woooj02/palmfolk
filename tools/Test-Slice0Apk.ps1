[CmdletBinding(DefaultParameterSetName = 'Apk')]
param(
    [Parameter(ParameterSetName = 'Apk')]
    [string]$Apk = "$PSScriptRoot/../Builds/Palmfolk-slice0.apk",
    [Parameter(ParameterSetName = 'Apk')]
    [string]$Sdk = 'C:/Program Files/Unity/Hub/Editor/6000.0.66f2/Editor/Data/PlaybackEngines/AndroidPlayer/SDK',
    [Parameter(Mandatory = $true, ParameterSetName = 'Manifest')]
    [string]$ManifestXml,
    [Parameter(ParameterSetName = 'Manifest')]
    [switch]$Source,
    [switch]$TableMode,
    [switch]$SceneSnap
)
$ErrorActionPreference = 'Stop'
Import-Module "$PSScriptRoot/ManifestChecks.psm1" -Force
if ($PSCmdlet.ParameterSetName -eq 'Manifest') {
    $text = Get-Content -LiteralPath $ManifestXml -Raw
    Assert-PalmfolkManifest -Text $text -Source:$Source -TableMode:$TableMode -SceneSnap:$SceneSnap
    Write-Output 'PASS: manifest XML checks. This does not verify an APK or headset behavior.'
    return
}
if (-not (Test-Path -LiteralPath $Apk)) { throw "APK not found: $Apk. Build slice 0 in Unity first." }
$analyzer = Get-ChildItem "$Sdk/cmdline-tools" -Filter apkanalyzer.bat -Recurse |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $analyzer) { throw 'apkanalyzer.bat not found. Supply -Sdk with the Android SDK path shown in Unity Preferences > External Tools.' }

# apkanalyzer needs the JDK bundled alongside Unity's SDK.
$previousJava = $env:JAVA_HOME
try {
    $jdk = Join-Path (Split-Path $Sdk -Parent) 'OpenJDK'
    if (Test-Path $jdk) { $env:JAVA_HOME = $jdk }
    $manifestText = & $analyzer manifest print (Resolve-Path -LiteralPath $Apk).Path
    if ($LASTEXITCODE -ne 0) { throw 'apkanalyzer could not read the built APK.' }
    Assert-PalmfolkManifest -Text ($manifestText -join "`n") -TableMode:$TableMode -SceneSnap:$SceneSnap
} finally {
    $env:JAVA_HOME = $previousJava
}
Write-Output 'PASS: built APK declares hands-only tracking, HIGH frequency, VR launch, expected package, and no INTERNET permission.'
Write-Output 'Quest 2 hands visibility still requires the headset test in docs/slice-0-quest2.md.'
