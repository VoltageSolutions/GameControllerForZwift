# Validates a publish output folder. The release zips are intentionally flattened,
# so the publish output must contain no subfolders or files would be lost.
param(
    [Parameter(Mandatory)] [string] $Path,
    [switch] $RequireSdl2
)
$ErrorActionPreference = 'Stop'
$dirs = @(Get-ChildItem -Path $Path -Directory)
if ($dirs.Count -gt 0) {
    Write-Error "Publish output '$Path' contains subfolders ($($dirs.Name -join ', ')). Release zips are flattened, so this would lose files."
    exit 1
}
if ($RequireSdl2 -and -not (Test-Path (Join-Path $Path 'SDL2.dll'))) {
    Write-Error "SDL2.dll is missing from '$Path'."
    exit 1
}
if (-not (Test-Path (Join-Path $Path 'GameControllerForZwift.exe'))) {
    Write-Error "GameControllerForZwift.exe is missing from '$Path'."
    exit 1
}
Write-Host "Publish output '$Path' OK: $((Get-ChildItem -Path $Path -File).Count) files, no subfolders."
