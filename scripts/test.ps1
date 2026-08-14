$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# The CLI emits UTF-8 (Console.OutputEncoding = UTF8 in Program.cs). Decode the
# child process's stdout as UTF-8 too, or Chinese text is mangled by the system
# OEM codepage and --help/--format json assertions fail on non-UTF-8 locales.
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

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

# Config get/set/unset guards (exit codes, no destructive writes)
Test-Case "config get output_format" @("config", "get", "output_format") -Assert {
    param($out)
    if ([string]::IsNullOrWhiteSpace("$out")) {
        throw "config get output_format produced no output"
    }
}
Test-Case "config get unknown_key" @("config", "get", "nope") -ExpectedExitCode 2
Test-Case "config set unknown_key value" @("config", "set", "nope", "value") -ExpectedExitCode 2
Test-Case "config set timeout_seconds not-a-number" @("config", "set", "timeout_seconds", "not-a-number") -ExpectedExitCode 2
Test-Case "config set output_format bogus" @("config", "set", "output_format", "bogus") -ExpectedExitCode 2
Test-Case "config unset unknown_key" @("config", "unset", "nope") -ExpectedExitCode 2

# Fetch option guards
Test-Case "--format with --raw conflict" @("--format", "json", "--raw", "text") -ExpectedExitCode 2
Test-Case "--no-config --format json" @("--no-config", "--format", "json") -Assert {
    param($out)
    $text = "$out"
    try {
        $null = $text | ConvertFrom-Json
    }
    catch {
        throw "--no-config --format json output is not valid JSON: $text"
    }
}

# Summary
Write-Host ""
Write-Host "Results: $passed passed, $failed failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })

if ($failed -gt 0) {
    exit 1
}
