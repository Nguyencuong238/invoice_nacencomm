using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nacencomm.InvoiceManagement.Models;
using Nacencomm.InvoiceManagement.Services;
using Xunit;

namespace Nacencomm.InvoiceManagement.Tests
{
    public class InvoiceSigningTests
    {
        private readonly InvoiceService _invoiceService;
        private readonly SigningService _signingService;

        public InvoiceSigningTests()
        {
            _invoiceService = new InvoiceService();
            _signingService = new SigningService(_invoiceService);
        }

        [Fact]
        public async Task Test_InvoiceSorting_InvoiceDateAscending()
        {
            // Arrange: Filter all invoices
            var filter = new InvoiceFilter { TabStatus = "ISSUED", PageSize = 100 };
            
            // Act
            var invoices = await _invoiceService.GetInvoicesAsync(filter);

            // Assert: Must be sorted by InvoiceDate ASC, then InvoiceNumber ASC
            for (int i = 0; i < invoices.Count - 1; i++)
            {
                var current = invoices[i];
                var next = invoices[i + 1];

                Assert.True(current.InvoiceDate <= next.InvoiceDate, 
                    $"InvoiceDate out of order: #{current.Id} ({current.InvoiceDate:yyyy-MM-dd}) vs #{next.Id} ({next.InvoiceDate:yyyy-MM-dd})");

                if (current.InvoiceDate == next.InvoiceDate)
                {
                    Assert.True(string.CompareOrdinal(current.InvoiceNumber, next.InvoiceNumber) <= 0,
                        $"InvoiceNumber out of order for same date: #{current.Id} ({current.InvoiceNumber}) vs #{next.Id} ({next.InvoiceNumber})");
                }
            }
        }

        [Fact]
        public async Task Test_MaxLimitConstraint_Reject51Invoices()
        {
            // Arrange: 51 invoice IDs
            var ids = Enumerable.Range(1, 51).Select(i => (long)i).ToList();
            var req = new CreateJobRequest { InvoiceIds = ids };

            // Act & Assert: Must throw ArgumentException for > 50 items
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _signingService.CreateJobAsync(req));
            Assert.Contains("50", ex.Message);
        }

        [Fact]
        public async Task Test_MaxLimitConstraint_Accept50Invoices()
        {
            // Arrange: Exactly 50 invoice IDs
            var ids = Enumerable.Range(1, 50).Select(i => (long)i).ToList();
            var req = new CreateJobRequest { InvoiceIds = ids };

            // Act
            var job = await _signingService.CreateJobAsync(req);

            // Assert
            Assert.NotNull(job);
            Assert.Equal(50, job.Total);
            Assert.Equal(50, job.Items.Count);
        }

        [Fact]
        public async Task Test_EmptyInvoiceList_Reject0Invoices()
        {
            // Arrange: 0 invoice IDs
            var req = new CreateJobRequest { InvoiceIds = new List<long>() };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _signingService.CreateJobAsync(req));
        }

        [Fact]
        public async Task Test_GetXml_ReturnsValidXml()
        {
            // Act
            var xml = await _invoiceService.GetXmlAsync(1);

            // Assert
            Assert.NotNull(xml);
            Assert.Contains("<HDon>", xml);
            Assert.Contains("NACENCOMM1", xml);
        }

        [Fact]
        public async Task Test_XmlSanitizer_ProhibitsXXEAndMalformedXml()
        {
            // Test 1: Malformed XXE payload
            string xxePayload = @"<!DOCTYPE foo [<!ENTITY xxe SYSTEM ""file:///etc/passwd"">]><HDon><DLHDon>&xxe;</DLHDon></HDon>";
            bool result1 = XmlSanitizer.ValidateAndSanitizeXml(xxePayload, out _, out var err1);
            Assert.False(result1);
            Assert.NotNull(err1);

            // Test 2: Invalid root element
            string badRoot = @"<InvalidRoot><Data>123</Data></InvalidRoot>";
            bool result2 = XmlSanitizer.ValidateAndSanitizeXml(badRoot, out _, out var err2);
            Assert.False(result2);
            Assert.Contains("HDon", err2);

            // Test 3: Valid XML
            string validXml = @"<HDon><DLHDon><TTChung><PBan>2.0.0</PBan></TTChung></DLHDon></HDon>";
            bool result3 = XmlSanitizer.ValidateAndSanitizeXml(validXml, out var clean, out var err3);
            Assert.True(result3);
            Assert.Null(err3);
            Assert.Contains("<HDon>", clean);
        }

        [Fact]
        public async Task Test_UpdateSignedXml_UpdatesInvoiceStatus()
        {
            // Arrange
            long invoiceId = 1;
            string signedXml = "<HDon><Signature>MOCK_SIGN</Signature></HDon>";
            string subject = "CN=NACENCOMM CA2 TEST";

            // Act
            bool success = await _invoiceService.UpdateSignedXmlAsync(invoiceId, signedXml, subject);
            var updatedInv = await _invoiceService.GetByIdAsync(invoiceId);

            // Assert
            Assert.True(success);
            Assert.NotNull(updatedInv);
            Assert.Equal("ĐÃ KÝ SỐ", updatedInv.Status);
            Assert.Equal(signedXml, updatedInv.SignedXmlContent);
            Assert.Equal(subject, updatedInv.SignerSubject);
        }

        [Fact]
        public async Task Test_SigningJob_ItemStatusProgressTracking()
        {
            // Arrange
            var req = new CreateJobRequest { InvoiceIds = new List<long> { 1, 2 } };
            var job = await _signingService.CreateJobAsync(req);

            // Act 1: Item 1 success
            await _signingService.UpdateItemStatusAsync(job.JobId, 1, "Success");
            var jobState1 = await _signingService.GetJobAsync(job.JobId);

            Assert.Equal(1, jobState1!.Completed);
            Assert.Equal(1, jobState1.Success);
            Assert.Equal(0, jobState1.Failed);

            // Act 2: Item 2 failed
            await _signingService.UpdateItemStatusAsync(job.JobId, 2, "Failed", SigningErrorCodes.TOKEN_NOT_AVAILABLE, "Rút USB Token");
            var jobState2 = await _signingService.GetJobAsync(job.JobId);

            Assert.Equal(2, jobState2!.Completed);
            Assert.Equal(1, jobState2.Success);
            Assert.Equal(1, jobState2.Failed);
            Assert.Equal("Completed", jobState2.Status);
        }

        [Fact]
        public async Task Test_ExportToExcel_ReturnsValidExcelStream()
        {
            // Arrange
            var filter = new InvoiceFilter { TabStatus = "ISSUED" };

            // Act
            var excelBytes = await _invoiceService.ExportToExcelAsync(filter);

            // Assert
            Assert.NotNull(excelBytes);
            Assert.True(excelBytes.Length > 0, "Excel output byte array should not be empty.");
            // OpenXml / xlsx files start with PK zip header bytes (0x50, 0x4B, 0x03, 0x04)
            Assert.Equal(0x50, excelBytes[0]);
            Assert.Equal(0x4B, excelBytes[1]);
        }
    }
}
