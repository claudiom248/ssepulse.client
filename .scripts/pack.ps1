param(
    [string] $Configuration = "Release",
    [string] $Projects = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Path $PSScriptRoot -Parent
$srcPath = Join-Path $repoRoot "src"
$outputPath = Join-Path $repoRoot ".artifacts\nuget\$Configuration"

Write-Host "--- Package Preparation ---" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray

dotnet restore (Join-Path $repoRoot "SsePulse.Client.slnx") --locked-mode
if ($LASTEXITCODE -ne 0) { exit 1 }

$csprojFiles = Get-ChildItem -Path $srcPath -Recurse -Filter "*.csproj" |
        Where-Object { $_.Directory.Name -ne "obj" -and $_.Directory.Name -ne "bin" }

if ($Projects -ne "") {
    $projectList = $Projects.Split(",") | ForEach-Object { $_.Trim() }
    $csprojFiles = $csprojFiles | Where-Object {
        $projectList -contains $_.Directory.Name
    }
}

if (Test-Path $outputPath) {
    Remove-Item -Path $outputPath -Recurse -Force
}

Write-Host "Found $($csprojFiles.Count) projects to pack" -ForegroundColor Cyan
Write-Host ""

foreach ($csproj in $csprojFiles) {
    $projectName = $csproj.Directory.Name
    Write-Host "[$projectName] Packaging..." -ForegroundColor Yellow

    dotnet pack $csproj.FullName --configuration $Configuration --no-restore --output $outputPath

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[$projectName] Pack failed" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "--- Completed! ---" -ForegroundColor Green
Get-ChildItem -Path $outputPath -Filter "*.nupkg" | ForEach-Object { Write-Host $_.Name -ForegroundColor Gray }
Write-Host "Output: $outputPath" -ForegroundColor Gray
