param(
    [int] $Port = 9056,
    [switch] $BuildOnly
)

$repoRoot = Split-Path -Path $PSScriptRoot -Parent
$docfxJson = Join-Path $repoRoot "docs\docfx.json"
$siteDir = Join-Path $repoRoot "..\.artifacts\docfx\_site"

Write-Host "--- Docs: Build & Serve ---" -ForegroundColor Cyan
Write-Host "Output: $siteDir" -ForegroundColor Gray

if ($BuildOnly) {
    Write-Host "Mode: build only" -ForegroundColor Gray
    docfx $docfxJson

    if ($LASTEXITCODE -ne 0) {
        Write-Host "docfx build failed" -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "--- Completed! ---" -ForegroundColor Green
    Write-Host "Site: $siteDir" -ForegroundColor Gray
}
else {
    Write-Host "Port: $Port" -ForegroundColor Gray
    Write-Host "Mode: build + serve (Ctrl+C to stop)" -ForegroundColor Gray
    Write-Host ""
    docfx $docfxJson --serve --port $Port

    if ($LASTEXITCODE -ne 0) {
        Write-Host "docfx failed" -ForegroundColor Red
        exit 1
    }
}

