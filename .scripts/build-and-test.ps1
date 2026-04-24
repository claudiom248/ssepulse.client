param(
    [string] $Configuration = "Release",
    [switch] $RunTests,
    [string] $Framework = "",
    [string] $Project = "",
    [string] $TestResultsDirectory = ".artifacts/test-results"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Path $PSScriptRoot -Parent
$testResultsPath = Join-Path $repoRoot $TestResultsDirectory

Write-Host "--- Restore tools ---" -ForegroundColor Cyan
dotnet tool restore

Write-Host "--- Restore packages ---" -ForegroundColor Cyan
dotnet restore --locked-mode

$allSourceProjects = Get-ChildItem -Path (Join-Path $repoRoot "src") -Recurse -Filter "*.csproj" |
    Select-Object -ExpandProperty FullName |
    Sort-Object

$allTestProjects = Get-ChildItem -Path (Join-Path $repoRoot "tests") -Recurse -Filter "*.csproj" |
    Select-Object -ExpandProperty FullName |
    Sort-Object

$commonTestProject = $allTestProjects | Where-Object { [IO.Path]::GetFileName($_) -eq "SsePulse.Client.Tests.Common.csproj" } | Select-Object -First 1
$allRunnableTestProjects = $allTestProjects | Where-Object { $_ -ne $commonTestProject }

# Resolve project: accept either a direct path or a bare project-folder name.
$resolvedProject = ""
if ($Project) {
    if (Test-Path $Project) {
        $resolvedProject = (Resolve-Path $Project).Path
    }
    else {
        $match = Get-ChildItem -Path $repoRoot -Recurse -Filter "$Project.csproj" |
            Where-Object { $_.Directory.Name -eq $Project } |
            Select-Object -First 1
        if (-not $match) {
            Write-Error "Cannot resolve project '$Project' - no matching .csproj found."
            exit 1
        }

        $resolvedProject = $match.FullName
    }

    Write-Host "Resolved project: $resolvedProject" -ForegroundColor Gray
}

$sourceProjectsToBuild = @()
$testProjectsToRun = @()

if ($resolvedProject) {
    if ($resolvedProject -like (Join-Path $repoRoot "src\*") ) {
        $sourceProjectsToBuild = @($resolvedProject)
    }
    elseif ($resolvedProject -eq $commonTestProject) {
        Write-Host "Project '$resolvedProject' is a shared test utility project; skipping direct test execution." -ForegroundColor Yellow
    }
    else {
        $testProjectsToRun = @($resolvedProject)
    }
}
else {
    $sourceProjectsToBuild = $allSourceProjects
    $testProjectsToRun = $allRunnableTestProjects
}

if ($sourceProjectsToBuild.Count -gt 0) {
    Write-Host "--- Build ---" -ForegroundColor Cyan

    $buildFramework = $Framework
    if ($Framework -eq "net462") {
        $buildFramework = "netstandard2.0"
    }

    foreach ($sourceProject in $sourceProjectsToBuild) {
        $buildArgs = @("build", $sourceProject, "-c", $Configuration, "--no-restore", "--maxcpucount", "--binaryLogger")
        if ($buildFramework) {
            $buildArgs += "--framework"
            $buildArgs += $buildFramework
        }

        dotnet @buildArgs

        if ($LASTEXITCODE -ne 0) { exit 1 }
    }
}
else {
    Write-Host "--- Build skipped (no source projects selected) ---" -ForegroundColor Yellow
}

if (-not $RunTests) {
    Write-Host "--- Tests skipped ---" -ForegroundColor Yellow
    exit 0
}

if ($testProjectsToRun.Count -eq 0) {
    Write-Host "--- Test skipped (no runnable test projects selected) ---" -ForegroundColor Yellow
    exit 0
}

New-Item -ItemType Directory -Path $testResultsPath -Force | Out-Null

Write-Host "--- Test ---" -ForegroundColor Cyan

$frameworkLabel = if ($Framework) { $Framework } else { "all" }

foreach ($testProject in $testProjectsToRun) {
    $testProjectName = [IO.Path]::GetFileNameWithoutExtension($testProject)
    $logFileName = "$testProjectName-$frameworkLabel.trx"

    $testArgs = @(
        "test",
        $testProject,
        "-c", $Configuration,
        "--no-restore",
        "--results-directory", $testResultsPath,
        "--logger", "trx;LogFileName=$logFileName",
        "--consoleLoggerParameters:Summary;Verbosity=Minimal"
    )

    if ($Framework) {
        $testArgs += "--framework"
        $testArgs += $Framework
    }

    dotnet @testArgs

    if ($LASTEXITCODE -ne 0) { exit 1 }
}
