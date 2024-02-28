param (
    [string]$HollowKnightPath
)

$process = Get-Process "Hollow Knight" -ErrorAction SilentlyContinue
if ($process) {
    taskkill /IM "Hollow Knight.exe"
    Start-Sleep -Seconds 3
}
& "$HollowKnightPath\Hollow Knight.exe"
