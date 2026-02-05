Write-Host "=== DEBUG STREAM LAN ===" -ForegroundColor Cyan
Write-Host ""

# Check build files
Write-Host "1. Kiểm tra file build:" -ForegroundColor Yellow
$serverExe = ".\N12_StreamLAN\bin\Debug\net10.0-windows\Server_StreamLAN.exe"
$clientExe = ".\Client_StreamLAN\bin\Debug\net10.0-windows\Client_StreamLAN.exe"

if (Test-Path $serverExe) {
    Write-Host "   ✅ Server.exe tồn tại" -ForegroundColor Green
}
else {
    Write-Host "   ❌ Server.exe KHÔNG tồn tại - cần build lại" -ForegroundColor Red
}

if (Test-Path $clientExe) {
    Write-Host "   ✅ Client.exe tồn tại" -ForegroundColor Green
}
else {
    Write-Host "   ❌ Client.exe KHÔNG tồn tại - cần build lại" -ForegroundColor Red
}

Write-Host ""

# Check port UDP 9000
Write-Host "2. Kiểm tra UDP port 9000:" -ForegroundColor Yellow
$port = Get-NetUDPEndpoint -LocalPort 9000 -ErrorAction SilentlyContinue
if ($port) {
    Write-Host "   ⚠️  Port 9000 đang được sử dụng bởi:" -ForegroundColor Yellow
    $port | Format-Table LocalAddress, LocalPort, OwningProcess
}
else {
    Write-Host "   ✅ Port 9000 trống (OK)" -ForegroundColor Green
}

Write-Host ""

# Check camera
Write-Host "3. Kiểm tra Camera:" -ForegroundColor Yellow
$cameras = Get-PnpDevice -Class Camera -Status OK -ErrorAction SilentlyContinue
if ($cameras) {
    Write-Host "   ✅ Phát hiện camera:" -ForegroundColor Green
    $cameras | ForEach-Object { Write-Host "      - $($_.FriendlyName)" -ForegroundColor White }
}
else {
    Write-Host "   ❌ KHÔNG phát hiện camera" -ForegroundColor Red
}

Write-Host ""
Write-Host "=======================" -ForegroundColor Cyan
