using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Nacencomm.InvoiceManagement.Models;

namespace Nacencomm.InvoiceManagement.Controllers;

public class HomeController : Controller
{
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
        // 1. Check for published ZIP package containing full files (.application, .manifest, .config, .exe, app.publish)
        var zipPath = Path.Combine(Directory.GetCurrentDirectory(), "publish", "Nacencomm.WinFormsSigner.zip");
        if (System.IO.File.Exists(zipPath))
        {
            var bytes = System.IO.File.ReadAllBytes(zipPath);
            return File(bytes, "application/zip", "Nacencomm.WinFormsSigner.zip");
        }

        var wwwrootZip = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "downloads", "signer", "Nacencomm.WinFormsSigner.zip");
        if (System.IO.File.Exists(wwwrootZip))
        {
            var bytes = System.IO.File.ReadAllBytes(wwwrootZip);
            return File(bytes, "application/zip", "Nacencomm.WinFormsSigner.zip");
        }

        // 2. Fallback to single EXE if zip not found
        var publishExe = Path.Combine(Directory.GetCurrentDirectory(), "publish", "Nacencomm.WinFormsSigner.exe");
        if (System.IO.File.Exists(publishExe))
        {
            var bytes = System.IO.File.ReadAllBytes(publishExe);
            return File(bytes, "application/vnd.microsoft.portable-executable", "Nacencomm.WinFormsSigner.exe");
        }

        var binPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Nacencomm.WinFormsSigner", "bin", "Debug", "net10.0", "Nacencomm.WinFormsSigner.exe");
        if (System.IO.File.Exists(binPath))
        {
            var bytes = System.IO.File.ReadAllBytes(binPath);
            return File(bytes, "application/vnd.microsoft.portable-executable", "Nacencomm.WinFormsSigner.exe");
        }

        // Fallback content if file is not found
        var mockContent = System.Text.Encoding.UTF8.GetBytes("Nacencomm WinForms Signer Package");
        return File(mockContent, "application/zip", "Nacencomm.WinFormsSigner.zip");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
