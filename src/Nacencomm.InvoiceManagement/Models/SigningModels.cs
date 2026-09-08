using System;
using System.Collections.Generic;

namespace Nacencomm.InvoiceManagement.Models
{
    public static class SigningErrorCodes
    {
        public const string SIGNER_NOT_RUNNING = "SIGNER_NOT_RUNNING";
        public const string CERTIFICATE_NOT_FOUND = "CERTIFICATE_NOT_FOUND";
        public const string CERTIFICATE_EXPIRED = "CERTIFICATE_EXPIRED";
        public const string TOKEN_NOT_AVAILABLE = "TOKEN_NOT_AVAILABLE";
        public const string SIGN_FAILED = "SIGN_FAILED";
        public const string INVALID_XML = "INVALID_XML";
        public const string BACKEND_TIMEOUT = "BACKEND_TIMEOUT";
        public const string BACKEND_UNAVAILABLE = "BACKEND_UNAVAILABLE";
        public const string AUTH_FAILED = "AUTH_FAILED";
        public const string BUSINESS_ERROR = "BUSINESS_ERROR";
        public const string ALREADY_PROCESSED = "ALREADY_PROCESSED";
        public const string MAX_LIMIT = "MAX_LIMIT";
        public const string CANCELLED = "CANCELLED";
    }

    public class SigningRequest
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public long InvoiceId { get; set; }
        public string Xml { get; set; } = string.Empty;
    }

    public class SigningResult
    {
        public Guid RequestId { get; set; }
        public long InvoiceId { get; set; }
        public bool Success { get; set; }
        public string? SignedXml { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? SignedDate { get; set; }
    }

    public class CertificateInfo
    {
        public string Subject { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class SigningJob
    {
        public Guid JobId { get; set; } = Guid.NewGuid();
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }
        public string Status { get; set; } = "Created"; // "Created", "Processing", "Completed", "Cancelled"
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<SigningJobItem> Items { get; set; } = new();
    }

    public class SigningJobItem
    {
        public long InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string Status { get; set; } = "Pending"; // "Pending", "GettingXml", "Signing", "Submitting", "Success", "Failed"
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class CreateJobRequest
    {
        public List<long> InvoiceIds { get; set; } = new();
    }
}
