# Publish both Server and Client to Desktop\PublishAppWPF
# Run from repo root: .\Publish-Both.ps1

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$outRoot = [Environment]::GetFolderPath("Desktop") + "\PublishAppWPF"

Write-Host "Publishing to: $outRoot" -ForegroundColor Cyan

Write-Host "`n[1/2] Publishing Server..." -ForegroundColor Yellow
Push-Location (Join-Path $root "N12_StreamLAN")
dotnet publish -c Release -o (Join-Path $outRoot "Server")
Pop-Location

Write-Host "`n[2/2] Publishing Client..." -ForegroundColor Yellow
Push-Location (Join-Path $root "Client_StreamLAN")
dotnet publish -c Release -o (Join-Path $outRoot "Client")
Pop-Location

Write-Host "`nDone. Output:" -ForegroundColor Green
Write-Host "  $outRoot\Server\  (StreamLAN Server)"
Write-Host "  $outRoot\Client\  (StreamLAN Client)"
