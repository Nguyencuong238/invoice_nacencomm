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
        // Check for Single-File or Published EXE first
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

        // Fallback mock binary content if file is not found
        var mockContent = System.Text.Encoding.UTF8.GetBytes("Nacencomm WinForms Signer v1.0.0 Executable Package");
        return File(mockContent, "application/octet-stream", "Nacencomm.WinFormsSigner.exe");
    }

    [HttpGet("Home/DownloadSignerZip")]
    public IActionResult DownloadSignerZip()
    {
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

        return RedirectToAction("DownloadSignerApp");
    }

    [HttpGet("Home/DownloadSignerClickOnce")]
    public IActionResult DownloadSignerClickOnce()
    {
        var appPath = Path.Combine(Directory.GetCurrentDirectory(), "publish", "clickonce", "Nacencomm.WinFormsSigner.application");
        if (System.IO.File.Exists(appPath))
        {
            var bytes = System.IO.File.ReadAllBytes(appPath);
            return File(bytes, "application/x-ms-application", "Nacencomm.WinFormsSigner.application");
        }

        return RedirectToAction("DownloadSignerApp");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
