using System;
using System.Threading.Tasks;
using Nacencomm.InvoiceManagement.Models;

namespace Nacencomm.InvoiceManagement.Services
{
    public interface ISigningService
    {
        Task<SigningJob> CreateJobAsync(CreateJobRequest request);
        Task<SigningJob?> GetJobAsync(Guid jobId);
        Task UpdateItemStatusAsync(Guid jobId, long invoiceId, string status, string? errorCode = null, string? errorMessage = null);
    }
}
