$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$projectDir = Join-Path $PSScriptRoot ".." "hitokoto-cli"
$failed = 0
$passed = 0

Write-Host "::group::Building project"
Push-Location $projectDir
try {
    dotnet build --configuration Release | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Host "::error::Build failed"
        exit 1
    }
}
finally {
    Pop-Location
}
Write-Host "::endgroup::"

# Find the built binary
$binaryName = if ($IsWindows) { "hitokoto.exe" } else { "hitokoto" }

$frameworkDir = Get-ChildItem -Path (Join-Path $projectDir "bin" "Release") -Directory `
    | Where-Object { $_.Name -like "net*" } `
    | Select-Object -First 1

if (-not $frameworkDir) {
    Write-Host "::error::Build output directory not found (expected bin/Release/net*/)"
    exit 1
}

$binPath = Join-Path $frameworkDir.FullName $binaryName
if (-not (Test-Path $binPath)) {
    Write-Host "::error::Built binary not found at: $binPath"
    exit 1
}
Write-Host "Binary: $binPath"

function Test-Case {
    param(
        [string]$Name,
        [string[]]$CliArgs,
        [int]$ExpectedExitCode = 0,
        [scriptblock]$Assert = $null
    )

    Write-Host "  TEST: $Name" -NoNewline
    try {
        $output = & $binPath @CliArgs 2>&1
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne $ExpectedExitCode) {
            Write-Host "  FAIL (exit code $exitCode, expected $ExpectedExitCode)" -ForegroundColor Red
            Write-Host "  Output: $output"
            $script:failed++
            return
        }
        if ($Assert) {
            & $Assert $output
        }
        Write-Host "  PASS" -ForegroundColor Green
        $script:passed++
    }
    catch {
        Write-Host "  FAIL ($($_.Exception.Message))" -ForegroundColor Red
        $script:failed++
    }
}

Write-Host ""
Write-Host "Running tests..."
Write-Host ""

# Version
Test-Case "--version" @("--version") -Assert {
    param($out)
    if ("$out" -notmatch '\d+\.\d+\.\d+') {
        throw "Version output doesn't match semver pattern: $out"
    }
}
Test-Case "-v" @("-v") -Assert {
    param($out)
    if ("$out" -notmatch '\d+\.\d+\.\d+') {
        throw "Version output doesn't match semver pattern: $out"
    }
}

# Help
Test-Case "--help" @("--help") -Assert {
    param($out)
    $text = "$out"
    if ($text -notmatch '用法') {
        throw "Help output missing expected content"
    }
}
Test-Case "-h" @("-h") -Assert {
    param($out)
    $text = "$out"
    if ($text -notmatch '用法') {
        throw "Help output missing expected content"
    }
}

# Config
Test-Case "config list" @("config", "list")
Test-Case "config path" @("config", "path") -Assert {
    param($out)
    if ("$out" -notmatch '\.json') {
        throw "Config path should point to a .json file: $out"
    }
}

# Default fetch
Test-Case "default (no args)" @() -Assert {
    param($out)
    if ([string]::IsNullOrWhiteSpace($out)) {
        throw "Default command produced no output"
    }
}

# Format options
Test-Case "--format text" @("--format", "text") -Assert {
    param($out)
    if ([string]::IsNullOrWhiteSpace($out)) {
        throw "--format text produced no output"
    }
}

Test-Case "--format json" @("--format", "json") -Assert {
    param($out)
    $text = "$out"
    try {
        $null = $text | ConvertFrom-Json
    }
    catch {
        throw "--format json output is not valid JSON: $text"
    }
}

Test-Case "--format full" @("--format", "full") -Assert {
    param($out)
    if ([string]::IsNullOrWhiteSpace($out)) {
        throw "--format full produced no output"
    }
}

# Summary
Write-Host ""
Write-Host "Results: $passed passed, $failed failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })

if ($failed -gt 0) {
    exit 1
}
