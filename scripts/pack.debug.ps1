$gvJson = dotnet gitversion | ConvertFrom-Json

# Estraiamo le variabili che ci servono
$version = $gvJson.FullSemVer        # Es: 1.2.0-beta.1
$nugetVersion = $gvJson.NuGetVersion # Es: 1.2.0-beta0001

Write-Host "--- Preparazione Package ---" -ForegroundColor Cyan
Write-Host "Versione rilevata: $version"

dotnet pack ../src/SsePulse/SsePulse.csproj `
    --configuration Debug `
    -p:IncludeSymbols=true `
    -p:SymbolPackageFormat=snupkg   `
    -p:Version=$version `
    --output ../artifacts

Write-Host "--- Completato! Il file si trova in ./artifacts ---" -ForegroundColor Green