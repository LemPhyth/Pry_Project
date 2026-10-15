<#
    verify-window-close-paths.ps1 - real-machine instrument for task 0-5 (tray residency vs. a real exit).

    WHY THIS EXISTS
    The unit gates (tests/Pry.App.Tests/TrayResidencyBoundaryTests.cs) prove the source shape. This script proves the
    same ruling on a running process, because "closing the window only hides it" is only worth something if the live
    process keeps its embedded backend and its discovery file after the close request.

    JUDGEMENT SURFACE: three close paths x three observables
      path A: WM_CLOSE posted to the visible main window            -> hide only (alive + serving + descriptor present)
      path B: a second WM_CLOSE, now against the hidden window      -> still hide only
      path C: "Exit ..." chosen in the tray                         -> real exit (process gone + port closed + descriptor cleared)
    observables: the process, whether the loopback port still answers GET /health, and whether runtime.json is published.

    HARD RULES (reverse constraints shipped with the work order)
      - This script never force-kills a process. taskkill /F is not a judgement: it is not a graceful exit, so it would
        leave runtime.json behind and produce a red that describes the instrument, not the product.
      - Path C's trigger is manual (the native tray menu has no script-reachable entry point); every judgement is scripted.
        If nobody clicks, path C reports BLOCKED, never FAIL.
      - An isolated -DataDirectory is mandatory and the author's live directory is refused, both as the data root and as
        an untouched-before/after check.
      - The session token is never printed or stored; only its length and an 8-hex-character SHA-256 fingerprint.

    TOPOLOGY: whatever the App discovers is what we judge. If runtime.json names this App's own pid the backend is
    embedded; if it names a different live pid this run is topology A' (independent backend), and then path C must NOT
    clear the descriptor - the other process still owns it, and pretending otherwise would be a lie about someone
    else's backend.

    USAGE
      powershell -ExecutionPolicy Bypass -File scripts\verify-window-close-paths.ps1 `
        -DataDirectory <repo>\.local-notes\window-close-paths\walk1
      Optional: -AppExe <path to Pry.App.exe> -StartupBudgetSeconds 90 -WindowBudgetSeconds 30
              -ExitBudgetSeconds 20 -EvidenceDirectory <path>
      For A' launch the independent backend first with the SAME Pry__DataDirectory, then run this script.
#>
[CmdletBinding()]
param(
    [string]$AppExe = '',
    [string]$DataDirectory = '',
    [string]$EvidenceDirectory = '',
    [int]$StartupBudgetSeconds = 90,
    [int]$WindowBudgetSeconds = 30,
    [int]$ExitBudgetSeconds = 20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $AppExe) { $AppExe = Join-Path $repoRoot 'src\Pry.App\bin\Release\net10.0\Pry.App.exe' }
$liveDirectory = Join-Path $env:LOCALAPPDATA 'PryCompanion'
$results = New-Object System.Collections.Generic.List[object]
$runStamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
if (-not $EvidenceDirectory) { $EvidenceDirectory = Join-Path $repoRoot ('.local-notes\window-close-paths\{0}' -f $runStamp) }

function Add-Result([string]$Id, [string]$Verdict, [string]$Detail) {
    $results.Add([pscustomobject]@{ Id = $Id; Verdict = $Verdict; Detail = $Detail }) | Out-Null
    Write-Host ("{0,-58} {1,-8} {2}" -f $Id, $Verdict, $Detail)
}

function Write-Abort([string]$Reason) {
    Write-Host ''
    Write-Host ("BLOCKED: {0}" -f $Reason)
    exit 2
}

function Get-LatestWrite([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return 'absent' }
    $newest = Get-ChildItem -LiteralPath $Path -Recurse -File -ErrorAction SilentlyContinue |
        Sort-Object -Property LastWriteTimeUtc -Descending | Select-Object -First 1
    if (-not $newest) { return 'empty' }
    return ('{0}|{1:o}' -f $newest.FullName, $newest.LastWriteTimeUtc)
}

function Get-TokenFingerprint([string]$Value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($Value)) }
    finally { $sha.Dispose() }
    return (($hash[0..3] | ForEach-Object { $_.ToString('x2') }) -join '')
}

Add-Type -Language CSharp @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class PryWindowProbe
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);

    public static List<IntPtr> WindowsOf(int processId)
    {
        var found = new List<IntPtr>();
        var target = (uint)processId;
        EnumWindows(delegate(IntPtr handle, IntPtr parameter)
        {
            uint owner;
            GetWindowThreadProcessId(handle, out owner);
            if (owner == target) found.Add(handle);
            return true;
        }, IntPtr.Zero);
        return found;
    }

    public static bool IsVisible(IntPtr handle) { return IsWindow(handle) && IsWindowVisible(handle); }

    public static bool Close(IntPtr handle) { return PostMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero); }
}
'@

function Test-PortOpen([int]$Port) {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect('127.0.0.1', $Port, $null, $null)
        return $async.AsyncWaitHandle.WaitOne(700) -and $client.Connected
    }
    catch { return $false }
    finally { $client.Close() }
}

function Test-Health([int]$Port) {
    try {
        $response = Invoke-WebRequest -UseBasicParsing -TimeoutSec 3 ("http://127.0.0.1:{0}/health" -f $Port)
        return $response.StatusCode -eq 200
    }
    catch { return $false }
}

function Get-State($Process, $Descriptor, [string]$DescriptorPath) {
    $alive = $true
    try { $Process.Refresh() } catch { }
    try { $alive = -not $Process.HasExited } catch { $alive = $false }
    $port = 0
    if ($Descriptor) { $port = [int]$Descriptor.port }
    $listening = $false
    $healthy = $false
    if ($alive -and $port -gt 0) { $listening = Test-PortOpen $port; $healthy = Test-Health $port }
    $visible = 0
    $handles = @()
    if ($alive) {
        $handles = @([PryWindowProbe]::WindowsOf($Process.Id))
        $visible = @($handles | Where-Object { [PryWindowProbe]::IsVisible($_) }).Count
    }
    return [pscustomobject]@{
        Alive = $alive
        Port = $port
        Listening = $listening
        Healthy = $healthy
        VisibleWindows = $visible
        Handles = $handles
        DescriptorPublished = (Test-Path -LiteralPath $DescriptorPath)
    }
}

# ---- preflight ---------------------------------------------------------------------------------------------------------------------------------
if (-not (Test-Path -LiteralPath $AppExe)) { Write-Abort ("Pry.App.exe not found at {0}; build Release first." -f $AppExe) }
if (-not $DataDirectory) { Write-Abort '-DataDirectory is required: an isolated data root keeps this run off the author''s live directory.' }
# Windows PowerShell 5.1 has no Split-Path -Qualifier; use the .NET path API so this guard works on both shells.
$resolved = [System.IO.Path]::GetFullPath($DataDirectory).TrimEnd('\')
$liveRoot = [System.IO.Path]::GetFullPath($liveDirectory).TrimEnd('\')
# Refuse the live data root itself and anything below it - a subdirectory still writes into the author's real data.
if ($resolved.Equals($liveRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
    $resolved.StartsWith($liveRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    Write-Abort ("-DataDirectory must not be the live data root ({0}) or anything under it." -f $liveRoot)
}
New-Item -ItemType Directory -Force -Path $resolved | Out-Null
New-Item -ItemType Directory -Force -Path $EvidenceDirectory | Out-Null
$descriptorPath = Join-Path $resolved 'runtime.json'
Remove-Item -LiteralPath $descriptorPath -Force -ErrorAction SilentlyContinue
$liveBefore = Get-LatestWrite $liveDirectory

# ---- launch ------------------------------------------------------------------------------------------------------------------------------------
$env:Pry__DataDirectory = $resolved
$process = Start-Process -FilePath $AppExe -WorkingDirectory ([System.IO.Path]::GetDirectoryName($AppExe)) -PassThru
Write-Host ("launched pid {0}, data root {1}" -f $process.Id, $resolved)

$deadline = (Get-Date).AddSeconds($StartupBudgetSeconds)
$descriptor = $null
while ((Get-Date) -lt $deadline) {
    if (-not (Test-Path -LiteralPath $descriptorPath)) { Start-Sleep -Milliseconds 200; continue }
    try { $descriptor = Get-Content -LiteralPath $descriptorPath -Raw -Encoding UTF8 | ConvertFrom-Json } catch { $descriptor = $null }
    if ($descriptor) { break }
    Start-Sleep -Milliseconds 200
}
if (-not $descriptor) {
    $state = Get-State $process $null $descriptorPath
    Add-Result 'preflight.descriptor_published' 'FAIL' ("no readable runtime.json within {0}s (alive={1})" -f $StartupBudgetSeconds, $state.Alive)
    Write-Host ("left running as pid {0}; exit it from the tray, this instrument never force-kills" -f $process.Id)
    exit 1
}

$topology = 'embedded'
if ([int]$descriptor.pid -ne $process.Id) {
    $otherAlive = $true
    try { Get-Process -Id ([int]$descriptor.pid) -ErrorAction Stop | Out-Null } catch { $otherAlive = $false }
    $topology = if ($otherAlive) { 'independent-backend' } else { 'stale-descriptor' }
}

$facts = [ordered]@{
    runStamp = $runStamp
    appExe = $AppExe
    appPid = $process.Id
    dataDirectory = $resolved
    topology = $topology
    descriptorPid = [int]$descriptor.pid
    port = [int]$descriptor.port
    tokenLength = ([string]$descriptor.token).Length
    tokenFingerprint = Get-TokenFingerprint ([string]$descriptor.token)
    liveDirectoryBefore = $liveBefore
}

Add-Result 'preflight.backend_answers' $(if ((Test-Health ([int]$descriptor.port))) { 'PASS' } else { 'FAIL' }) `
    ("GET /health on {0} (topology {1})" -f ([int]$descriptor.port), $topology)
