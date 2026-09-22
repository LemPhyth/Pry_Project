param(
    [string]$ResultsDirectory,
    [switch]$CatalogOnly
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$catalogPath = Join-Path $repoRoot 'docs/eval/scenarios.json'
$catalog = Get-Content -LiteralPath $catalogPath -Raw -Encoding UTF8 | ConvertFrom-Json
$categories = @('Timing','Behavior','Expression','Memory','Relationship','Capability','Persistence','Protocol','Recovery','Proactive')
$severities = @('Critical','High','Medium','Low')
$required = @('id','category','severity','scenario','input','initialState','expectedDecision','expectedOutputConstraints','forbiddenOutcome')
$ids = @{}
$sources = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'tests') -Recurse -Filter '*Tests.cs'

foreach ($case in $catalog.scenarios) {
    foreach ($field in $required) {
        if ([string]::IsNullOrWhiteSpace([string]$case.$field)) { throw "$($case.id): missing $field" }
    }
    if ($ids.ContainsKey($case.id)) { throw "Duplicate scenario: $($case.id)" }
    $ids[$case.id] = $true
    if ($categories -notcontains $case.category) { throw "Unknown category: $($case.category)" }
    if ($severities -notcontains $case.severity) { throw "Unknown severity: $($case.severity)" }
    foreach ($test in $case.automatedTests) {
        $parts = $test.Split('.')
        if ($parts.Count -lt 3) { throw "Expected Namespace.Class.Method mapping: $test" }
        $namespace = $parts[0..($parts.Count - 3)] -join '.'
        $file = @($sources | Where-Object {
            $_.BaseName -eq $parts[-2] -and (Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8) -match
                ('namespace\s+' + [regex]::Escape($namespace) + '\s*[;{]')
        })
        if ($file.Count -ne 1) { throw "Missing or ambiguous class: $test" }
        $source = Get-Content -LiteralPath $file[0].FullName -Raw -Encoding UTF8
        if ($source -notmatch ('\b' + [regex]::Escape($parts[-1]) + '\s*\(')) {
            throw "Stale test mapping: $test"
        }
    }
}

# v1 permanent IDs cannot silently disappear when a feature is deferred or renamed.
$permanent = @{TIM=8;BEH=7;EXP=5;MEM=8;REL=4;ACT=6;CAP=3;PER=4;PRO=4;REC=4}
foreach ($prefix in $permanent.Keys) {
    foreach ($number in 1..$permanent[$prefix]) {
        $id = '{0}-{1:000}' -f $prefix, $number
        if (-not $ids.ContainsKey($id)) { throw "Permanent scenario removed: $id" }
    }
}
Write-Output "Catalog valid: $($ids.Count) permanent scenarios. Mappings are partial evidence only."
if ($CatalogOnly) { exit 0 }
if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) { throw 'Provide a fresh TRX results directory or -CatalogOnly.' }
$resultRoot = (Resolve-Path -LiteralPath $ResultsDirectory).Path
$trxFiles = @(Get-ChildItem -LiteralPath $resultRoot -Filter '*.trx' -File)
if ($trxFiles.Count -eq 0) { throw 'No TRX results found; absence is never a pass.' }
$results = @()
foreach ($file in $trxFiles) {
    [xml]$trx = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($result in $trx.TestRun.Results.UnitTestResult) {
        $results += [pscustomobject]@{name=[string]$result.testName; outcome=[string]$result.outcome; evidence=$file.Name}
    }
}
if ($results.Count -eq 0) { throw 'TRX contains no test results.' }

$coverage = foreach ($case in $catalog.scenarios) {
    $evidence = @()
    $missing = @()
    foreach ($test in $case.automatedTests) {
        $matched = @($results | Where-Object { $_.name -match ('(^|\.)' + [regex]::Escape($test) + '(\(|$)') })
        if ($matched.Count -eq 0) { $missing += $test } else { $evidence += $matched }
    }
    $state = 'Unmapped'
    if ($case.automatedTests.Count -gt 0) {
        if (@($evidence | Where-Object outcome -eq 'Failed').Count -gt 0) { $state = 'Failed' }
        elseif ($missing.Count -gt 0) { $state = 'NotRun' }
        elseif (@($evidence | Where-Object outcome -ne 'Passed').Count -gt 0) { $state = 'Incomplete' }
        else { $state = 'PartialPass' }
    }
    [pscustomobject]@{id=$case.id; category=$case.category; severity=$case.severity;
        automatedEvidence=$state; scenarioAcceptance='NotRun'; missingTests=$missing; evidence=$evidence}
}
$report = [ordered]@{
    suiteVersion=$catalog.suiteVersion
    generatedAt=[DateTimeOffset]::UtcNow.ToString('O')
    evidenceDirectory=$resultRoot
    passed=@($results | Where-Object outcome -eq 'Passed').Count
    failed=@($results | Where-Object outcome -eq 'Failed').Count
    other=@($results | Where-Object { $_.outcome -notin @('Passed','Failed') }).Count
    failedTests=@($results | Where-Object outcome -eq 'Failed')
    releaseAcceptance='NotEvaluated'
    note='TRX proves only mapped assertions. Record build, source hashes, manual/model review and blockers separately. Never mix runs in this directory.'
    scenarios=@($coverage)
}
$output = Join-Path $resultRoot 'fixed-eval-coverage.json'
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $output -Encoding UTF8
Write-Output "Passed=$($report.passed); Failed=$($report.failed); Other=$($report.other). Evidence: $output"
if ($report.failed -gt 0) { exit 1 }
if (@($coverage | Where-Object { $_.automatedEvidence -in @('NotRun','Incomplete') }).Count -gt 0) { exit 2 }
exit 0
