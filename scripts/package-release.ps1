[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '0.0.2',
    [ValidateSet('win-x64')]
    [string]$RuntimeIdentifier = 'win-x64',
    [switch]$IncludeModels,
    [switch]$SkipValidation
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$releaseRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot "artifacts/releases/v$Version"))
$stagingRoot = Join-Path $releaseRoot 'staging'
$selfContainedPublishRoot = Join-Path $stagingRoot 'publish-self-contained'
$frameworkPublishRoot = Join-Path $stagingRoot 'publish-framework-dependent'
$liteName = "Pry-v$Version-$RuntimeIdentifier-lite"
$minimalName = "Pry-v$Version-$RuntimeIdentifier-minimal"
$fullName = "Pry-v$Version-$RuntimeIdentifier-full"
$liteRoot = Join-Path $stagingRoot $liteName
$minimalRoot = Join-Path $stagingRoot $minimalName
$fullRoot = Join-Path $stagingRoot $fullName

function Assert-UnderArtifacts([string]$Path) {
    $artifactsRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'artifacts'))
    $resolved = [IO.Path]::GetFullPath($Path)
    if (-not $resolved.StartsWith($artifactsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside artifacts: $resolved"
    }
}

function Invoke-Checked([string]$FilePath, [string[]]$Arguments) {
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE"
    }
}

function Copy-DirectoryContents([string]$Source, [string]$Destination) {
    if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
        throw "Required directory is missing: $Source"
    }
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Copy-Item -Path (Join-Path $Source '*') -Destination $Destination -Recurse -Force
}

function Copy-InferenceRuntime([string]$Destination) {
    $source = Join-Path $projectRoot 'runtime'
    if (-not (Test-Path -LiteralPath (Join-Path $source 'llama-server.exe') -PathType Leaf)) {
        throw "Required inference runtime is missing: $source"
    }
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Copy-Item -LiteralPath (Join-Path $source 'llama-server.exe') -Destination $Destination -Force
    Get-ChildItem -LiteralPath $source -Filter '*.dll' -File |
        Copy-Item -Destination $Destination -Force
}

function Add-ModelFile([string]$RelativePath, [string]$ExpectedSha256) {
    $source = Join-Path $projectRoot $RelativePath
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Required model is missing: $RelativePath"
    }
    $actual = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $ExpectedSha256) {
        throw "Model checksum mismatch: $RelativePath (got $actual)"
    }
    $destination = Join-Path $fullRoot $RelativePath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    try {
        New-Item -ItemType HardLink -Path $destination -Target $source | Out-Null
    }
    catch {
        Copy-Item -LiteralPath $source -Destination $destination -Force
    }
}

function Copy-PackageDocuments([string]$PackageRoot) {
    Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $PackageRoot
    Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $PackageRoot
    Copy-Item -LiteralPath (Join-Path $projectRoot 'NOTICE') -Destination $PackageRoot
    Copy-Item -LiteralPath (Join-Path $projectRoot 'ASSETS_LICENSE.md') -Destination $PackageRoot
    Copy-Item -LiteralPath (Join-Path $projectRoot 'FAN_CONTENT_POLICY.md') -Destination $PackageRoot
    Copy-DirectoryContents (Join-Path $projectRoot 'licenses') (Join-Path $PackageRoot 'licenses')
    New-Item -ItemType Directory -Force -Path (Join-Path $PackageRoot 'scripts') | Out-Null
    Copy-Item -LiteralPath (Join-Path $projectRoot 'scripts/install-local-runtime.ps1') -Destination (Join-Path $PackageRoot 'scripts')
    Copy-Item -LiteralPath (Join-Path $projectRoot 'scripts/download-models.ps1') -Destination (Join-Path $PackageRoot 'scripts')
}