if ($topology -eq 'stale-descriptor') {
    Add-Result 'preflight.descriptor_owner' 'FAIL' ("runtime.json names pid {0}, which is gone" -f ([int]$descriptor.pid))
    exit 1
}

# ---- path A: WM_CLOSE on the visible window ------------------------------------------------------------------------------------------------------
# The discovery file is written by the embedded host long before Avalonia has painted a window, so the instrument waits
# for the window instead of judging an empty screen (walk1 measured 0 visible windows and blocked for exactly that reason).
$baseline = Get-State $process $descriptor $descriptorPath
$windowDeadline = (Get-Date).AddSeconds($WindowBudgetSeconds)
while ($baseline.VisibleWindows -eq 0 -and (Get-Date) -lt $windowDeadline) {
    Start-Sleep -Milliseconds 250
    $baseline = Get-State $process $descriptor $descriptorPath
}
$facts['visibleWindowsAtBaseline'] = $baseline.VisibleWindows
$facts['windowBudgetSeconds'] = $WindowBudgetSeconds
# One visible top-level is what "the main window" means here. If the run shows two of them (an enabled desktop-pet window
# adds one), picking a handle by index would let path A close something else than the thing the ruling is about, so the
# instrument says BLOCKED instead of guessing.
$target = $null
if ($baseline.VisibleWindows -eq 1) { $target = @($baseline.Handles | Where-Object { [PryWindowProbe]::IsVisible($_) })[0] }
if (-not $target) {
    Add-Result 'A.close_requested' 'BLOCKED' ("expected exactly one visible top-level window, found {0}" -f $baseline.VisibleWindows)
    Add-Result 'A.window_hidden' 'BLOCKED' 'path A never ran'
    Add-Result 'A.process_alive' 'BLOCKED' 'path A never ran'
    Add-Result 'A.backend_still_serving' 'BLOCKED' 'path A never ran'
    Add-Result 'A.descriptor_still_published' 'BLOCKED' 'path A never ran'
    Add-Result 'B.second_close_still_hides' 'BLOCKED' 'path A never ran'
}
else {
    [void][PryWindowProbe]::Close($target)
    $waited = (Get-Date).AddSeconds(8)
    do { Start-Sleep -Milliseconds 200; $after = Get-State $process $descriptor $descriptorPath }
    while ($after.VisibleWindows -gt 0 -and (Get-Date) -lt $waited)
    Add-Result 'A.window_hidden' $(if ($after.VisibleWindows -eq 0) { 'PASS' } else { 'FAIL' }) ("visible windows now {0}" -f $after.VisibleWindows)
    Add-Result 'A.process_alive' $(if ($after.Alive) { 'PASS' } else { 'FAIL' }) 'closing the window must not end the process'
    Add-Result 'A.backend_still_serving' $(if ($after.Healthy) { 'PASS' } else { 'FAIL' }) ("GET /health on {0}" -f $after.Port)
    Add-Result 'A.descriptor_still_published' $(if ($after.DescriptorPublished) { 'PASS' } else { 'FAIL' }) 'runtime.json must survive a hide'
    $facts['pathA'] = [ordered]@{ processAlive = $after.Alive; healthy = $after.Healthy; descriptorPublished = $after.DescriptorPublished }

    # ---- path B: a second WM_CLOSE, against the same window now that it is hidden ------------------------------------------------------------------
    # Deliberately the handle path A just closed, not "any invisible top-level" - the ruling is about a user pressing the
    # close button twice on that window, and a helper window would test a different thing.
    if ($after.VisibleWindows -gt 0) {
        Add-Result 'B.second_close_still_hides' 'BLOCKED' 'the window came back visible before the second close'
    }
    else {
        [void][PryWindowProbe]::Close($target)
        Start-Sleep -Milliseconds 800
        $second = Get-State $process $descriptor $descriptorPath
        Add-Result 'B.second_close_still_hides' $(if ($second.Alive -and $second.Healthy -and $second.VisibleWindows -eq 0) { 'PASS' } else { 'FAIL' }) `
            ("alive={0} healthy={1} visibleWindows={2}" -f $second.Alive, $second.Healthy, $second.VisibleWindows)
        $facts['pathB'] = [ordered]@{ processAlive = $second.Alive; healthy = $second.Healthy; visibleWindows = $second.VisibleWindows }
    }
}

# ---- path C: tray exit (manual trigger, scripted judgement) --------------------------------------------------------------------------------------
Write-Host ''
Write-Host 'PATH C needs one human click: open the tray icon menu and choose "Exit ..." (exit Pry), then confirm.'
Write-Host ("Waiting up to {0}s for that choice." -f $ExitBudgetSeconds)
$waited = (Get-Date).AddSeconds($ExitBudgetSeconds)
$exited = $false
while ((Get-Date) -lt $waited) {
    try { $exited = $process.HasExited } catch { $exited = $true }
    if ($exited) { break }
    Start-Sleep -Milliseconds 250
}
if (-not $exited) {
    Add-Result 'C.process_exited' 'BLOCKED' 'no tray exit was performed within the budget'
    Add-Result 'C.port_closed' 'BLOCKED' 'path C never completed'
    Add-Result 'C.descriptor_handled' 'BLOCKED' 'path C never completed'
}
else {
    $final = Get-State $process $descriptor $descriptorPath
    Add-Result 'C.process_exited' 'PASS' ("pid {0} is gone" -f $process.Id)
    Add-Result 'C.port_closed' $(if (-not (Test-PortOpen ([int]$descriptor.port))) { 'PASS' } else { 'FAIL' }) ("port {0}" -f ([int]$descriptor.port))
    if ($topology -eq 'embedded') {
        Add-Result 'C.descriptor_cleared' $(if (-not $final.DescriptorPublished) { 'PASS' } else { 'FAIL' }) `
            'a graceful exit deletes runtime.json; a leftover would advertise a dead entry'
    }
    else {
        Add-Result 'C.descriptor_left_for_its_owner' $(if ($final.DescriptorPublished) { 'PASS' } else { 'FAIL' }) `
            ("topology {0}: the backend belongs to pid {1} and must keep its own descriptor" -f $topology, ([int]$descriptor.pid))
    }
    $facts['pathC'] = [ordered]@{ descriptorPublished = $final.DescriptorPublished }
}

# ---- live directory untouched ---------------------------------------------------------------------------------------------------------------------------------
$liveAfter = Get-LatestWrite $liveDirectory
Add-Result 'Z.live_data_root_untouched' $(if ($liveAfter -eq $liveBefore) { 'PASS' } else { 'FAIL' }) `
    ("{0}: before={1} after={2}" -f $liveDirectory, $liveBefore, $liveAfter)
$facts['liveDirectoryAfter'] = $liveAfter

$evidencePath = Join-Path $EvidenceDirectory 'window-close-paths.json'
$evidence = [ordered]@{
    judgements = @($results | ForEach-Object { [ordered]@{ id = $_.Id; verdict = $_.Verdict; detail = $_.Detail } })
    facts = $facts
}
[System.IO.File]::WriteAllText($evidencePath, ($evidence | ConvertTo-Json -Depth 6))

$failed = @($results | Where-Object { $_.Verdict -eq 'FAIL' })
$blocked = @($results | Where-Object { $_.Verdict -eq 'BLOCKED' })
Write-Host ''
Write-Host ("evidence: {0}" -f $evidencePath)
Write-Host ("judgements: {0} total, {1} fail, {2} blocked" -f $results.Count, $failed.Count, $blocked.Count)
$alive = $false
try { $alive = -not $process.HasExited } catch { }
if ($alive) { Write-Host ("pid {0} is still running; exit it from the tray - this instrument never force-kills" -f $process.Id) }
if ($failed.Count -gt 0) { exit 1 }
if ($blocked.Count -gt 0) { exit 2 }
exit 0
