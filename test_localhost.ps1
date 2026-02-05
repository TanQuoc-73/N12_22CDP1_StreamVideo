# Script test Stream LAN trên localhost
# Chạy script này để test nhanh hệ thống

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Stream LAN Test - Localhost Mode" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check xem có build chưa
if (-not (Test-Path ".\N12_StreamLAN\bin\Debug\net10.0-windows\Server_StreamLAN.exe")) {
    Write-Host "⚠️  Chưa build Server. Đang build..." -ForegroundColor Yellow
    dotnet build ".\N12_StreamLAN\Server_StreamLAN.csproj"
}

if (-not (Test-Path ".\Client_StreamLAN\bin\Debug\net10.0-windows\Client_StreamLAN.exe")) {
    Write-Host "⚠️  Chưa build Client. Đang build..." -ForegroundColor Yellow
    dotnet build ".\Client_StreamLAN\Client_StreamLAN.csproj"
}

Write-Host ""
Write-Host "📋 HƯỚNG DẪN TEST:" -ForegroundColor Green
Write-Host "1. Server sẽ mở trong 3 giây" -ForegroundColor White
Write-Host "2. Client sẽ tự động mở sau đó" -ForegroundColor White
Write-Host "3. Nếu Client yêu cầu login:" -ForegroundColor White
Write-Host "   - Nhập email/password Supabase của bạn" -ForegroundColor Yellow
Write-Host "   - HOẶC tắt và xem test_guide.md để bypass login" -ForegroundColor Yellow
Write-Host ""

# Chạy Server
Write-Host "🚀 Đang khởi động Server (UDP port 9000)..." -ForegroundColor Cyan
Start-Process ".\N12_StreamLAN\bin\Debug\net10.0-windows\Server_StreamLAN.exe"

Start-Sleep -Seconds 3

# Chạy Client
Write-Host "🚀 Đang khởi động Client..." -ForegroundColor Cyan
Start-Process ".\Client_StreamLAN\bin\Debug\net10.0-windows\Client_StreamLAN.exe"

Write-Host ""
Write-Host "✅ Đã khởi động cả 2 ứng dụng!" -ForegroundColor Green
Write-Host ""
Write-Host "🔍 KIỂM TRA:" -ForegroundColor Yellow
Write-Host "- Server window: Phải hiển thị video từ webcam" -ForegroundColor White
Write-Host "- Client window: Phải hiển thị preview camera" -ForegroundColor White
Write-Host ""
Write-Host "❌ NẾU CÓ VẤN ĐỀ:" -ForegroundColor Red
Write-Host "- Xem file test_guide.md để troubleshoot" -ForegroundColor White
Write-Host "- Check Windows Firewall cho UDP port 9000" -ForegroundColor White
Write-Host "- Đảm bảo webcam không bị app khác sử dụng" -ForegroundColor White
Write-Host ""
