function Assert-PalmfolkManifest {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [switch]$Source
    )
    $settings = New-Object System.Xml.XmlReaderSettings
    $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $reader = [System.Xml.XmlReader]::Create((New-Object System.IO.StringReader($Text)), $settings)
    $manifest = New-Object System.Xml.XmlDocument
    $manifest.XmlResolver = $null
    try { $manifest.Load($reader) } finally { $reader.Dispose() }
    $android = 'http://schemas.android.com/apk/res/android'
    $tools = 'http://schemas.android.com/tools'
    function Find-Entry($path, $name) {
        @($manifest.SelectNodes($path) | Where-Object { $_.GetAttribute('name', $android) -eq $name })
    }
    if ($manifest.DocumentElement.Name -ne 'manifest') { throw 'Missing manifest root.' }
    if (-not $Source -and $manifest.DocumentElement.GetAttribute('package') -ne 'com.keepsrunning.palmfolk') {
        throw 'Unexpected APK package identifier.'
    }
    $hand = @(Find-Entry '/manifest/uses-feature' 'oculus.software.handtracking')
    if ($hand.Count -ne 1 -or $hand[0].GetAttribute('required', $android) -ne 'true') { throw 'Manifest must require hand tracking exactly once.' }
    $permissions = @($manifest.SelectNodes('/manifest/*[starts-with(local-name(), "uses-permission")]'))
    $handPermission = @($permissions | Where-Object { $_.GetAttribute('name', $android) -eq 'com.oculus.permission.HAND_TRACKING' })
    if ($handPermission.Count -ne 1 -or $handPermission[0].GetAttribute('node', $tools) -eq 'remove') { throw 'Manifest is missing hand tracking permission.' }
    $internet = @($permissions | Where-Object { $_.GetAttribute('name', $android) -eq 'android.permission.INTERNET' })
    foreach ($permission in $internet) {
        if (-not $Source -or $permission.GetAttribute('node', $tools) -ne 'remove') { throw 'Manifest grants INTERNET permission.' }
    }
    $frequency = @(Find-Entry '/manifest/application/meta-data' 'com.oculus.handtracking.frequency')
    if ($frequency.Count -ne 1 -or $frequency[0].GetAttribute('value', $android) -ne 'HIGH') { throw 'Manifest must request HIGH hand tracking frequency exactly once.' }
    $activity = @(Find-Entry '/manifest/application/activity' 'com.unity3d.player.UnityPlayerActivity')
    if ($activity.Count -ne 1 -or $activity[0].GetAttribute('exported', $android) -ne 'true') { throw 'Missing exported Unity launch activity.' }
    $validFilter = $false
    foreach ($filter in $activity[0].SelectNodes('intent-filter')) {
        $actions = @($filter.SelectNodes('action') | ForEach-Object { $_.GetAttribute('name', $android) })
        $categories = @($filter.SelectNodes('category') | ForEach-Object { $_.GetAttribute('name', $android) })
        if ($actions -contains 'android.intent.action.MAIN' -and
            $categories -contains 'android.intent.category.LAUNCHER' -and
            $categories -contains 'com.oculus.intent.category.VR') { $validFilter = $true }
    }
    if (-not $validFilter) { throw 'Unity launch activity is missing the MAIN, LAUNCHER and VR intent filter.' }
    $devices = @(Find-Entry '/manifest/application/meta-data' 'com.oculus.supportedDevices')
    if ($devices.Count -ne 1 -or ($devices[0].GetAttribute('value', $android) -split '\|') -notcontains 'quest2') { throw 'Manifest must include Quest 2 as a supported device.' }
}

Export-ModuleMember -Function Assert-PalmfolkManifest
