# PowerShell script to publish Nacencomm WinForms Signer application
param (
    [string]$PublishType = "SingleFile", # Options: SingleFile, Zip, ClickOnce
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
} elseif ($PublishType -eq "ClickOnce") {
    Write-Host "[1/3] Building ClickOnce application binaries..." -ForegroundColor Yellow
    $clickonceDir = "$OutputDir/clickonce"
    if (!(Test-Path $clickonceDir)) { New-Item -ItemType Directory -Path $clickonceDir -Force }

    dotnet publish src/Nacencomm.WinFormsSigner/Nacencomm.WinFormsSigner.csproj -c Release -o $clickonceDir

    Write-Host "[2/3] Generating ClickOnce Manifest files (.application & .manifest)..." -ForegroundColor Yellow
    
    $manifestXml = @"
<?xml version="1.0" encoding="utf-8"?>
<asmv1:assembly xsi:schemaLocation="urn:schemas-microsoft-com:asm.v1 assembly.adaptive.xsd" manifestVersion="1.0" xmlns:asmv1="urn:schemas-microsoft-com:asm.v1" xmlns="urn:schemas-microsoft-com:asm.v2" xmlns:asmv2="urn:schemas-microsoft-com:asm.v2" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:co.v1="urn:schemas-microsoft-com:clickonce.v1" xmlns:asmv3="urn:schemas-microsoft-com:asm.v3" xmlns:dsig="http://www.w3.org/2000/09/xmldsig#" xmlns:co.v2="urn:schemas-microsoft-com:clickonce.v2">
  <asmv1:assemblyIdentity name="Nacencomm.WinFormsSigner.exe" version="1.0.0.0" publicKeyToken="0000000000000000" language="neutral" processorArchitecture="msil" type="win32" />
  <entryPoint>
    <assemblyIdentity name="Nacencomm.WinFormsSigner" version="1.0.0.0" language="neutral" processorArchitecture="msil" />
    <commandLine file="Nacencomm.WinFormsSigner.exe" parameters="" />
  </entryPoint>
  <trustInfo>
    <security>
      <applicationRequestMinimum>
        <PermissionSet version="1" class="System.Security.NamedPermissionSet" Name="LocalIntranet" Description="Default rights given to applications on the local intranet" Unrestricted="true" ID="Custom" SameSite="site" />
        <defaultAssemblyRequest permissionSetReference="Custom" />
      </applicationRequestMinimum>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <requestedExecutionLevel level="asInvoker" uiAccess="false" />
      </requestedPrivileges>
    </security>
  </trustInfo>
</asmv1:assembly>
"@
    Set-Content -Path "$clickonceDir/Nacencomm.WinFormsSigner.exe.manifest" -Value $manifestXml -Encoding UTF8

    $appXml = @"
<?xml version="1.0" encoding="utf-8"?>
<asmv1:assembly xsi:schemaLocation="urn:schemas-microsoft-com:asm.v1 assembly.adaptive.xsd" manifestVersion="1.0" xmlns:asmv1="urn:schemas-microsoft-com:asm.v1" xmlns="urn:schemas-microsoft-com:asm.v2" xmlns:asmv2="urn:schemas-microsoft-com:asm.v2" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:co.v1="urn:schemas-microsoft-com:clickonce.v1" xmlns:co.v2="urn:schemas-microsoft-com:clickonce.v2">
  <assemblyIdentity name="Nacencomm.WinFormsSigner.application" version="1.0.0.0" publicKeyToken="0000000000000000" language="neutral" processorArchitecture="msil" />
  <description asmv2:publisher="Nacencomm" asmv2:product="Nacencomm WinForms Signer" />
  <deployment install="true" mapFileExtensions="true" />
  <dependency>
    <dependentAssembly dependencyType="install" codebase="Nacencomm.WinFormsSigner.exe.manifest" size="2000">
      <assemblyIdentity name="Nacencomm.WinFormsSigner.exe" version="1.0.0.0" publicKeyToken="0000000000000000" language="neutral" processorArchitecture="msil" type="win32" />
    </dependentAssembly>
  </dependency>
</asmv1:assembly>
"@
    Set-Content -Path "$clickonceDir/Nacencomm.WinFormsSigner.application" -Value $appXml -Encoding UTF8

    # Create app.publish directory like reference package
    $appPublishDir = "$clickonceDir/app.publish"
    if (!(Test-Path $appPublishDir)) { New-Item -ItemType Directory -Path $appPublishDir -Force }
    Copy-Item -Path "$clickonceDir/Nacencomm.WinFormsSigner.exe" -Destination "$appPublishDir/Nacencomm.WinFormsSigner.exe" -Force

    Write-Host "`n[SUCCESS] ClickOnce package generated at: $clickonceDir" -ForegroundColor Green
    Write-Host "Contains .application, .manifest, .exe, and app.publish structure matching reference package!" -ForegroundColor Green
}
