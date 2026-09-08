using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nacencomm.InvoiceManagement.Models;

namespace Nacencomm.InvoiceManagement.Services
{
    public class SigningService : ISigningService
    {
        private static readonly ConcurrentDictionary<Guid, SigningJob> _jobs = new();
        private readonly IInvoiceService _invoiceService;

        public SigningService(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        public async Task<SigningJob> CreateJobAsync(CreateJobRequest request)
        {
            if (request.InvoiceIds == null || !request.InvoiceIds.Any())
            {
                throw new ArgumentException("Danh sách hóa đơn chọn để ký không được rỗng.");
            }

            // Max 50 items constraint enforcement
            if (request.InvoiceIds.Count > 50)
            {
                throw new ArgumentException("Một lượt ký chỉ được chọn tối đa 50 hóa đơn.");
            }

            var items = new List<SigningJobItem>();

            // Fetch invoices to validate, sort Date ASC, then InvoiceNumber ASC
            var invoices = new List<Invoice>();
            foreach (var id in request.InvoiceIds)
            {
                var inv = await _invoiceService.GetByIdAsync(id);
                if (inv != null)
                {
                    invoices.Add(inv);
                }
            }

            // MANDATORY sort requirement from Phase 1 & 4: Order by InvoiceDate ASC, then InvoiceNumber ASC
            var sortedInvoices = invoices
                .OrderBy(x => x.InvoiceDate)
                .ThenBy(x => x.InvoiceNumber)
                .ToList();

            foreach (var inv in sortedInvoices)
            {
                items.Add(new SigningJobItem
                {
                    InvoiceId = inv.Id,
                    InvoiceNumber = inv.InvoiceNumber,
                    InvoiceDate = inv.InvoiceDate,
                    Status = "Pending"
                });
            }

            var job = new SigningJob
            {
                JobId = Guid.NewGuid(),
                Total = items.Count,
                Completed = 0,
                Success = 0,
                Failed = 0,
                Status = "Processing",
                CreatedAt = DateTime.Now,
                Items = items
            };

            _jobs[job.JobId] = job;
            return job;
        }

        public Task<SigningJob?> GetJobAsync(Guid jobId)
        {
            _jobs.TryGetValue(jobId, out var job);
            return Task.FromResult(job);
        }

        public Task UpdateItemStatusAsync(Guid jobId, long invoiceId, string status, string? errorCode = null, string? errorMessage = null)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                var item = job.Items.FirstOrDefault(x => x.InvoiceId == invoiceId);
                if (item != null)
                {
                    var oldStatus = item.Status;
                    item.Status = status;
                    item.ErrorCode = errorCode;
                    item.ErrorMessage = errorMessage;

                    if (status == "Success")
                    {
                        item.CompletedAt = DateTime.Now;
                        if (oldStatus != "Success")
                        {
                            job.Success++;
                            job.Completed++;
                        }
                    }
                    else if (status == "Failed")
                    {
                        item.CompletedAt = DateTime.Now;
                        if (oldStatus != "Failed")
                        {
                            job.Failed++;
                            job.Completed++;
                        }
                    }

                    if (job.Completed >= job.Total)
                    {
                        job.Status = "Completed";
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
