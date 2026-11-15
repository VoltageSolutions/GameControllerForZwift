# Starts GameControllerForZwift.exe, waits until the app log shows that SDL2 initialized and enumerated
# devices, checks that the process is still alive, then stops it and prints the log.
# Used by the "ARM smoke test" CI job to prove the x64 build runs under Windows 11 x64 emulation.
# Exit code 0 on success, 1 on failure (with ::error annotations).
param(
    [Parameter(Mandatory)] [string] $ExePath,
    [int] $TimeoutSeconds = 60,
    # Seconds the app must stay alive after the log lines appeared.
    [int] $StableSeconds = 5,
    [string] $Label = ''
)
$ErrorActionPreference = 'Stop'
if (-not $Label) { $Label = Split-Path (Split-Path $ExePath -Parent) -Leaf }

# Lines the app writes on startup (SDL2InputService). Both the init line and one enumeration line are required.
$initPattern = 'SDL2 initialized successfully\.'
$enumPatterns = @('No game controllers found\.', 'SDL2 reports \d+ joystick device\(s\)\.')

function Get-PeMachine([string] $Path) {
    $fs = [System.IO.File]::OpenRead($Path)
    try {
        $br = [System.IO.BinaryReader]::new($fs)
        $fs.Position = 0x3C
        $peOffset = $br.ReadInt32()
        $fs.Position = $peOffset + 4
        $machine = $br.ReadUInt16()
    } finally { $fs.Dispose() }
    switch ($machine) {
        0x8664 { 'x64 (AMD64)' }
        0xAA64 { 'ARM64' }
        0x014C { 'x86' }
        default { '0x{0:X4}' -f $machine }
    }
}

function Find-NewestLog([string[]] $Dirs, [datetime] $Since) {
    foreach ($d in $Dirs) {
        if (-not (Test-Path $d)) { continue }
        $f = Get-ChildItem -Path $d -Filter 'app-*.log' -File -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTime -ge $Since } |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($f) { return $f }
    }
    return $null
}

function Read-LogText([string] $Path) {
    # The app keeps the file open with FileShare.ReadWrite, so open it the same way.
    $fs = [System.IO.File]::Open($Path, 'Open', 'Read', 'ReadWrite')
    try { [System.IO.StreamReader]::new($fs).ReadToEnd() } finally { $fs.Dispose() }
}

$ExePath = (Resolve-Path $ExePath).Path
$exeDir = Split-Path $ExePath -Parent
Write-Host "::group::Smoke test: $Label"
Write-Host "OS architecture (PROCESSOR_ARCHITECTURE): $env:PROCESSOR_ARCHITECTURE"
Write-Host "OS architecture (RuntimeInformation):      $([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture)"
Write-Host "Executable:                                $ExePath"
Write-Host "Executable PE machine type:                $(Get-PeMachine $ExePath)"
Write-Host "DOTNET_ROOT_X64 / DOTNET_ROOT:             '$env:DOTNET_ROOT_X64' / '$env:DOTNET_ROOT'"

# Logs go to <exe dir>\logs. A single-file build may report its extraction folder instead, so look there as a fallback.
$logDirs = @(
    (Join-Path $exeDir 'logs'),
    (Join-Path $env:TEMP '.net')
)
$started = Get-Date
$proc = Start-Process -FilePath $ExePath -WorkingDirectory $exeDir -PassThru
Write-Host "Started PID $($proc.Id)"

$failure = $null
$log = $null
$text = ''
try {
    $deadline = $started.AddSeconds($TimeoutSeconds)
    $ok = $false
    while ((Get-Date) -lt $deadline) {
        if ($proc.HasExited) {
            $failure = "The app exited early with exit code $($proc.ExitCode)."
            break
        }
        if (-not $log) {
            $log = Find-NewestLog -Dirs @($logDirs[0]) -Since $started.AddSeconds(-2)
            if (-not $log -and (Test-Path $logDirs[1])) {
                $log = Get-ChildItem -Path $logDirs[1] -Recurse -Filter 'app-*.log' -File -ErrorAction SilentlyContinue |
                    Where-Object { $_.LastWriteTime -ge $started.AddSeconds(-2) } | Select-Object -First 1
            }
        }
        if ($log) {
            $text = Read-LogText $log.FullName
            if ($text -match $initPattern -and ($enumPatterns | Where-Object { $text -match $_ })) { $ok = $true; break }
        }
        Start-Sleep -Milliseconds 500
    }

    if (-not $failure) {
        if ($ok) {
            Start-Sleep -Seconds $StableSeconds
            $proc.Refresh()
            if ($proc.HasExited) { $failure = "The app started but exited $StableSeconds s later with exit code $($proc.ExitCode)." }
        } elseif (-not $log) {
            $failure = "No app-*.log appeared in '$($logDirs[0])' within $TimeoutSeconds s."
        } else {
            $failure = "Log '$($log.FullName)' did not show the expected lines within $TimeoutSeconds s (need '$initPattern' and one of: $($enumPatterns -join ' | '))."
        }
    }
} finally {
    if (-not $proc.HasExited) {
        [void]$proc.CloseMainWindow()
        if (-not $proc.WaitForExit(10000)) {
            Write-Host 'CloseMainWindow did not stop the app within 10 s, killing it.'
            $proc.Kill($true)
            [void]$proc.WaitForExit(10000)
        }
    }
}

if ($log) {
    Write-Host "--- $($log.FullName) ---"
    Write-Host (Read-LogText $log.FullName)
    Write-Host '--- end of log ---'
}
Write-Host '::endgroup::'

if ($failure) {
    Write-Host "::error title=ARM smoke test failed ($Label)::$failure"
    exit 1
}
Write-Host "Smoke test '$Label' passed: SDL2 initialized and devices were enumerated; the app stayed alive."
exit 0
