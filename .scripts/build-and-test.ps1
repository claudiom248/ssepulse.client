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

Write-Host "--- Build ---" -ForegroundColor Cyan

# Resolve project: accept either a direct path or a bare project-folder name.
$resolvedProject = ""
if ($Project) {
    if (Test-Path $Project) {
        $resolvedProject = $Project
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
        Write-Host "Resolved project: $resolvedProject" -ForegroundColor Gray
    }
}

$buildArgs = @("build", "-c", $Configuration, "--maxcpucount", "--binaryLogger")
if ($resolvedProject) {
    $buildArgs += $resolvedProject
}
if ($Framework) {
    $buildArgs += "--framework"
    $buildArgs += $Framework
}

dotnet @buildArgs

if ($LASTEXITCODE -ne 0) { exit 1 }

if (-not $RunTests) {
    Write-Host "--- Tests skipped ---" -ForegroundColor Yellow
    exit 0
}

New-Item -ItemType Directory -Path $testResultsPath -Force | Out-Null

$testArgs = @(
    "test",
    "-c", $Configuration,
    "--no-build",
    "--results-directory", $testResultsPath,
    "--logger", "trx",
    "--consoleLoggerParameters:Summary;Verbosity=Minimal"
)

if ($Framework) {
    $testArgs += "--framework"
    $testArgs += $Framework
}

if ($resolvedProject) {
    $testArgs += $resolvedProject
}

Write-Host "--- Test ---" -ForegroundColor Cyan
dotnet @testArgs