function Write-PackageMetadata([string]$PackageRoot, [string]$Variant, [bool]$SelfContained, [object[]]$Models) {
    $commit = (git -C $projectRoot rev-parse --short=12 HEAD).Trim()
    $manifest = [ordered]@{
        product = 'Pry'
        version = $Version
        runtimeIdentifier = $RuntimeIdentifier
        variant = $Variant
        sourceCommit = $commit
        selfContained = $SelfContained
        generatedAtUtc = [DateTime]::UtcNow.ToString('O')
        models = $Models
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PackageRoot 'release-manifest.json') -Encoding utf8

    $variantText = if ($Variant -eq 'full') {
        '完整模型包：包含预设文字、视觉和语音模型，可离线使用。Qwen2.5-VL-3B 仅许可非商业用途，请阅读 licenses 目录。'
    } elseif ($Variant -eq 'minimal') {
        '环境依赖极简包：不包含 .NET、llama.cpp/CUDA 本地推理运行库或模型权重。请先安装 Windows x64 的 .NET 10 ASP.NET Core Runtime。使用本地模型时，再运行 .\scripts\install-local-runtime.ps1 安装 CPU 运行时和入门模型；自备模型时可使用 -RuntimeOnly。'
    } else {
        '自包含运行库包：包含 Pry、.NET/ASP.NET Core 和 CUDA llama.cpp 本地推理运行库，但不包含模型权重。无需安装 .NET；本地聊天、图片理解和语音识别仍需自行安装模型，或配置兼容服务。'
    }
    @"
Pry v$Version ($RuntimeIdentifier)

$variantText

启动：双击 Pry.App.exe。
数据：保存在 %LOCALAPPDATA%\PryCompanion，不会写入本目录。
许可：程序代码为 Apache-2.0；原创素材和第三方运行库/模型使用各自许可。分发或使用前请阅读 LICENSE、NOTICE、ASSETS_LICENSE.md、FAN_CONTENT_POLICY.md 和 licenses 目录。
"@ | Set-Content -LiteralPath (Join-Path $PackageRoot '发行包说明.txt') -Encoding utf8
}

function New-Zip([string]$DirectoryName) {
    $zipPath = Join-Path $releaseRoot "$DirectoryName.zip"
    if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
    Push-Location $stagingRoot
    try {
        Invoke-Checked 'tar.exe' @('-a', '-cf', $zipPath, $DirectoryName)
    }
    finally { Pop-Location }
    & tar.exe -tf $zipPath | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Archive verification failed: $zipPath" }
    return Get-Item -LiteralPath $zipPath
}

