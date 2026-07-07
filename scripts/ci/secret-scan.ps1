param(
    [string]$Root = "."
)

$ErrorActionPreference = "Stop"

$patterns = @(
    @{ Name = "private-key-block"; Pattern = "-----BEGIN (RSA |EC |OPENSSH |)?PRIVATE KEY-----" },
    @{ Name = "json-private-key"; Pattern = '"private_key"\s*:\s*"[^"]+' },
    @{ Name = "json-client-secret"; Pattern = '"client_secret"\s*:\s*"[^"]{8,}' },
    @{ Name = "json-refresh-token"; Pattern = '"refresh_token"\s*:\s*"[^"]+' },
    @{ Name = "json-access-token"; Pattern = '"access_token"\s*:\s*"[^"]+' },
    @{ Name = "openai-api-key"; Pattern = "sk-(proj-)?[A-Za-z0-9_\-]{20,}" },
    @{ Name = "github-token"; Pattern = "gh[pousr]_[A-Za-z0-9_]{20,}" },
    @{ Name = "google-api-key"; Pattern = "AIza[0-9A-Za-z_\-]{35}" }
)

$excludedPathFragments = @(
    "\bin\",
    "/bin/",
    "\obj\",
    "/obj/",
    "\.git\",
    "/.git/",
    "\TestResults\",
    "/TestResults/",
    "\coverage\",
    "/coverage/",
    "\playwright-report\",
    "/playwright-report/",
    "\secrets\",
    "/secrets/"
)

$trackedSecretFiles = git -C $Root ls-files secrets | Where-Object { $_ -ne "secrets/.gitkeep" }
if (-not [string]::IsNullOrWhiteSpace($trackedSecretFiles)) {
    throw "Secret files must not be tracked:`n$trackedSecretFiles"
}

$files = Get-ChildItem -Path $Root -Recurse -File -Force |
    Where-Object {
        $fullName = $_.FullName
        -not ($excludedPathFragments | Where-Object { $fullName.IndexOf($_, [StringComparison]::OrdinalIgnoreCase) -ge 0 })
    }

$findings = New-Object System.Collections.Generic.List[string]

foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $content) {
        continue
    }

    foreach ($pattern in $patterns) {
        if ($content -match $pattern.Pattern) {
            $relativePath = Resolve-Path -LiteralPath $file.FullName -Relative
            $findings.Add("$($pattern.Name): $relativePath")
        }
    }
}

if ($findings.Count -gt 0) {
    Write-Error ("Potential secrets found:`n" + ($findings -join "`n"))
}

Write-Host "Secret scan passed."
