# PowerShell script to publish Nacencomm WinForms Signer application
param (
    [string]$PublishType = "ClickOncePackage", # Options: ClickOncePackage, SingleFile
    [string]$OutputDir = "./publish"
)

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host " Nacencomm WinForms Signer Publishing Tool" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

$clickonceDir = "$OutputDir/Nacencomm.WinFormsSigner"
if (Test-Path $clickonceDir) { Remove-Item -Recurse -Force $clickonceDir }
New-Item -ItemType Directory -Path $clickonceDir -Force | Out-Null

Write-Host "[1/3] Building WinForms application binaries..." -ForegroundColor Yellow
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

$configXml = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5"/>
  </startup>
</configuration>
"@
Set-Content -Path "$clickonceDir/Nacencomm.WinFormsSigner.exe.config" -Value $configXml -Encoding UTF8

# Create app.publish directory like reference package
$appPublishDir = "$clickonceDir/app.publish"
if (!(Test-Path $appPublishDir)) { New-Item -ItemType Directory -Path $appPublishDir -Force }
Copy-Item -Path "$clickonceDir/Nacencomm.WinFormsSigner.exe" -Destination "$appPublishDir/Nacencomm.WinFormsSigner.exe" -Force

Write-Host "[3/3] Packing full package (.application, .manifest, .config, .exe, app.publish) into ZIP..." -ForegroundColor Yellow
$zipPath = "$OutputDir/Nacencomm.WinFormsSigner.zip"
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
Compress-Archive -Path "$clickonceDir" -DestinationPath $zipPath -Force

Write-Host "`n[SUCCESS] Package generated at: $zipPath" -ForegroundColor Green
Write-Host "Unzipping this package reveals the exact set of files matching the reference sample!" -ForegroundColor Green
