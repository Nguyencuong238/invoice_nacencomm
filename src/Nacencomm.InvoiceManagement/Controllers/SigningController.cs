using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nacencomm.InvoiceManagement.Models;
using Nacencomm.InvoiceManagement.Services;

namespace Nacencomm.InvoiceManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SigningController : ControllerBase
    {
        private readonly ISigningService _signingService;
        private readonly IInvoiceService _invoiceService;

        public SigningController(ISigningService signingService, IInvoiceService invoiceService)
        {
            _signingService = signingService;
            _invoiceService = invoiceService;
        }

        [HttpPost("create-job")]
        public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
        {
            try
            {
                var job = await _signingService.CreateJobAsync(request);
                return Ok(job);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo job ký: " + ex.Message });
            }
        }

        [HttpGet("job/{jobId}")]
        public async Task<IActionResult> GetJobStatus(Guid jobId)
        {
            var job = await _signingService.GetJobAsync(jobId);
            if (job == null) return NotFound(new { message = "Không tìm thấy thông tin job ký số." });
            return Ok(job);
        }

        [HttpPost("job/{jobId}/update-item")]
        public async Task<IActionResult> UpdateItemStatus(Guid jobId, [FromBody] SigningResult result)
        {
            if (result == null) return BadRequest();

            string statusStr = result.Success ? "Success" : "Failed";
            await _signingService.UpdateItemStatusAsync(jobId, result.InvoiceId, statusStr, result.ErrorCode, result.ErrorMessage);

            if (result.Success && !string.IsNullOrEmpty(result.SignedXml))
            {
                await _invoiceService.UpdateSignedXmlAsync(result.InvoiceId, result.SignedXml, "CN=CÔNG TY CỔ PHẦN CÔNG NGHỆ THẺ NACENCOMM1");
            }
            else
            {
                await _invoiceService.UpdateInvoiceStatusAsync(result.InvoiceId, "LỖI KÝ", result.ErrorMessage ?? "Ký số không thành công.");
            }

            return Ok(new { success = true });
        }
    }
}
