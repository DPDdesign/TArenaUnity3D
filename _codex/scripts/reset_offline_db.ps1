$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$projectSettings = Join-Path $workspaceRoot "TArenaUnity3D\ProjectSettings\ProjectSettings.asset"

if (-not (Test-Path -LiteralPath $projectSettings)) {
    throw "ProjectSettings.asset not found at $projectSettings"
}

$companyName = "DefaultCompany"
$productName = "TArenaUnity3D"

foreach ($line in Get-Content -LiteralPath $projectSettings) {
    if ($line -match "^\s*companyName:\s*(.+)\s*$") {
        $companyName = $Matches[1].Trim()
    }
    elseif ($line -match "^\s*productName:\s*(.+)\s*$") {
        $productName = $Matches[1].Trim()
    }
}

$databaseDirectory = Join-Path $env:USERPROFILE ("AppData\LocalLow\{0}\{1}" -f $companyName, $productName)
$databasePath = Join-Path $databaseDirectory "TArenaOffline.db"

if (Test-Path -LiteralPath $databasePath) {
    Remove-Item -LiteralPath $databasePath -Force
    Write-Output "Deleted $databasePath"
}
else {
    Write-Output "Database not found: $databasePath"
}

if (-not (Test-Path -LiteralPath $databaseDirectory)) {
    New-Item -ItemType Directory -Path $databaseDirectory | Out-Null
    Write-Output "Created $databaseDirectory"
}

Write-Output "Unity OfflineDatabaseModule.OpenOrCreate will rebuild the DB without legacy route_* tables."
