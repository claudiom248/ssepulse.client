#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Moves the entries of every PublicAPI.Unshipped.txt into PublicAPI.Shipped.txt.

.DESCRIPTION
    Run it in a pull request after a stable release. Entries prefixed with *REMOVED* delete the
    matching entry of the shipped file instead of being added to it.
#>
$ErrorActionPreference = 'Stop'

$srcPath = Join-Path (Split-Path -Path $PSScriptRoot -Parent) 'src'
$header = '#nullable enable'
$utf8 = New-Object System.Text.UTF8Encoding($false)

foreach ($project in Get-ChildItem -Path $srcPath -Directory) {
    $shippedPath = Join-Path $project.FullName 'PublicAPI.Shipped.txt'
    $unshippedPath = Join-Path $project.FullName 'PublicAPI.Unshipped.txt'
    if (-not (Test-Path $shippedPath) -or -not (Test-Path $unshippedPath)) {
        continue
    }

    $shipped = [System.Collections.Generic.HashSet[string]]::new([string[]] @(
            Get-Content $shippedPath | Where-Object { $_ -and $_ -ne $header }), [System.StringComparer]::Ordinal)
    $unshipped = @(Get-Content $unshippedPath | Where-Object { $_ -and $_ -ne $header })

    foreach ($entry in $unshipped) {
        if ($entry.StartsWith('*REMOVED*')) {
            [void] $shipped.Remove($entry.Substring('*REMOVED*'.Length))
        }
        else {
            [void] $shipped.Add($entry)
        }
    }

    $sorted = @($shipped | Sort-Object -CaseSensitive)
    $lines = @($header) + $sorted
    [IO.File]::WriteAllText($shippedPath, ($lines -join "`n") + "`n", $utf8)
    [IO.File]::WriteAllText($unshippedPath, "$header`n", $utf8)
    Write-Host "$($project.Name): $($unshipped.Count) entries shipped"
}
