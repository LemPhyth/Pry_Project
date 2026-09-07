# Third-party notices

Pry depends on third-party software and can optionally be used with separately downloaded runtimes and model weights. Each component remains subject to its own license; Pry's Apache-2.0 license does not replace those terms.

## Direct application dependencies

| Component | Version | License | Project |
|---|---:|---|---|
| Avalonia | 11.3.20 | MIT | https://github.com/AvaloniaUI/Avalonia |
| NAudio | 2.2.1 | MIT | https://github.com/naudio/NAudio |
| SkiaSharp | 2.88.9 | MIT | https://github.com/mono/SkiaSharp |
| Tmds.DBus.Protocol | 0.94.2 | MIT | https://github.com/tmds/Tmds.DBus |
| Microsoft.Data.Sqlite | 10.0.0 | MIT | https://github.com/dotnet/efcore |
| SQLitePCLRaw.lib.e_sqlite3 | 2.1.13 | Apache-2.0 | https://github.com/ericsink/SQLitePCL.raw |
| sherpa-onnx | 1.13.5 | Apache-2.0 | https://github.com/k2-fsa/sherpa-onnx |
| xUnit.net | 3.2.2 | Apache-2.0 | https://github.com/xunit/xunit |
| Microsoft.TestPlatform | 18.0.1 | MIT | https://github.com/microsoft/vstest |

Transitive NuGet dependencies are recorded in the generated restore assets and may add further notices. Release packaging must include all license texts and notices required by the exact dependency versions being distributed.

## Local runtime and optional models

| Component | Pinned artifact | License | Source |
|---|---|---|---|
| llama.cpp | build `10516` / commit `b95502ba9`, Windows x64 CUDA | MIT | https://github.com/ggml-org/llama.cpp |
| NVIDIA CUDA runtime libraries | CUDA 12 family redistributable DLLs shipped with the llama.cpp runtime | NVIDIA CUDA Toolkit EULA | https://docs.nvidia.com/cuda/eula/ |
| Qwen3-1.7B | `ggml-org/Qwen3-1.7B-GGUF`, `Q4_K_M` | Apache-2.0 | https://huggingface.co/ggml-org/Qwen3-1.7B-GGUF |
| Qwen3.5-9B | `bartowski/Qwen_Qwen3.5-9B-GGUF`, `Q4_K_M` plus F16 mmproj; derived from `Qwen/Qwen3.5-9B` | Apache-2.0 | https://huggingface.co/Qwen/Qwen3.5-9B |
| Qwen2.5-VL-3B-Instruct | `ggml-org/Qwen2.5-VL-3B-Instruct-GGUF`, `Q4_K_M` plus Q8_0 mmproj | Qwen Research License (non-commercial only) | https://huggingface.co/Qwen/Qwen2.5-VL-3B-Instruct |
| SenseVoiceSmall INT8 | `Mr7Cat/sherpa-onnx-sense-voice-zh-en-ja-ko-yue-int8-2025-09-09` conversion | FunASR Model Open Source License 1.1 | https://github.com/modelscope/FunASR/blob/main/MODEL_LICENSE |

Integrity values used by the optional full-package builder:

- `Qwen3-1.7B-Q4_K_M.gguf`: SHA-256 `d2387ca2dbfee2ffabce7120d3770dadca0b293052bc2f0e138fdc940d9bc7b5`
- `Qwen_Qwen3.5-9B-Q4_K_M.gguf`: SHA-256 `d784ce9eda1a5a7b51e8f705a9e6310844bf4f173654d115823c775fdea56d43`
- `mmproj-Qwen_Qwen3.5-9B-f16.gguf`: SHA-256 `97f420245a85ce129bb764e86a5e21e27d782fe6d6056c6839b9c5fdb8f38289`
- `Qwen2.5-VL-3B-Instruct-Q4_K_M.gguf`: SHA-256 `d02fe9b69ad8cadbbd228e387667af66612c44bed29ffc8eb1e7caf9ac486c12`
- `mmproj-Qwen2.5-VL-3B-Instruct-Q8_0.gguf`: SHA-256 `980c9b2f78c04e6cff93d277ada09e768394f112d75db3b4e9dea8a69f9fb904`
- `SenseVoiceSmall model.int8.onnx`: SHA-256 `12ca1a2ae7ecf3e0019ef2822307ee0b5cadc9196569e379b4c4026f8205276d`
- `SenseVoiceSmall tokens.txt`: SHA-256 `f449eb28dc567533d7fa59be34e2abca8784f771850c78a47fb731a31429a1dc`

Copies of the licenses currently bundled for optional components are stored beside this file. Model weights and runtime binaries are intentionally excluded from the Git repository.
- `llama-b10516-bin-win-cpu-x64.zip`: SHA-256 `fbbbc55e0eb2e1b07f9dcb9488616c98ed47d9003b90e15e7c8c7812c4307cd3`

The complete Qwen Research and FunASR model license texts are stored beside this notice. Apache-2.0 is included at the package root and with Qwen3-1.7B; the llama.cpp MIT text is also stored beside this notice. NVIDIA's current CUDA Toolkit EULA is linked above and applies to its redistributable DLLs.

The v0.0.2 public package does not contain model weights. Any future package that contains Qwen2.5-VL-3B is restricted to non-commercial distribution under its model license.
