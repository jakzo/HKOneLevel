param (
    [string]$HollowKnightPath
)

$process = Get-Process "Hollow Knight" -ErrorAction SilentlyContinue
if ($process) {
    taskkill /IM "Hollow Knight.exe" /F
}
Start-Sleep -Seconds 1
& "$HollowKnightPath\Hollow Knight.exe"
