[CmdletBinding()]
param(
    [switch]$UseMirror
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$downloadDirectory = Join-Path $projectRoot '.downloads'
$runtimeDirectory = Join-Path $projectRoot 'runtime'
$modelDirectory = Join-Path $projectRoot 'models'
$runtimeArchive = Join-Path $downloadDirectory 'llama-b10516-win-cpu-x64.zip'
$modelPath = Join-Path $modelDirectory 'qwen3-1.7b-q4_k_m.gguf'

$runtimeUrl = 'https://github.com/ggml-org/llama.cpp/releases/download/b10516/llama-b10516-bin-win-cpu-x64.zip'
$modelPrimaryUrl = 'https://huggingface.co/ggml-org/Qwen3-1.7B-GGUF/resolve/main/Qwen3-1.7B-Q4_K_M.gguf?download=true'
$modelMirrorUrl = 'https://hf-mirror.com/ggml-org/Qwen3-1.7B-GGUF/resolve/main/Qwen3-1.7B-Q4_K_M.gguf'
$runtimeSha256 = 'fbbbc55e0eb2e1b07f9dcb9488616c98ed47d9003b90e15e7c8c7812c4307cd3'
$modelSha256 = 'd2387ca2dbfee2ffabce7120d3770dadca0b293052bc2f0e138fdc940d9bc7b5'

New-Item -ItemType Directory -Force -Path $downloadDirectory, $runtimeDirectory, $modelDirectory | Out-Null

function Test-ExpectedHash([string]$Path, [string]$Expected) {
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.Equals($Expected, [StringComparison]::OrdinalIgnoreCase)
}

function Receive-VerifiedFile([string]$Url, [string]$Target, [string]$ExpectedHash) {
    if (Test-ExpectedHash $Target $ExpectedHash) {
        Write-Host "已存在并通过校验：$Target"
        return
    }
    if (Test-Path -LiteralPath $Target) {
        throw "目标文件已存在但哈希不匹配，请手动检查后移走：$Target"
    }
    $partial = "$Target.part"
    & curl.exe -L --fail --connect-timeout 60 --retry 5 --retry-all-errors -C - --progress-bar -o $partial $Url
    if ($LASTEXITCODE -ne 0) { throw "下载失败：$Url" }
    if (-not (Test-ExpectedHash $partial $ExpectedHash)) { throw "下载完成但 SHA-256 校验失败：$partial" }
    Move-Item -LiteralPath $partial -Destination $Target
}

Receive-VerifiedFile $runtimeUrl $runtimeArchive $runtimeSha256
$selectedModelUrl = if ($UseMirror) { $modelMirrorUrl } else { $modelPrimaryUrl }
Receive-VerifiedFile $selectedModelUrl $modelPath $modelSha256

Expand-Archive -LiteralPath $runtimeArchive -DestinationPath $runtimeDirectory -Force
if (-not (Test-Path -LiteralPath (Join-Path $runtimeDirectory 'llama-server.exe'))) {
    throw '运行时解压后未找到 llama-server.exe。'
}

Write-Host '本地运行时和 Qwen3-1.7B Q4_K_M 已安装并通过校验。'
