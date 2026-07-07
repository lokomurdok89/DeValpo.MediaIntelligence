param(
    [string]$Root = "."
)

$ErrorActionPreference = "Stop"

$trackedEnv = git -C $Root ls-files deploy/.env
if (-not [string]::IsNullOrWhiteSpace($trackedEnv)) {
    throw "deploy/.env must not be tracked."
}

$requiredFiles = @(
    "src/DeValpo.MediaIntelligence.Api/appsettings.Staging.json",
    "src/DeValpo.MediaIntelligence.Api/appsettings.Production.json",
    "deploy/.env.staging.example",
    "docs/runbooks/release-and-migrations.md"
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $Root $file
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required release file is missing: $file"
    }
}

$production = Get-Content -LiteralPath (Join-Path $Root "src/DeValpo.MediaIntelligence.Api/appsettings.Production.json") -Raw | ConvertFrom-Json
$staging = Get-Content -LiteralPath (Join-Path $Root "src/DeValpo.MediaIntelligence.Api/appsettings.Staging.json") -Raw | ConvertFrom-Json

foreach ($settings in @($production, $staging)) {
    if ($settings.Database.ApplyMigrationsOnStartup -ne $false) {
        throw "Release environments must not apply migrations automatically."
    }

    if ($settings.Swagger.Enabled -ne $false) {
        throw "Release environments must not expose Swagger publicly."
    }

    if ($settings.Jwt.RequireHttpsMetadata -ne $true) {
        throw "Release environments must require HTTPS metadata."
    }

    if ($settings.GoogleDocumentAi.AllowUserCredentials -ne $false) {
        throw "Release environments must not allow Google user credentials."
    }

    if ($settings.Skills.EnableMockSkills -ne $false) {
        throw "Release environments must not enable mock skills."
    }

    if ($settings.AnalysisProfiles.EnableMockProfiles -ne $false) {
        throw "Release environments must not enable mock profiles."
    }
}

Write-Host "Release configuration validation passed."
