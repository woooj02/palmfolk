param(
    [string]$Apk = "$PSScriptRoot/../Builds/Palmfolk-slice0.apk",
    [string]$Sdk = 'C:/Program Files/Unity/Hub/Editor/6000.0.66f2/Editor/Data/PlaybackEngines/AndroidPlayer/SDK'
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Apk)) { throw "APK not found: $Apk. Build slice 0 in Unity first." }
$analyzer = Get-ChildItem "$Sdk/cmdline-tools" -Filter apkanalyzer.bat -Recurse |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $analyzer) { throw 'apkanalyzer.bat not found. Supply -Sdk with the Android SDK path shown in Unity Preferences > External Tools.' }

# apkanalyzer needs the JDK bundled alongside Unity's SDK.
$previousJava = $env:JAVA_HOME
try {
    $jdk = Join-Path (Split-Path $Sdk -Parent) 'OpenJDK'
    if (Test-Path $jdk) { $env:JAVA_HOME = $jdk }
    $manifestText = & $analyzer manifest print (Resolve-Path $Apk).Path
    if ($LASTEXITCODE -ne 0) { throw 'apkanalyzer could not read the built APK.' }
    [xml]$manifest = $manifestText -join "`n"
} finally {
    $env:JAVA_HOME = $previousJava
}
$android = 'http://schemas.android.com/apk/res/android'
function Find-Entry($path, $name) {
    @($manifest.SelectNodes($path) | Where-Object { $_.GetAttribute('name', $android) -eq $name })
}
$hand = @(Find-Entry '/manifest/uses-feature' 'oculus.software.handtracking')
if ($hand.Count -ne 1 -or $hand[0].GetAttribute('required', $android) -ne 'true') { throw 'APK does not require hand tracking.' }
if (@(Find-Entry '/manifest/uses-permission' 'com.oculus.permission.HAND_TRACKING').Count -ne 1) { throw 'APK is missing hand tracking permission.' }
$frequency = @(Find-Entry '/manifest/application/meta-data' 'com.oculus.handtracking.frequency')
if ($frequency.Count -ne 1 -or $frequency[0].GetAttribute('value', $android) -ne 'HIGH') { throw 'APK does not request HIGH hand tracking frequency.' }
if (@(Find-Entry '/manifest/uses-permission' 'android.permission.INTERNET').Count -ne 0) { throw 'APK grants INTERNET permission.' }
if ($manifest.manifest.package -ne 'com.keepsrunning.palmfolk') { throw 'Unexpected APK package identifier.' }
$vr = @(Find-Entry '/manifest/application/activity/intent-filter/category' 'com.oculus.intent.category.VR')
if ($vr.Count -lt 1) { throw 'APK is missing the VR launch category.' }
Write-Output 'PASS: built APK declares hands-only tracking, HIGH frequency, VR launch, expected package, and no INTERNET permission.'
Write-Output 'Quest 2 hands visibility still requires the headset test in docs/slice-0-quest2.md.'
