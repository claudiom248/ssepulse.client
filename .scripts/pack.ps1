param(
    [string] $Configuration = "Release",
    [string] $Projects = "",
    [switch] $Push,
    [string] $GithubToken = $env:GITHUB_TOKEN
)

# ---------------------------------------------------------------------------
# Validate push prerequisites
# ---------------------------------------------------------------------------
if ($Push) {
    if (-not $GithubToken) {
        Write-Error "A GitHub token must be supplied via '-GithubToken' or the GITHUB_TOKEN environment variable."
        exit 1
    }
}

$repoRoot = Split-Path -Path $PSScriptRoot -Parent
$srcPath = Join-Path $repoRoot "src"

$repositoryUrl = "https://github.com/claudiom248/SsePulse.Client"

$gvJson = dotnet gitversion | ConvertFrom-Json

$version = $gvJson.FullSemVer
$preRelease = $gvJson.PreReleaseLabel

# Output goes into a version-specific subfolder so every pack run is isolated.
# This makes it trivial to push exactly what was just built and easy to locate
# a specific version locally for debugging (e.g. .artifacts\nuget\Release\1.2.3\).
$outputPath = Join-Path $repoRoot ".artifacts\nuget\$Configuration\$version"

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

    $packArgs = @(
        "pack", $csproj.FullName,
        "--configuration", $Configuration,
        "-p:IncludeSymbols=true",
        "-p:SymbolPackageFormat=snupkg",
        "-p:Version=$version",
        "--output", $outputPath
    )
    if ($repositoryUrl) {
        $packArgs += "-p:RepositoryUrl=$repositoryUrl"
        $packArgs += "-p:RepositoryType=git"
    }

    dotnet @packArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[$projectName] Pack failed" -ForegroundColor Red
        exit 1
    }

    Write-Host "[$projectName] v$version" -ForegroundColor Green
}

Write-Host ""
Write-Host "--- Completed! ---" -ForegroundColor Green
Write-Host "Output: $outputPath" -ForegroundColor Gray

# ---------------------------------------------------------------------------
# Optional push to GitHub Packages
# ---------------------------------------------------------------------------
if (-not $Push) {
    exit 0
}

$githubFeedUrl = "https://nuget.pkg.github.com/claudiom248/index.json"
$githubSourceName = "github"

Write-Host ""
Write-Host "--- Pushing to GitHub Packages ---" -ForegroundColor Cyan
Write-Host "Feed : $githubFeedUrl" -ForegroundColor Gray

# Register the source if it is not already present
$existingSource = dotnet nuget list source | Select-String -SimpleMatch $githubFeedUrl
if (-not $existingSource) {
    Write-Host "Registering NuGet source '$githubSourceName'..." -ForegroundColor Gray
    dotnet nuget add source $githubFeedUrl `
        --name $githubSourceName `
        --username claudiom248 `
        --password $GithubToken `
        --store-password-in-clear-text

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to register NuGet source."
        exit 1
    }
}

$nupkgFiles = Get-ChildItem -Path $outputPath -Filter "*.nupkg" |
    Where-Object { $_.Name -notlike "*.symbols.nupkg" }

Write-Host "Pushing $($nupkgFiles.Count) package(s)..." -ForegroundColor Cyan

foreach ($pkg in $nupkgFiles) {
    Write-Host "  Pushing $($pkg.Name)..." -ForegroundColor Yellow

    dotnet nuget push $pkg.FullName `
        --source $githubFeedUrl `
        --api-key $GithubToken `
        --skip-duplicate

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Push failed for $($pkg.Name)" -ForegroundColor Red
        exit 1
    }

    Write-Host "  $($pkg.Name) pushed" -ForegroundColor Green
}

Write-Host ""
Write-Host "--- Push completed! ---" -ForegroundColor Green

