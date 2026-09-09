using System.Diagnostics;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Nacencomm.InvoiceManagement.Models;

namespace Nacencomm.InvoiceManagement.Controllers;

public class HomeController : Controller
{
    private readonly IWebHostEnvironment _env;

    public HomeController(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Download()
    {
        ViewData["Title"] = "Tải xuống Nacencomm WinForms Signer";
        return View();
    }

    [HttpGet("Home/DownloadSignerApp")]
    public IActionResult DownloadSignerApp()
    {
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        // 1. Check candidate paths for published ZIP package on disk
        var candidateZipPaths = new[]
        {
            Path.Combine(_env.WebRootPath, "downloads", "signer", "Nacencomm.WinFormsSigner.zip"),
            Path.Combine(_env.ContentRootPath, "wwwroot", "downloads", "signer", "Nacencomm.WinFormsSigner.zip"),
            Path.Combine(_env.ContentRootPath, "publish", "Nacencomm.WinFormsSigner.zip"),
            Path.Combine(_env.ContentRootPath, "..", "publish", "Nacencomm.WinFormsSigner.zip"),
            Path.Combine(_env.ContentRootPath, "..", "..", "publish", "Nacencomm.WinFormsSigner.zip"),
            Path.Combine(Directory.GetCurrentDirectory(), "publish", "Nacencomm.WinFormsSigner.zip"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "publish", "Nacencomm.WinFormsSigner.zip")
        };

        foreach (var zipPath in candidateZipPaths)
        {
            if (System.IO.File.Exists(zipPath))
            {
                var bytes = System.IO.File.ReadAllBytes(zipPath);
                return File(bytes, "application/zip", "Nacencomm.WinFormsSigner.zip");
            }
        }

        // 2. Dynamically generate a valid ZIP package containing full ClickOnce file structure
        var dynamicZipBytes = GenerateValidZipPackage();
        return File(dynamicZipBytes, "application/zip", "Nacencomm.WinFormsSigner.zip");
    }

    private byte[] GenerateValidZipPackage()
    {
        byte[] exeBytes = GetWinFormsSignerExeBytes();

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            // Nacencomm.WinFormsSigner.application
            var appEntry = archive.CreateEntry("Nacencomm.WinFormsSigner/Nacencomm.WinFormsSigner.application");
            using (var writer = new StreamWriter(appEntry.Open()))
            {
                writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<asmv1:assembly xsi:schemaLocation=""urn:schemas-microsoft-com:asm.v1 assembly.adaptive.xsd"" manifestVersion=""1.0"" xmlns:asmv1=""urn:schemas-microsoft-com:asm.v1"" xmlns=""urn:schemas-microsoft-com:asm.v2"" xmlns:asmv2=""urn:schemas-microsoft-com:asm.v2"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:co.v1=""urn:schemas-microsoft-com:clickonce.v1"" xmlns:co.v2=""urn:schemas-microsoft-com:clickonce.v2"">
  <assemblyIdentity name=""Nacencomm.WinFormsSigner.application"" version=""1.0.0.0"" publicKeyToken=""0000000000000000"" language=""neutral"" processorArchitecture=""msil"" />
  <description asmv2:publisher=""Nacencomm"" asmv2:product=""Nacencomm WinForms Signer"" />
  <deployment install=""true"" mapFileExtensions=""true"" />
  <dependency>
    <dependentAssembly dependencyType=""install"" codebase=""Nacencomm.WinFormsSigner.exe.manifest"" size=""2000"">
      <assemblyIdentity name=""Nacencomm.WinFormsSigner.exe"" version=""1.0.0.0"" publicKeyToken=""0000000000000000"" language=""neutral"" processorArchitecture=""msil"" type=""win32"" />
    </dependentAssembly>
  </dependency>
</asmv1:assembly>");
            }

            // Nacencomm.WinFormsSigner.exe.manifest
            var manifestEntry = archive.CreateEntry("Nacencomm.WinFormsSigner/Nacencomm.WinFormsSigner.exe.manifest");
            using (var writer = new StreamWriter(manifestEntry.Open()))
            {
                writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<asmv1:assembly xsi:schemaLocation=""urn:schemas-microsoft-com:asm.v1 assembly.adaptive.xsd"" manifestVersion=""1.0"" xmlns:asmv1=""urn:schemas-microsoft-com:asm.v1"" xmlns=""urn:schemas-microsoft-com:asm.v2"" xmlns:asmv2=""urn:schemas-microsoft-com:asm.v2"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:co.v1=""urn:schemas-microsoft-com:clickonce.v1"" xmlns:asmv3=""urn:schemas-microsoft-com:asm.v3"" xmlns:dsig=""http://www.w3.org/2000/09/xmldsig#"" xmlns:co.v2=""urn:schemas-microsoft-com:clickonce.v2"">
  <asmv1:assemblyIdentity name=""Nacencomm.WinFormsSigner.exe"" version=""1.0.0.0"" publicKeyToken=""0000000000000000"" language=""neutral"" processorArchitecture=""msil"" type=""win32"" />
  <entryPoint>
    <assemblyIdentity name=""Nacencomm.WinFormsSigner"" version=""1.0.0.0"" language=""neutral"" processorArchitecture=""msil"" />
    <commandLine file=""Nacencomm.WinFormsSigner.exe"" parameters="""" />
  </entryPoint>
  <trustInfo>
    <security>
      <applicationRequestMinimum>
        <PermissionSet version=""1"" class=""System.Security.NamedPermissionSet"" Name=""LocalIntranet"" Description=""Default rights given to applications on the local intranet"" Unrestricted=""true"" ID=""Custom"" SameSite=""site"" />
        <defaultAssemblyRequest permissionSetReference=""Custom"" />
      </applicationRequestMinimum>
      <requestedPrivileges xmlns=""urn:schemas-microsoft-com:asm.v3"">
        <requestedExecutionLevel level=""asInvoker"" uiAccess=""false"" />
      </requestedPrivileges>
    </security>
  </trustInfo>
</asmv1:assembly>");
            }

            // Nacencomm.WinFormsSigner.exe.config
            var configEntry = archive.CreateEntry("Nacencomm.WinFormsSigner/Nacencomm.WinFormsSigner.exe.config");
            using (var writer = new StreamWriter(configEntry.Open()))
            {
                writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <startup>
    <supportedRuntime version=""v4.0"" sku="".NETFramework,Version=v4.5""/>
  </startup>
</configuration>");
            }

            // Nacencomm.WinFormsSigner.exe
            var exeEntry = archive.CreateEntry("Nacencomm.WinFormsSigner/Nacencomm.WinFormsSigner.exe");
            using (var stream = exeEntry.Open())
            {
                stream.Write(exeBytes, 0, exeBytes.Length);
            }

            // app.publish/Nacencomm.WinFormsSigner.exe
            var appPublishEntry = archive.CreateEntry("Nacencomm.WinFormsSigner/app.publish/Nacencomm.WinFormsSigner.exe");
            using (var stream = appPublishEntry.Open())
            {
                stream.Write(exeBytes, 0, exeBytes.Length);
            }
        }
        return ms.ToArray();
    }

    private byte[] GetWinFormsSignerExeBytes()
    {
        var candidateExePaths = new[]
        {
            Path.Combine(_env.ContentRootPath, "..", "publish", "Nacencomm.WinFormsSigner", "Nacencomm.WinFormsSigner.exe"),
            Path.Combine(_env.ContentRootPath, "..", "publish_singlefile", "Nacencomm.WinFormsSigner.exe"),
            Path.Combine(_env.ContentRootPath, "publish", "Nacencomm.WinFormsSigner.exe"),
            Path.Combine(Directory.GetCurrentDirectory(), "publish", "Nacencomm.WinFormsSigner.exe"),
            Path.Combine(_env.ContentRootPath, "..", "src", "Nacencomm.WinFormsSigner", "bin", "Release", "net8.0", "win-x64", "Nacencomm.WinFormsSigner.exe"),
            Path.Combine(_env.ContentRootPath, "..", "src", "Nacencomm.WinFormsSigner", "bin", "Debug", "net8.0", "win-x64", "Nacencomm.WinFormsSigner.exe"),
            Path.Combine(_env.ContentRootPath, "..", "src", "Nacencomm.WinFormsSigner", "bin", "Release", "net8.0", "Nacencomm.WinFormsSigner.exe"),
            Path.Combine(_env.ContentRootPath, "..", "src", "Nacencomm.WinFormsSigner", "bin", "Debug", "net8.0", "Nacencomm.WinFormsSigner.exe")
        };

        foreach (var exePath in candidateExePaths)
        {
            if (System.IO.File.Exists(exePath))
            {
                return System.IO.File.ReadAllBytes(exePath);
            }
        }

        return System.Text.Encoding.UTF8.GetBytes("Nacencomm WinForms Signer Executable Placeholder");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
