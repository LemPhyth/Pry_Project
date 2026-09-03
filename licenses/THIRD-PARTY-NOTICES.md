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

## Optional local runtime and models

| Component | Pinned artifact | License | Source |
|---|---|---|---|
| llama.cpp | `b10516`, Windows x64 CPU/CUDA | MIT | https://github.com/ggml-org/llama.cpp/releases/tag/b10516 |
| Qwen3-1.7B | `ggml-org/Qwen3-1.7B-GGUF`, `Q4_K_M` | Apache-2.0 | https://huggingface.co/ggml-org/Qwen3-1.7B-GGUF |

Integrity value used by the development installer:

- `Qwen3-1.7B-Q4_K_M.gguf`: SHA-256 `d2387ca2dbfee2ffabce7120d3770dadca0b293052bc2f0e138fdc940d9bc7b5`

Copies of the licenses currently bundled for optional components are stored beside this file. Model weights and runtime binaries are intentionally excluded from the Git repository.
- `llama-b10516-bin-win-cpu-x64.zip`: SHA-256 `fbbbc55e0eb2e1b07f9dcb9488616c98ed47d9003b90e15e7c8c7812c4307cd3`

The complete upstream license texts are stored beside this notice.
