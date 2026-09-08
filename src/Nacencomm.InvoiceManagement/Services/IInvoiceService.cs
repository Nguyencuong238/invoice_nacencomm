using System.Collections.Generic;
using System.Threading.Tasks;
using Nacencomm.InvoiceManagement.Models;

namespace Nacencomm.InvoiceManagement.Services
{
    public interface IInvoiceService
    {
        Task<IReadOnlyList<Invoice>> GetInvoicesAsync(InvoiceFilter filter);
        Task<Invoice?> GetByIdAsync(long id);
        Task<string> GetXmlAsync(long invoiceId);
        Task<bool> UpdateSignedXmlAsync(long invoiceId, string signedXml, string? signerSubject);
        Task<bool> UpdateInvoiceStatusAsync(long invoiceId, string status, string? errorMessage);
        Task<byte[]> ExportToExcelAsync(InvoiceFilter filter);
    }
}
