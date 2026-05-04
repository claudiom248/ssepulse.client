#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deletes GitHub Packages versions for all SsePulse.Client NuGet packages.

.DESCRIPTION
    By default removes every pre-release version (versions whose name contains a hyphen,
    e.g. 1.1.0-alpha.3). When -Version is supplied the exact version is deleted regardless
    of whether it is a pre-release or a stable release.

.PARAMETER Owner
    GitHub organisation or user name that owns the packages. Defaults to 'claudiom248'.

.PARAMETER Version
    Exact version string to delete (e.g. '1.1.1' or '1.2.0-rc.1').
    When omitted, all pre-release versions are deleted.

.PARAMETER DryRun
    Print what would be deleted without actually deleting anything.

.EXAMPLE
    # Remove all pre-release packages
    ./.scripts/remove-packages.ps1

.EXAMPLE
    # Remove the accidental stable release v1.1.1
    ./.scripts/remove-packages.ps1 -Version 1.1.1

.EXAMPLE
    # Preview what would be deleted without touching anything
    ./.scripts/remove-packages.ps1 -DryRun
#>
[CmdletBinding()]
param(
    [string] $Owner   = 'claudiom248',
    [string] $Version = '',
    [switch] $DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$packages = @(
    'SsePulse.Client'
    'SsePulse.Client.Authentication'
    'SsePulse.Client.Authentication.DependencyInjection'
    'SsePulse.Client.DependencyInjection'
    'SsePulse.Client.Hosting'
)

function Get-PackageVersions([string] $PackageName) {
    $endpoint = "/users/$Owner/packages/nuget/$PackageName/versions"
    $json = gh api -H "Accept: application/vnd.github+json" $endpoint --paginate 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Could not list versions for '$PackageName' - skipping."
        return @()
    }
    return $json | ConvertFrom-Json
}

function Remove-PackageVersion([string] $PackageName, [int] $VersionId, [string] $VersionName) {
    if ($DryRun) {
        Write-Host "[DRY RUN] Would delete  $PackageName  $VersionName  (id=$VersionId)"
        return
    }

    Write-Host "Deleting  $PackageName  $VersionName  (id=$VersionId) ..."
    $endpoint = "/users/$Owner/packages/nuget/$PackageName/versions/$VersionId"
    gh api --method DELETE -H "Accept: application/vnd.github+json" $endpoint | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Failed to delete version id=$VersionId of '$PackageName'."
    }
}

foreach ($package in $packages) {
    Write-Host ""
    Write-Host "--- $package ---"

    $versions = Get-PackageVersions -PackageName $package
    if ($versions.Count -eq 0) {
        Write-Host "  No versions found."
        continue
    }

    foreach ($v in $versions) {
        $name = $v.name

        $shouldDelete = if ($Version) {
            $name -eq $Version
        } else {
            $name -match '-'
        }

        if ($shouldDelete) {
            Remove-PackageVersion -PackageName $package -VersionId $v.id -VersionName $name
        } else {
            Write-Verbose "  Keeping $name"
        }
    }
}

Write-Host ""
if ($DryRun) {
    Write-Host "Dry run complete - nothing was deleted."
} else {
    Write-Host "Done."
}
