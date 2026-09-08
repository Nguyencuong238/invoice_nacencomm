using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nacencomm.InvoiceManagement.Models;
using Nacencomm.InvoiceManagement.Services;

namespace Nacencomm.InvoiceManagement.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(InvoiceFilter filter)
        {
            var invoices = await _invoiceService.GetInvoicesAsync(filter);
            ViewBag.Filter = filter;
            return View(invoices);
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoicesPartial([FromQuery] InvoiceFilter filter)
        {
            var invoices = await _invoiceService.GetInvoicesAsync(filter);
            ViewBag.Filter = filter;
            return PartialView("_InvoiceTablePartial", invoices);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel([FromQuery] InvoiceFilter filter)
        {
            var content = await _invoiceService.ExportToExcelAsync(filter);
            var fileName = $"Danh_sach_hoa_don_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> GetXml(long id)
        {
            try
            {
                var xml = await _invoiceService.GetXmlAsync(id);
                
                // Security Hardening (Phase 06): Validate & Sanitize XML against XXE / Malformed XML
                if (!XmlSanitizer.ValidateAndSanitizeXml(xml, out var sanitizedXml, out var error))
                {
                    return BadRequest(new { error = SigningErrorCodes.INVALID_XML, message = error });
                }

                return Content(sanitizedXml, "application/xml");
            }
            catch (Exception ex)
            {
                return NotFound(new { error = SigningErrorCodes.BUSINESS_ERROR, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitSignedXml([FromBody] SigningResult result)
        {
            if (result == null || result.InvoiceId <= 0)
            {
                return BadRequest(new { success = false, errorCode = SigningErrorCodes.INVALID_XML, message = "Thông tin kết quả ký không hợp lệ." });
            }

            if (result.Success && !string.IsNullOrEmpty(result.SignedXml))
            {
                // Validate signed XML structure
                if (!XmlSanitizer.ValidateAndSanitizeXml(result.SignedXml, out var sanitizedSignedXml, out var error))
                {
                    await _invoiceService.UpdateInvoiceStatusAsync(result.InvoiceId, "LỖI KÝ", error ?? "Signed XML không hợp lệ.");
                    return Json(new { success = false, errorCode = SigningErrorCodes.INVALID_XML, message = error });
                }

                var ok = await _invoiceService.UpdateSignedXmlAsync(result.InvoiceId, sanitizedSignedXml, "CN=CÔNG TY CỔ PHẦN CÔNG NGHỆ THẺ NACENCOMM1");
                return Json(new { success = ok });
            }
            else
            {
                var ok = await _invoiceService.UpdateInvoiceStatusAsync(result.InvoiceId, "LỖI KÝ", result.ErrorMessage ?? "Lỗi ký số tại WinForms client.");
                return Json(new { success = ok, errorCode = result.ErrorCode ?? SigningErrorCodes.SIGN_FAILED, message = result.ErrorMessage });
            }
        }
    }
}