Assert-UnderArtifacts $releaseRoot
if (Test-Path -LiteralPath $stagingRoot) { Remove-Item -LiteralPath $stagingRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $releaseRoot, $stagingRoot | Out-Null

if (-not $SkipValidation) {
    Invoke-Checked 'dotnet' @('build', (Join-Path $projectRoot 'Pry.slnx'), '-c', 'Release')
    Invoke-Checked 'dotnet' @('test', (Join-Path $projectRoot 'Pry.slnx'), '-c', 'Release', '--no-build')
    Invoke-Checked 'git' @('-C', $projectRoot, 'diff', '--check')
}

Invoke-Checked 'dotnet' @(
    'publish', (Join-Path $projectRoot 'src/Pry.App/Pry.App.csproj'),
    '-c', 'Release', '-r', $RuntimeIdentifier, '--self-contained', 'true',
    '-p:DebugType=None', '-p:DebugSymbols=false', '-o', $selfContainedPublishRoot
)

Invoke-Checked 'dotnet' @(
    'publish', (Join-Path $projectRoot 'src/Pry.App/Pry.App.csproj'),
    '-c', 'Release', '-r', $RuntimeIdentifier, '--self-contained', 'false',
    '-p:DebugType=None', '-p:DebugSymbols=false', '-o', $frameworkPublishRoot
)

Copy-DirectoryContents $selfContainedPublishRoot $liteRoot
Copy-InferenceRuntime (Join-Path $liteRoot 'runtime')
Copy-PackageDocuments $liteRoot

Copy-DirectoryContents $frameworkPublishRoot $minimalRoot
Copy-PackageDocuments $minimalRoot

$modelSpecs = @(
    [ordered]@{ path = 'models/qwen3-1.7b-q4_k_m.gguf'; sha256 = 'd2387ca2dbfee2ffabce7120d3770dadca0b293052bc2f0e138fdc940d9bc7b5' },
    [ordered]@{ path = 'models/qwen3.5-9b-q4_k_m.gguf'; sha256 = 'd784ce9eda1a5a7b51e8f705a9e6310844bf4f173654d115823c775fdea56d43' },
    [ordered]@{ path = 'models/mmproj-qwen3.5-9b-f16.gguf'; sha256 = '97f420245a85ce129bb764e86a5e21e27d782fe6d6056c6839b9c5fdb8f38289' },
    [ordered]@{ path = 'models/qwen2.5-vl-3b-instruct-q4_k_m.gguf'; sha256 = 'd02fe9b69ad8cadbbd228e387667af66612c44bed29ffc8eb1e7caf9ac486c12' },
    [ordered]@{ path = 'models/mmproj-qwen2.5-vl-3b-q8_0.gguf'; sha256 = '980c9b2f78c04e6cff93d277ada09e768394f112d75db3b4e9dea8a69f9fb904' },
    [ordered]@{ path = 'models/sensevoice-small-int8/model.int8.onnx'; sha256 = '12ca1a2ae7ecf3e0019ef2822307ee0b5cadc9196569e379b4c4026f8205276d' },
    [ordered]@{ path = 'models/sensevoice-small-int8/tokens.txt'; sha256 = 'f449eb28dc567533d7fa59be34e2abca8784f771850c78a47fb731a31429a1dc' }
)

New-Item -ItemType Directory -Force -Path (Join-Path $liteRoot 'models') | Out-Null
'此版本不含模型。请按 Resources/appsettings.json 中的相对路径放置模型，或在应用设置中配置兼容服务。' |
    Set-Content -LiteralPath (Join-Path $liteRoot 'models/README.txt') -Encoding utf8

Write-PackageMetadata $liteRoot 'lite' $true @()
$liteZip = New-Zip $liteName

New-Item -ItemType Directory -Force -Path (Join-Path $minimalRoot 'models') | Out-Null
'此版本不含模型。请运行 scripts/install-local-runtime.ps1，或在应用设置中配置兼容服务。' |
    Set-Content -LiteralPath (Join-Path $minimalRoot 'models/README.txt') -Encoding utf8
Write-PackageMetadata $minimalRoot 'minimal' $false @()
$minimalZip = New-Zip $minimalName
$archives = @($liteZip, $minimalZip)

if ($IncludeModels) {
    Copy-DirectoryContents $selfContainedPublishRoot $fullRoot
    Copy-InferenceRuntime (Join-Path $fullRoot 'runtime')
    Copy-PackageDocuments $fullRoot
    foreach ($model in $modelSpecs) { Add-ModelFile $model.path $model.sha256 }
    Write-PackageMetadata $fullRoot 'full' $true $modelSpecs
    $archives += New-Zip $fullName
}

$hashRows = foreach ($archive in $archives) {
    $hash = (Get-FileHash -LiteralPath $archive.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($archive.Name)"
}
$hashRows | Set-Content -LiteralPath (Join-Path $releaseRoot 'SHA256SUMS.txt') -Encoding ascii

[pscustomobject]@{
    LiteZip = $liteZip.FullName
    LiteBytes = $liteZip.Length
    MinimalZip = $minimalZip.FullName
    MinimalBytes = $minimalZip.Length
    FullZip = if ($IncludeModels) { $archives[-1].FullName } else { $null }
    FullBytes = if ($IncludeModels) { $archives[-1].Length } else { $null }
    Checksums = (Join-Path $releaseRoot 'SHA256SUMS.txt')
} | Format-List
