# Third-party notices

Pry depends on third-party software and can optionally be used with separately downloaded runtimes and model weights. Each component remains subject to its own license; Pry's GPL-3.0-only code license does not replace those terms.

## Direct application dependencies

| Component | Version | License | Project |
|---|---:|---|---|
| Avalonia | 11.3.20 | MIT | https://github.com/AvaloniaUI/Avalonia |
| NAudio | 2.2.1 | MIT | https://github.com/naudio/NAudio |
| SkiaSharp | 2.88.9 | MIT | https://github.com/mono/SkiaSharp |
| Tmds.DBus.Protocol | 0.94.2 | MIT | https://github.com/tmds/Tmds.DBus |
| Microsoft.Data.Sqlite | 10.0.0 | MIT | https://github.com/dotnet/efcore |
| Microsoft.Extensions.Logging.Abstractions | 10.0.0 | MIT | https://github.com/dotnet/runtime |
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

## Upstream code reused in Pry

Pry borrows architecture and mechanisms from researched projects. Verbatim or
line-by-line code reuse is restricted to the authorized list in
`docs/upstream-license-compatibility.md` §4.4 (author decision 2026-09-21):

| Upstream | License | Code reuse |
|---|---|---|
| `Mai-with-u/MaiBot` (MaiCore) | GPL-3.0 | ✅ allowed (this project is GPL-3.0-only); check the per-directory LICENSE of the exact path before porting |
| `satorijs/satori` | MIT | ✅ allowed with the copyright/notice text retained under `licenses/upstream/` |
| `AstrBotDevs/AstrBot` | AGPL-3.0 | ❌ mechanism reference only — AGPL code cannot be placed under a GPL-3.0-only work |
| `KroMiose/nekro-agent` | custom "Nekro Agent 开源协议 V1.1" | ❌ mechanism and market research only |
| `menglimi/astrbot_plugin_private_companion` | none published | ❌ mechanism and market research only |

Every file that is copied or ported must carry a provenance header naming
`<repo>@<commit>:<path>`, retain the upstream copyright notice, and have its
upstream license text stored under `licenses/upstream/<repo>_<commit>/`.
Release packaging must include that directory.

| Pry file | Upstream source (`repo@commit:path`) | Type | Verified |
|---|---|---|---|
| `src/Pry.Core/Runtime/MessageIntervalStats.cs` | `Mai-with-u/MaiBot@fb4a8bd917d6fe373df9e3c22289d229e7f1b344:src/maisaka/runtime.py` (`_record_external_message_interval`, `_prune_recent_external_message_intervals`, `_get_recent_average_external_message_interval`) | ported | 2026-09-23 |
| `src/Pry.Core/Runtime/ReplyNecessity.cs` | `Mai-with-u/MaiBot@fb4a8bd917d6fe373df9e3c22289d229e7f1b344:src/maisaka/reply_necessity.py` (`score_reply_necessity` and helpers) | ported | 2026-09-23 |
| `src/Pry.Core/Runtime/TurnGates.cs` | `Mai-with-u/MaiBot@fb4a8bd917d6fe373df9e3c22289d229e7f1b344:src/maisaka/turn_gates.py`, `src/maisaka/mode_policy.py`, `src/maisaka/runtime.py` (`_get_message_trigger_threshold`) | ported | 2026-09-23 |
| `src/Pry.Core/Runtime/IdleBackoffController.cs` | `Mai-with-u/MaiBot@fb4a8bd917d6fe373df9e3c22289d229e7f1b344:src/maisaka/idle_backoff.py`, `src/maisaka/mode_policy.py` | ported | 2026-09-23 |
| `src/Pry.Core/Runtime/TurnScheduler.cs` | `Mai-with-u/MaiBot@fb4a8bd917d6fe373df9e3c22289d229e7f1b344:src/maisaka/turn_scheduler.py` (`schedule_message_turn` gate ordering only; the surrounding state fences are Pry-original) | mechanism | 2026-09-23 |

The four `ported` files carry a provenance header naming the upstream repo,
commit and path. `TurnScheduler.cs` borrowed only the order of the gate chain,
so its source is recorded in this table and in its own type documentation
rather than in a header.

**Parameter provenance — re-checked 2026-09-23 against the upstream files at the
locked commit.** The scoring layers, curve shapes and most numeric magnitudes in
`ReplyNecessity.cs` and `MessageIntervalStats.cs` were taken from upstream's
source-level constants. Pry changed only three of them — the trigger score
(80 → 70), the private-chat / focus relevance (40 → 45) and the interval sample
window (1800 s → 900 s) — and left the rest equal to upstream. §4.3 of
`docs/upstream-license-compatibility.md` forbids importing group-chat tuned
thresholds, so these values are recorded here as **upstream-derived and
uncalibrated**, not as Pry parameters. Consequently none of them is a release
default: the whole trigger chain ships inert (`ReplyTriggerMode.MessageCount`,
frequency 1.0, private chat) — which is also upstream's own shipping posture
(`official_configs.py:537-614`: `reply_trigger_mode = "frequency"`, talk
frequencies default to 1).

Two further provenance facts from the same re-check. `IdleBackoffController.cs`
takes its four magnitudes from upstream's *shipped* defaults
(`src/config/official_configs.py:645-709`): Pry changed the base (15 s → 10 s),
the cap (300 s → 180 s) and the start count (2 → 3), while
`BypassPendingCount = 6` equals upstream — so that file mixes tuned and
inherited values and all four remain uncalibrated. And the detection vocabulary
in `ReplyNecessityOptions` (question / request / opinion term lists and the
short-reaction set) is upstream's group-chat corpus, copied as part of the
`ported` type; the upstream character name and the competitor-assistant list
were deliberately not carried over (`CharacterNames` comes from the input
snapshot, `OtherAssistantNames` defaults to empty). The vocabulary is lawful
here but is still borrowed, unaudited wording and is registered as
calibration-scope debt.

The per-value table, the deviation register and the open author decision are in
`docs/phase2-runtime-contract.md` §5 and §6.
