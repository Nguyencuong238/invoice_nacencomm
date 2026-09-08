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
        var binPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Nacencomm.WinFormsSigner", "bin", "Debug", "net10.0", "Nacencomm.WinFormsSigner.exe");
        if (System.IO.File.Exists(binPath))
        {
            var bytes = System.IO.File.ReadAllBytes(binPath);
            return File(bytes, "application/vnd.microsoft.portable-executable", "Nacencomm.WinFormsSigner.exe");
        }

        var appHostPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Nacencomm.WinFormsSigner", "bin", "Debug", "net10.0", "apphost.exe");
        if (System.IO.File.Exists(appHostPath))
        {
            var bytes = System.IO.File.ReadAllBytes(appHostPath);
            return File(bytes, "application/vnd.microsoft.portable-executable", "Nacencomm.WinFormsSigner.exe");
        }

        // Fallback mock binary content if file is not found
        var mockContent = System.Text.Encoding.UTF8.GetBytes("Nacencomm WinForms Signer v1.0.0 Executable Package");
        return File(mockContent, "application/octet-stream", "Nacencomm.WinFormsSigner.exe");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
