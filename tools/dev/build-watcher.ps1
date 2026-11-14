# Remote build watcher. Run from the repo root:
#   powershell -ExecutionPolicy Bypass -File tools\dev\build-watcher.ps1
# Polls every 2 s for .build-request and runs only dotnet build/test/clean commands.
# Compatible with Windows PowerShell 5.1 and pwsh.
$ErrorActionPreference = 'Continue'
$root = (Get-Location).Path
$request = Join-Path $root '.build-request'
$running = Join-Path $root '.build-running'
$done = Join-Path $root '.build-done'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Get-Steps([string[]]$lines) {
    $steps = @()
    $n = 0
    foreach ($raw in $lines) {
        $line = $raw.Trim()
        if ($line -eq '') { continue }
        $n++
        switch -Regex ($line) {
            '^build$'        { $steps += ,@('build', 'dotnet build GameControllerForZwift.sln -v:minimal', 'build.log'); break }
            '^test-network$' { $steps += ,@('test-network', 'dotnet test tests\GameControllerForZwift.Network.UnitTests', 'test-network.log'); break }
            '^test-unit$'    { $steps += ,@('test-unit', 'dotnet test tests\GameControllerForZwift.UnitTests', 'test-unit.log'); break }
            '^test$'         { $steps += ,@('test-network', 'dotnet test tests\GameControllerForZwift.Network.UnitTests', 'test-network.log')
                               $steps += ,@('test-unit', 'dotnet test tests\GameControllerForZwift.UnitTests', 'test-unit.log'); break }
            '^clean$'        { $steps += ,@('clean', 'dotnet clean GameControllerForZwift.sln -v:minimal', 'clean.log'); break }
            '^dotnet\s'      { $steps += ,@("custom$n", $line, "step-$n.log"); break }
            default          { $steps += ,@("rejected$n", $null, $line) }
        }
    }
    return ,$steps
}

function Invoke-Step([string]$cmdLine, [string]$logPath) {
    # cmd.exe merges stderr into stdout in order; output is written as UTF-8 without BOM.
    $tmp = [IO.Path]::GetTempFileName()
    try {
        & cmd.exe /d /c "$cmdLine > `"$tmp`" 2>&1"
        $code = $LASTEXITCODE
        $text = [IO.File]::ReadAllText($tmp)
        [IO.File]::WriteAllText($logPath, $text, $utf8NoBom)
    } finally { Remove-Item -LiteralPath $tmp -ErrorAction SilentlyContinue }
    return $code
}

Write-Host "build-watcher: watching $root for .build-request (Ctrl+C to stop)"
while ($true) {
    if (Test-Path -LiteralPath $request) {
        $started = Get-Date
        try {
            Move-Item -LiteralPath $request -Destination $running -Force
        } catch { Start-Sleep -Seconds 2; continue }
        $content = @()
        try { $content = @(Get-Content -LiteralPath $running -ErrorAction Stop) } catch { }
        $steps = Get-Steps $content
        if ($steps.Count -eq 0) { $steps = Get-Steps @('build', 'test-network', 'test-unit') }

        $report = New-Object System.Collections.Generic.List[string]
        $report.Add("started: " + $started.ToString('o'))
        $locking = @(Get-Process -Name 'ZwiftLanProbe', 'GameControllerForZwift' -ErrorAction SilentlyContinue)
        if ($locking.Count -gt 0) {
            $report.Add("note: running and may lock bin files: " + (($locking | ForEach-Object { $_.ProcessName + ' (pid ' + $_.Id + ')' }) -join ', '))
        }
        $summary = @()
        foreach ($s in $steps) {
            $name = $s[0]
            if ($null -eq $s[1]) {
                $report.Add("${name}: REJECTED (only build/test/test-network/test-unit/clean or lines starting with 'dotnet ' are allowed): " + $s[2])
                $summary += "$name=rejected"
                continue
            }
            $sw = [Diagnostics.Stopwatch]::StartNew()
            $code = Invoke-Step $s[1] (Join-Path $root $s[2])
            $sw.Stop()
            $report.Add(("{0}: exit {1}, {2:N1} s, log {3} ({4})" -f $name, $code, $sw.Elapsed.TotalSeconds, $s[2], $s[1]))
            $summary += "$name=$code"
            if ($name -eq 'build' -and $code -ne 0) {
                $report.Add('stopped: build failed, remaining steps skipped')
                break
            }
        }
        $report.Add("finished: " + (Get-Date).ToString('o'))
        [IO.File]::WriteAllText($done, (($report -join "`r`n") + "`r`n"), $utf8NoBom)
        Remove-Item -LiteralPath $running -ErrorAction SilentlyContinue
        Write-Host ("{0} build-watcher: {1} ({2:N0} s)" -f (Get-Date -Format 'HH:mm:ss'), ($summary -join ' '), ((Get-Date) - $started).TotalSeconds)
    }
    Start-Sleep -Seconds 2
}
