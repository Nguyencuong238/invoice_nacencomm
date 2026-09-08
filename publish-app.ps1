# PowerShell script to publish Nacencomm WinForms Signer application
param (
    [string]$PublishType = "SingleFile", # Options: SingleFile, Zip
    [string]$OutputDir = "./publish"
)

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host " Nacencomm WinForms Signer Publishing Tool" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

if ($PublishType -eq "SingleFile") {
    Write-Host "[1/2] Building Single-File Standalone Executable..." -ForegroundColor Yellow
    dotnet publish src/Nacencomm.WinFormsSigner/Nacencomm.WinFormsSigner.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $OutputDir

    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n[SUCCESS] Single-File EXE generated at: $OutputDir/Nacencomm.WinFormsSigner.exe" -ForegroundColor Green
        Write-Host "Ready to upload to Web Server for 1-click download!" -ForegroundColor Green
    } else {
        Write-Host "`n[ERROR] Build failed. Please check logs above." -ForegroundColor Red
    }
} elseif ($PublishType -eq "Zip") {
    $tempDir = "./publish_temp"
    Write-Host "[1/3] Building application files..." -ForegroundColor Yellow
    dotnet publish src/Nacencomm.WinFormsSigner/Nacencomm.WinFormsSigner.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $tempDir

    Write-Host "[2/3] Adding auto-launch script (MoAppKeySo.bat)..." -ForegroundColor Yellow
    $batContent = @"
@echo off
echo Running Nacencomm WinForms Signer...
powershell -Command "Unblock-File -Path '%~dp0Nacencomm.WinFormsSigner.exe'"
start "" "%~dp0Nacencomm.WinFormsSigner.exe"
"@
    Set-Content -Path "$tempDir/MoAppKeySo.bat" -Value $batContent -Encoding UTF8

    Write-Host "[3/3] Compressing into ZIP package..." -ForegroundColor Yellow
    if (!(Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir }
    Compress-Archive -Path "$tempDir/*" -DestinationPath "$OutputDir/Nacencomm.WinFormsSigner.zip" -Force
    Remove-Item -Recurse -Force $tempDir

    Write-Host "`n[SUCCESS] ZIP package generated at: $OutputDir/Nacencomm.WinFormsSigner.zip" -ForegroundColor Green
    Write-Host "Customers can extract and run MoAppKeySo.bat without Windows Defender blocks!" -ForegroundColor Green
}
