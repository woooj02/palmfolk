$ErrorActionPreference = 'Stop'
Import-Module "$PSScriptRoot/ManifestChecks.psm1" -Force
$sourcePath = "$PSScriptRoot/../Assets/Plugins/Android/AndroidManifest.xml"
$source = Get-Content -LiteralPath $sourcePath -Raw
$sourceInternet = '  <uses-permission android:name="android.permission.INTERNET" tools:node="remove" />'
# Synthetic merged manifest: this is a validator fixture, not a built APK.
$merged = $source.Replace($sourceInternet, '').Replace('<manifest xmlns:', '<manifest package="com.keepsrunning.palmfolk" xmlns:')
$script:passed = 0
function Check-Accepted($name, $text, [switch]$SourceMode) {
    Assert-PalmfolkManifest -Text $text -Source:$SourceMode
    $script:passed++
    Write-Output "PASS: $name"
}
function Check-Rejected($name, $text, $expected) {
    $message = $null
    try { Assert-PalmfolkManifest -Text $text } catch { $message = $_.Exception.Message }
    if (-not $message -or $message -notlike "*$expected*") { throw "$name was not rejected for the expected reason: $message" }
    $script:passed++
    Write-Output "PASS: $name rejected"
}
Check-Accepted 'source manifest with Internet removal marker' $source -SourceMode
Check-Accepted 'synthetic merged manifest' $merged
Check-Accepted 'CRLF input' ($merged.Replace("`r`n", "`n").Replace("`n", "`r`n"))
Check-Accepted 'alternate XML namespace prefix' ($merged.Replace('xmlns:android=', 'xmlns:a=').Replace('android:', 'a:'))
Check-Rejected 'optional hands' ($merged.Replace('oculus.software.handtracking" android:required="true"', 'oculus.software.handtracking" android:required="false"')) 'must require hand tracking'
Check-Rejected 'missing hand permission' ($merged.Replace('com.oculus.permission.HAND_TRACKING', 'com.example.OTHER')) 'missing hand tracking permission'
Check-Rejected 'low frequency' ($merged.Replace('android:value="HIGH"', 'android:value="LOW"')) 'HIGH hand tracking frequency'
Check-Rejected 'wrong package' ($merged.Replace('com.keepsrunning.palmfolk', 'com.example.wrong')) 'package identifier'
Check-Rejected 'nonexported activity' ($merged.Replace('android:exported="true"', 'android:exported="false"')) 'exported Unity launch activity'
Check-Rejected 'missing launch action' ($merged.Replace('android.intent.action.MAIN', 'com.example.OTHER')) 'MAIN, LAUNCHER and VR'
Check-Rejected 'missing VR category' ($merged.Replace('com.oculus.intent.category.VR', 'com.example.OTHER')) 'MAIN, LAUNCHER and VR'
$misplacedVr = $merged.Replace('<category android:name="com.oculus.intent.category.VR" />', '').Replace('</application>', '<activity android:name="example.Other"><intent-filter><category android:name="com.oculus.intent.category.VR" /></intent-filter></activity></application>')
Check-Rejected 'VR category on another activity' $misplacedVr 'MAIN, LAUNCHER and VR'
Check-Rejected 'Quest 2 excluded' ($merged.Replace('quest2|quest3|quest3s', 'quest3|quest3s')) 'include Quest 2'
$internet = '<uses-permission android:name="android.permission.INTERNET" />'
Check-Rejected 'Internet permission' ($merged.Replace('</manifest>', $internet + '</manifest>')) 'INTERNET permission'
Check-Rejected 'versioned Internet permission' ($merged.Replace('</manifest>', $internet.Replace('uses-permission ', 'uses-permission-sdk-23 ') + '</manifest>')) 'INTERNET permission'
foreach ($permission in @('android.permission.CAMERA', 'android.permission.RECORD_AUDIO', 'android.permission.READ_EXTERNAL_STORAGE', 'com.oculus.permission.USE_SCENE', 'com.oculus.permission.USE_ANCHOR_API')) {
    Check-Rejected $permission ($merged.Replace('</manifest>', '<uses-permission android:name="' + $permission + '" /></manifest>')) 'allowlist'
}
$anchors = $merged.Replace('</manifest>', '<uses-permission android:name="com.oculus.permission.USE_ANCHOR_API" /></manifest>')
Assert-PalmfolkManifest -Text $anchors -TableMode
$script:passed++
$scene = $anchors.Replace('</manifest>', '<uses-permission android:name="com.oculus.permission.USE_SCENE" /></manifest>')
Assert-PalmfolkManifest -Text $scene -TableMode -SceneSnap
$script:passed++
Check-Rejected 'removal marker in merged manifest' ($merged.Replace('</manifest>', $sourceInternet + '</manifest>')) 'INTERNET permission'
$hand = '<uses-feature android:name="oculus.software.handtracking" android:required="true" />'
Check-Rejected 'duplicate hand declarations' ($merged.Replace('</manifest>', $hand + '</manifest>')) 'exactly once'
$frequency = '<meta-data android:name="com.oculus.handtracking.frequency" android:value="HIGH" />'
Check-Rejected 'duplicate frequency declarations' ($merged.Replace('</application>', $frequency + '</application>')) 'exactly once'
Check-Rejected 'malformed XML' '<manifest' 'unexpected'
Check-Rejected 'DTD input' ('<!DOCTYPE manifest [<!ENTITY value "test">]>' + $merged.Substring($merged.IndexOf('<manifest'))) 'DTD'

# Exercise the user-facing entry point and its file handling, including spaces.
$fixtureDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('palmfolk manifest ' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureDirectory | Out-Null
try {
    $fixturePath = Join-Path $fixtureDirectory 'merged manifest.xml'
    [System.IO.File]::WriteAllText($fixturePath, $merged)
    & "$PSScriptRoot/Test-Slice0Apk.ps1" -ManifestXml $fixturePath | Out-Null
    $script:passed++
    Write-Output 'PASS: manifest CLI with a spaced file path'
} finally {
    Remove-Item -LiteralPath $fixtureDirectory -Recurse -Force
}
Write-Output "$script:passed manifest checks passed. No Unity build or headset behavior was tested."
