using System;

namespace Nacencomm.InvoiceManagement.Models
{
    public class InvoiceFilter
    {
        public string CompanyUnit { get; set; } = "Công ty cổ phần công nghệ thẻ NACENCOMM1";
        public string InvoiceType { get; set; } = "Hóa đơn giá trị gia tăng";
        public string PatternSymbol { get; set; } = "1";
        public string InvoiceSymbol { get; set; } = "C26MQT";
        public string SearchBy { get; set; } = "Date"; // "Date", "Number", "LookupCode"
        public string SearchKeyword { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string TabStatus { get; set; } = "DRAFT"; // "DRAFT", "ISSUED", "POS"
        public string SubTabStatus { get; set; } = "ALL"; // "ALL", "NEW", "SENT_CQT"
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
