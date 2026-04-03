param(
    [string] $Configuration = "Debug",
    [string] $Projects = "",
    [string] $ReportTypes = "Html;TextSummary"
)

$repoRoot = Split-Path -Path $PSScriptRoot -Parent
$testsPath = Join-Path $repoRoot "tests"
$artifactsPath = Join-Path $repoRoot ".artifacts\coverage"
$rawResultsPath = Join-Path $artifactsPath "raw"
$reportPath = Join-Path $artifactsPath "report"
$toolsPath = Join-Path $repoRoot ".artifacts\tools"
$reportGeneratorPath = Join-Path $toolsPath "reportgenerator.exe"

Write-Host "--- Coverage Generation ---" -ForegroundColor Cyan

$testProjects = Get-ChildItem -Path $testsPath -Recurse -Filter "*.csproj" |
    Where-Object {
        $_.FullName -notmatch "[\\/](bin|obj)[\\/]"
    }

if ($Projects -ne "") {
    $projectList = $Projects.Split(",") | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
    $testProjects = $testProjects | Where-Object {
        $projectList -contains $_.BaseName -or $projectList -contains $_.Directory.Name
    }
}

if (-not $testProjects -or $testProjects.Count -eq 0) {
    Write-Host "No test projects found" -ForegroundColor Red
    exit 1
}

if (Test-Path $artifactsPath) {
    Remove-Item -Path $artifactsPath -Recurse -Force
}

New-Item -Path $rawResultsPath -ItemType Directory -Force | Out-Null
New-Item -Path $reportPath -ItemType Directory -Force | Out-Null

Write-Host "Found $($testProjects.Count) test projects" -ForegroundColor Cyan
Write-Host ""

foreach ($testProject in $testProjects) {
    $projectName = $testProject.BaseName
    $projectResultsPath = Join-Path $rawResultsPath $projectName
    $projectCoveragePath = Join-Path $projectResultsPath "coverage"

    Write-Host "[$projectName] Running tests + coverage..." -ForegroundColor Yellow

    New-Item -Path $projectResultsPath -ItemType Directory -Force | Out-Null

    dotnet test $testProject.FullName `
        --configuration Release `
        /p:CollectCoverage=true `
        /p:Exclude="[*.Tests]*" `
        /p:CoverletOutputFormat="cobertura" `
        /p:CoverletOutput="$projectCoveragePath"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[$projectName] Tests failed" -ForegroundColor Red
        exit 1
    }

    Write-Host "[$projectName] Completed" -ForegroundColor Green
}

$coverageFiles = Get-ChildItem -Path $rawResultsPath -Recurse -Filter "*.cobertura.xml"

if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
    Write-Host "No coverage.cobertura.xml files found" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $reportGeneratorPath)) {
    Write-Host "Installing ReportGenerator..." -ForegroundColor Yellow
    New-Item -Path $toolsPath -ItemType Directory -Force | Out-Null

    dotnet tool install --tool-path $toolsPath dotnet-reportgenerator-globaltool

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ReportGenerator installation failed" -ForegroundColor Red
        exit 1
    }
}

$reports = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"

Write-Host ""
Write-Host "Generating report..." -ForegroundColor Cyan

& $reportGeneratorPath "-reports:$reports" "-targetdir:$reportPath" "-reporttypes:$ReportTypes"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Report generation failed" -ForegroundColor Red
    exit 1
}

$indexPath = Join-Path $reportPath "index.html"

Write-Host ""
Write-Host "--- Completed! ---" -ForegroundColor Green
Write-Host "Coverage files: $($coverageFiles.Count)" -ForegroundColor Gray
Write-Host "Report: $indexPath" -ForegroundColor Gray



