#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Applies the retention policy of the GitHub Packages feed.

.DESCRIPTION
    For every package built from src, keeps the newest KeepPreviews pre-release versions and the
    newest KeepStable non-pre-release versions (even when they are older than the previews) and
    deletes everything else. A version is a pre-release when its name contains a hyphen.

.PARAMETER Owner
    GitHub user that owns the packages. Defaults to the repository owner of the running workflow.

.PARAMETER KeepPreviews
    Number of newest pre-release versions to keep per package.

.PARAMETER KeepStable
    Number of newest stable versions to keep per package.

.PARAMETER DryRun
    Print what would be deleted without deleting anything.
#>
[CmdletBinding()]
param(
    [string] $Owner = $env:GITHUB_REPOSITORY_OWNER,
    [int] $KeepPreviews = 10,
    [int] $KeepStable = 1,
    [switch] $DryRun
)

Set-StrictMode -Version Latest

function Get-VersionsToDelete {
    param(
        [Parameter(Mandatory)] [object[]] $Versions,
        [int] $KeepPreviews,
        [int] $KeepStable
    )

    $newestFirst = @($Versions | Sort-Object { [datetime] $_.created_at } -Descending)
    $previews = @($newestFirst | Where-Object { $_.name.Contains('-') })
    $stable = @($newestFirst | Where-Object { -not $_.name.Contains('-') })

    $toDelete = @()
    $toDelete += @($previews | Select-Object -Skip $KeepPreviews)
    $toDelete += @($stable | Select-Object -Skip $KeepStable)
    return $toDelete
}

if ($MyInvocation.InvocationName -eq '.') {
    return
}

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Owner)) {
    throw 'Owner is required: pass -Owner or run inside a GitHub Actions workflow.'
}

$srcPath = Join-Path (Split-Path -Path $PSScriptRoot -Parent) 'src'
$packages = @(
    Get-ChildItem -Path $srcPath -Recurse -Filter '*.csproj' |
        Where-Object { $_.Directory.Name -notin 'obj', 'bin' } |
        ForEach-Object { $_.BaseName } |
        Sort-Object -Unique
)

$failed = $false
foreach ($package in $packages) {
    Write-Host ""
    Write-Host "--- $package ---"

    $json = gh api -H 'Accept: application/vnd.github+json' "/users/$Owner/packages/nuget/$package/versions" --paginate --slurp
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Could not list the versions of '$package'."
        $failed = $true
        continue
    }

    $versions = @($json | ConvertFrom-Json | ForEach-Object { $_ })
    if ($versions.Count -eq 0) {
        Write-Host '  No versions found.'
        continue
    }

    $toDelete = @(Get-VersionsToDelete -Versions $versions -KeepPreviews $KeepPreviews -KeepStable $KeepStable)
    Write-Host "  $($versions.Count) versions, $($toDelete.Count) to delete."

    foreach ($version in $toDelete) {
        if ($DryRun) {
            Write-Host "  [DRY RUN] would delete $($version.name) (id=$($version.id), created $($version.created_at))"
            continue
        }

        Write-Host "  Deleting $($version.name) (id=$($version.id))"
        gh api --method DELETE -H 'Accept: application/vnd.github+json' "/users/$Owner/packages/nuget/$package/versions/$($version.id)" | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not delete version $($version.name) of '$package'."
            $failed = $true
        }
    }
}

Write-Host ''
if ($failed) {
    Write-Error 'Some packages could not be listed or pruned.'
    exit 1
}

Write-Host $(if ($DryRun) { 'Dry run complete - nothing was deleted.' } else { 'Done.' })
