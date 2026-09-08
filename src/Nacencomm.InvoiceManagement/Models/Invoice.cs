using System;

namespace Nacencomm.InvoiceManagement.Models
{
    public class Invoice
    {
        public long Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string PatternSymbol { get; set; } = "1";
        public string InvoiceSymbol { get; set; } = "C26MQT";
        public string InvoiceType { get; set; } = "Hóa đơn giá trị gia tăng";
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public string CompanyUnit { get; set; } = "Công ty cổ phần công nghệ thẻ NACENCOMM1";
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerTaxCode { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string LookupCode { get; set; } = string.Empty;
        public string Currency { get; set; } = "VND";
        public string BuyerPhone { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public string? TaxAuthorityCode { get; set; }
        public string? CqtStatusText { get; set; }

        /// <summary>
        /// Trạng thái hóa đơn: "Mới lập", "HÓA ĐƠN NHÁP", "ĐÃ PHÁT HÀNH", "ĐÃ KÝ SỐ", "LỖI KÝ"
        /// </summary>
        public string Status { get; set; } = "Mới lập";

        /// <summary>
        /// Hình thức HĐ: "Hóa đơn điện tử", "Khởi tạo từ máy tính tiền", "Hóa đơn mới"
        /// </summary>
        public string FormType { get; set; } = "Hóa đơn điện tử";

        public string XmlContent { get; set; } = string.Empty;
        public string? SignedXmlContent { get; set; }
        public DateTime? SignedDate { get; set; }
        public string? SignerSubject { get; set; }
        public string? ErrorMessage { get; set; }

        public bool CanBeSigned => Status == "Mới lập" || Status == "HÓA ĐƠN NHÁP" || Status == "ĐÃ PHÁT HÀNH" || Status == "LỖI KÝ";
    }
}
