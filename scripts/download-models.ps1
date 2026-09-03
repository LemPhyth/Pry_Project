$ErrorActionPreference = 'Stop'

$projectDirectory = Split-Path -Parent $PSScriptRoot
$modelDirectory = Join-Path $projectDirectory 'models'
New-Item -ItemType Directory -Force -Path $modelDirectory | Out-Null

function Get-CheckedModelFile {
    param(
        [Parameter(Mandatory)] [string] $Url,
        [Parameter(Mandatory)] [string] $FileName,
        [Parameter(Mandatory)] [string] $ExpectedSha256
    )

    $destination = Join-Path $modelDirectory $FileName
    $partial = "$destination.part"
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    if (Test-Path -LiteralPath $destination) {
        $actual = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -eq $ExpectedSha256) {
            Write-Output "Verified existing: $FileName"
            return
        }
        throw "Existing file checksum mismatch: $FileName"
    }

    & curl.exe --fail --location --retry 8 --retry-delay 3 --continue-at - --output $partial $Url
    if ($LASTEXITCODE -ne 0) { throw "Download failed: $FileName" }
    $actual = (Get-FileHash -LiteralPath $partial -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $ExpectedSha256) { throw "Checksum mismatch for $FileName (got $actual)" }
    Move-Item -LiteralPath $partial -Destination $destination
    Write-Output "Downloaded and verified: $FileName"
}

$modelScope = 'https://modelscope.cn/api/v1/models'
Get-CheckedModelFile `
    -Url "$modelScope/bartowski/Qwen_Qwen3.5-9B-GGUF/repo?Revision=master&FilePath=Qwen_Qwen3.5-9B-Q4_K_M.gguf" `
    -FileName 'qwen3.5-9b-q4_k_m.gguf' `
    -ExpectedSha256 'd784ce9eda1a5a7b51e8f705a9e6310844bf4f173654d115823c775fdea56d43'
Get-CheckedModelFile `
    -Url "$modelScope/bartowski/Qwen_Qwen3.5-9B-GGUF/repo?Revision=master&FilePath=mmproj-Qwen_Qwen3.5-9B-f16.gguf" `
    -FileName 'mmproj-qwen3.5-9b-f16.gguf' `
    -ExpectedSha256 '97f420245a85ce129bb764e86a5e21e27d782fe6d6056c6839b9c5fdb8f38289'
Get-CheckedModelFile `
    -Url "$modelScope/ggml-org/Qwen2.5-VL-3B-Instruct-GGUF/repo?Revision=master&FilePath=Qwen2.5-VL-3B-Instruct-Q4_K_M.gguf" `
    -FileName 'qwen2.5-vl-3b-instruct-q4_k_m.gguf' `
    -ExpectedSha256 'd02fe9b69ad8cadbbd228e387667af66612c44bed29ffc8eb1e7caf9ac486c12'
Get-CheckedModelFile `
    -Url "$modelScope/ggml-org/Qwen2.5-VL-3B-Instruct-GGUF/repo?Revision=master&FilePath=mmproj-Qwen2.5-VL-3B-Instruct-Q8_0.gguf" `
    -FileName 'mmproj-qwen2.5-vl-3b-q8_0.gguf' `
    -ExpectedSha256 '980c9b2f78c04e6cff93d277ada09e768394f112d75db3b4e9dea8a69f9fb904'

$senseVoiceRepository = 'Mr7Cat/sherpa-onnx-sense-voice-zh-en-ja-ko-yue-int8-2025-09-09'
Get-CheckedModelFile `
    -Url "$modelScope/$senseVoiceRepository/repo?Revision=master&FilePath=model.int8.onnx" `
    -FileName 'sensevoice-small-int8/model.int8.onnx' `
    -ExpectedSha256 '12ca1a2ae7ecf3e0019ef2822307ee0b5cadc9196569e379b4c4026f8205276d'
Get-CheckedModelFile `
    -Url "$modelScope/$senseVoiceRepository/repo?Revision=master&FilePath=tokens.txt" `
    -FileName 'sensevoice-small-int8/tokens.txt' `
    -ExpectedSha256 'f449eb28dc567533d7fa59be34e2abca8784f771850c78a47fb731a31429a1dc'

$senseVoiceDirectory = Join-Path $modelDirectory 'sensevoice-small-int8'
$senseVoiceModel = Join-Path $senseVoiceDirectory 'model.int8.onnx'
$onnx = Get-Item -LiteralPath $senseVoiceModel
$tokens = Get-Item -LiteralPath (Join-Path $senseVoiceDirectory 'tokens.txt')
if ($onnx.Length -lt 200MB -or $tokens.Length -lt 100KB) {
    throw 'SenseVoice extracted files are incomplete'
}

Write-Output 'ALL_DOWNLOADS_COMPLETE'
