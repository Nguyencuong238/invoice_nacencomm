using System;
using System.Collections.Generic;

namespace Nacencomm.InvoiceManagement.Models
{
    public class InvoiceLineItem
    {
        public int ItemNo { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = "Cái";
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal Amount => Quantity * UnitPrice;
        public string VatRate { get; set; } = "10%";
    }

    public class Invoice
    {
        public long Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string PatternSymbol { get; set; } = "1";
        public string InvoiceSymbol { get; set; } = "C26MQT";
        public string InvoiceType { get; set; } = "Hóa đơn giá trị gia tăng";
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public string CompanyUnit { get; set; } = "CÔNG TY CỔ PHẦN CÔNG NGHỆ THẺ NACENCOMM1";
        
        // Seller Information matching xem-truoc-hoa-don.png
        public string SellerTaxCode { get; set; } = "0103930279-999";
        public string SellerAddress { get; set; } = "Tòa nhà BOHEMIA số 25, Lê Văn Thiêm, Thanh Xuân, Hà Nội, Việt Nam";
        public string SellerBankAccount { get; set; } = "22222222341 Tại: Ngân hàng Tiên phong bank- chi nhánh hà thành";
        public string SellerPhone { get; set; } = "1900.54.54.07";

        // Buyer Information
        public string BuyerCustomerName { get; set; } = "Khách MTT";
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerTaxCode { get; set; } = string.Empty;
        public string BuyerAddress { get; set; } = string.Empty;
        public string BuyerBankAccount { get; set; } = "1244444444 Tại: Chi nhánh ACB";
        public string PaymentMethod { get; set; } = "TM/CK";

        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "VND";
        public string LookupCode { get; set; } = string.Empty;
        public string BuyerPhone { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public string? TaxAuthorityCode { get; set; }
        public string? CqtStatusText { get; set; }
        public string AmountInWords { get; set; } = string.Empty;
        public string DigestValue { get; set; } = "dGVzdERpZ2VzdFZhbHVlMTIzNDU2Nzg5";

        public List<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();

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

        public bool CanBeSigned => Status == "Mới lập" || Status == "HÓA ĐƠN NHÁP" || Status == "LỖI KÝ";
    }
}

