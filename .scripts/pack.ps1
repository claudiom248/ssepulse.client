param(
    [string] $Configuration = "Debug",
    [string] $Projects = ""
)

$repoRoot = Split-Path -Path $PSScriptRoot -Parent
$srcPath = Join-Path $repoRoot "src"
$outputPath = Join-Path $repoRoot ".artifacts\nuget\$Configuration"

$gvJson = dotnet gitversion | ConvertFrom-Json

$version = $gvJson.FullSemVer
$preRelease = $gvJson.PreReleaseLabel

Write-Host "--- Package Preparation ---" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Detected version: $version"
if ($preRelease) {
    Write-Host "Pre-release: $preRelease" -ForegroundColor Yellow
}

$csprojFiles = Get-ChildItem -Path $srcPath -Recurse -Filter "*.csproj" |
        Where-Object { $_.Directory.Name -ne "obj" -and $_.Directory.Name -ne "bin" }

if ($Projects -ne "") {
    $projectList = $Projects.Split(",") | ForEach-Object { $_.Trim() }
    $csprojFiles = $csprojFiles | Where-Object {
        $projectList -contains $_.Directory.Name
    }
}

Write-Host "Found $($csprojFiles.Count) projects to pack" -ForegroundColor Cyan
Write-Host ""

foreach ($csproj in $csprojFiles) {
    $projectName = $csproj.Directory.Name
    Write-Host "[$projectName] Packaging..." -ForegroundColor Yellow

    dotnet pack $csproj.FullName `
        --configuration $Configuration `
        -p:IncludeSymbols=true `
        -p:SymbolPackageFormat=snupkg `
        -p:Version=$version `
        --output $outputPath

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[$projectName] Pack failed" -ForegroundColor Red
        exit 1
    }

    Write-Host "[$projectName] v$version" -ForegroundColor Green
}

Write-Host ""
Write-Host "--- Completed! ---" -ForegroundColor Green
Write-Host "Output: $outputPath" -ForegroundColor Gray