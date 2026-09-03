[CmdletBinding()]
param(
    [long]$MaximumFileBytes = 10MB
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    $trackedFiles = @(git ls-files)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to enumerate tracked files.' }

    $errors = [System.Collections.Generic.List[string]]::new()
    $forbiddenExtensions = @('.db', '.db-shm', '.db-wal', '.gguf', '.onnx', '.pfx', '.snk')
    $binaryExtensions = @('.png', '.jpg', '.jpeg', '.webp', '.gif', '.ico', '.wav', '.mp3', '.ogg')
    $secretRules = @(
        @{ Name = 'private key'; Pattern = '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----' },
        @{ Name = 'OpenAI-style API key'; Pattern = '\bsk-[A-Za-z0-9_-]{20,}\b' },
        @{ Name = 'GitHub token'; Pattern = '\bgh[oprsu]_[A-Za-z0-9]{20,}\b' },
        @{ Name = 'AWS access key'; Pattern = '\bAKIA[0-9A-Z]{16}\b' }
    )

    foreach ($relativePath in $trackedFiles) {
        $path = Join-Path $repositoryRoot $relativePath
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
        $file = Get-Item -LiteralPath $path
        $extension = $file.Extension.ToLowerInvariant()
        $normalizedPath = $relativePath.Replace('\', '/')

        if ($file.Length -gt $MaximumFileBytes) {
            $errors.Add("Oversized tracked file ($($file.Length) bytes): $relativePath")
        }
        if ($forbiddenExtensions -contains $extension) {
            $errors.Add("Forbidden generated, credential, database, or model file: $relativePath")
        }
        if ($normalizedPath -match '^(models|runtime|data)/') {
            $errors.Add("Forbidden runtime data directory entry: $relativePath")
        }
        if ($binaryExtensions -contains $extension -or $file.Length -gt 2MB) { continue }

        $content = Get-Content -LiteralPath $path -Raw
        foreach ($rule in $secretRules) {
            if ($content -match $rule.Pattern) {
                $errors.Add("Possible $($rule.Name) in tracked file: $relativePath")
            }
        }
    }

    if ($errors.Count -gt 0) {
        $errors | ForEach-Object { Write-Error $_ }
        exit 1
    }
    Write-Host "Repository verification passed for $($trackedFiles.Count) tracked files."
}
finally {
    Pop-Location
}
