using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Nacencomm.InvoiceManagement.Models;

namespace Nacencomm.InvoiceManagement.Services
{
    public class InvoiceService : IInvoiceService
    {
        private static readonly ConcurrentDictionary<long, Invoice> _invoices = new();

        static InvoiceService()
        {
            SeedInitialData();
        }

        private static void SeedInitialData()
        {
            var random = new Random(42);
            var baseDate = new DateTime(2026, 8, 25);
            
            var buyers = new[]
            {
                ("Công ty TNHH Phần Mềm & Giải Pháp Công Nghệ Việt", "0108991234"),
                ("Công ty CP Tập Đoàn Đầu Tư & Phát Triển Hải Hà", "0301445678"),
                ("Tổng Công Ty Điện Lực Hà Nội - Chi Nhánh 1", "0100101156"),
                ("Công ty TNHH Dịch Vụ Thương Mại Tổng Hợp Sao Mai", "0105678901"),
                ("Công ty CP Xây Dựng & Thiết Kế Kiến Trúc An Khánh", "0109123456"),
                ("Công ty TNHH Xuất Nhập Khẩu Nông Sản Miền Bắc", "0103344556"),
                ("Công ty CP Vật Tư & Thiết Bị Y Tế Thủ Đô", "0107788990"),
                ("Công ty TNHH Tư Vấn Tài Chính & Kế Toán Á Châu", "0102233445")
            };

            // Seed 60 invoices for testing filtering, pagination, and batch limits (> 50 items)
            for (long i = 1; i <= 60; i++)
            {
                var buyer = buyers[(i - 1) % buyers.Length];
                var dateOffset = (i % 7);
                var invDate = baseDate.AddDays(dateOffset);
                var invNum = $"{i}";
                var amount = random.Next(1500000, 85000000);
                
                string tabStatus = (i % 5 == 0) ? "Mới lập" : "ĐÃ PHÁT HÀNH";
                string formType = (i % 5 == 0) ? "Hóa đơn mới" : ((i % 3 == 0) ? "Khởi tạo từ máy tính tiền" : "Hóa đơn điện tử");
                
                if (i == 3) tabStatus = "ĐÃ KÝ SỐ";
                if (i == 7) tabStatus = "LỖI KÝ";

                var rawXml = $@"<HDon>
    <DLHDon Id=""HD_{i:D6}"">
        <TTChung>
            <PBan>2.0.0</PBan>
            <THDon>Hóa đơn giá trị gia tăng</THDon>
            <KHMSHDon>1</KHMSHDon>
            <KHHDon>C26MQT</KHHDon>
            <SHDon>{invNum}</SHDon>
            <NLap>{invDate:yyyy-MM-dd}</NLap>
            <DVTTe>VND</DVTTe>
        </TTChung>
        <NDHDon>
            <NBan>
                <Ten>Công ty cổ phần công nghệ thẻ NACENCOMM1</Ten>
                <MST>0102030405</MST>
                <DChi>Tầng 5, Tòa nhà CA2, Hà Nội</DChi>
            </NBan>
            <NMua>
                <Ten>{buyer.Item1}</Ten>
                <MST>{buyer.Item2}</MST>
            </NMua>
            <TToan>
                <TGTThue>{amount:F0}</TGTThue>
            </TToan>
        </NDHDon>
    </DLHDon>
</HDon>";

                var inv = new Invoice
                {
                    Id = i,
                    InvoiceNumber = invNum,
                    PatternSymbol = "1",
                    InvoiceSymbol = "C26MQT",
                    InvoiceType = "Hóa đơn giá trị gia tăng",
                    InvoiceDate = invDate,
                    CompanyUnit = "Công ty cổ phần công nghệ thẻ NACENCOMM1",
                    BuyerName = buyer.Item1,
                    BuyerTaxCode = buyer.Item2,
                    BuyerPhone = "09999999999",
                    BuyerEmail = "ndtoan161075@gmail.com",
                    TotalAmount = amount,
                    Currency = "VND",
                    LookupCode = $"NC26-{i:D5}-X",
                    Status = tabStatus,
                    FormType = formType,
                    XmlContent = rawXml
                };

                if (formType == "Khởi tạo từ máy tính tiền")
                {
                    inv.TaxAuthorityCode = $"M1-26-ZAMJZ-{1000 + i:D7}";
                    if (i % 2 == 0)
                    {
                        inv.CqtStatusText = $"Mã của CQT: {inv.TaxAuthorityCode}. Hóa đơn hợp lệ.";
                    }
                }

                if (tabStatus == "ĐÃ KÝ SỐ")
                {
                    inv.SignedDate = invDate.AddHours(2);
                    inv.SignerSubject = "CN=CÔNG TY CỔ PHẦN CÔNG NGHỆ THẺ NACENCOMM1, MST=0102030405";
                    inv.SignedXmlContent = rawXml.Replace("</HDon>", "<Signature>MOCK_SIGNATURE</Signature></HDon>");
                }

                _invoices[inv.Id] = inv;
            }

            // Seed specific invoices matched with old UI screenshots for perfect visualization
            var screenshotInv1 = new Invoice
            {
                Id = 16867529,
                InvoiceNumber = "4",
                PatternSymbol = "1",
                InvoiceSymbol = "C26MQT",
                InvoiceType = "Hóa đơn giá trị gia tăng",
                InvoiceDate = new DateTime(2026, 8, 4),
                CompanyUnit = "Công ty cổ phần công nghệ thẻ NACENCOMM1",
                BuyerName = "",
                BuyerPhone = "09999999999",
                BuyerEmail = "ndtoan161075@gmail.com",
                TotalAmount = 0,
                Currency = "VND",
                LookupCode = "ELJUTTIXY",
                Status = "Đã ký xác thực",
                FormType = "Khởi tạo từ máy tính tiền",
                TaxAuthorityCode = "M1-26-ZAMJZ-00000001000",
                XmlContent = "<HDon><DLHDon></DLHDon></HDon>"
            };
            _invoices[screenshotInv1.Id] = screenshotInv1;

            var screenshotInv2 = new Invoice
            {
                Id = 16902719,
                InvoiceNumber = "5",
                PatternSymbol = "1",
                InvoiceSymbol = "C26MQT",
                InvoiceType = "Hóa đơn giá trị gia tăng",
                InvoiceDate = new DateTime(2026, 8, 6),
                CompanyUnit = "Công ty cổ phần công nghệ thẻ NACENCOMM1",
                BuyerName = "",
                TotalAmount = 500000,
                Currency = "VND",
                LookupCode = "BGGN4FH5M",
                Status = "Mới lập",
                FormType = "Khởi tạo từ máy tính tiền",
                TaxAuthorityCode = "M1-26-ZAMJZ-00000001003",
                CqtStatusText = "Mã của CQT: M1-26-ZAMJZ-00000001003. Hóa đơn hợp lệ.",
                XmlContent = "<HDon><DLHDon></DLHDon></HDon>"
            };
            _invoices[screenshotInv2.Id] = screenshotInv2;

            var screenshotInv3 = new Invoice
            {
                Id = 16902715,
                InvoiceNumber = "0",
                PatternSymbol = "1",
                InvoiceSymbol = "C26MQT",
                InvoiceType = "Hóa đơn giá trị gia tăng",
                InvoiceDate = new DateTime(2026, 8, 6),
                CompanyUnit = "Công ty cổ phần công nghệ thẻ NACENCOMM1",
                BuyerName = "",
                TotalAmount = 500000,
                Currency = "VND",
                LookupCode = "R4EBCSGD8",
                Status = "Mới lập",
                FormType = "Hóa đơn mới",
                XmlContent = "<HDon><DLHDon></DLHDon></HDon>"
            };
            _invoices[screenshotInv3.Id] = screenshotInv3;
        }

        public Task<IReadOnlyList<Invoice>> GetInvoicesAsync(InvoiceFilter filter)
        {
            IEnumerable<Invoice> query = _invoices.Values;

            // Filter by TabStatus
            if (!string.IsNullOrEmpty(filter.TabStatus))
            {
                if (filter.TabStatus.Equals("DRAFT", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.Status == "Mới lập" || x.Status == "HÓA ĐƠN NHÁP");
                }
                else if (filter.TabStatus.Equals("ISSUED", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.Status == "ĐÃ PHÁT HÀNH" || x.Status == "ĐÃ KÝ SỐ" || x.Status == "LỖI KÝ");
                }
                else if (filter.TabStatus.Equals("POS", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.FormType.Contains("máy tính tiền", StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(filter.SubTabStatus))
                    {
                        if (filter.SubTabStatus.Equals("NEW", StringComparison.OrdinalIgnoreCase))
                        {
                            query = query.Where(x => string.IsNullOrEmpty(x.CqtStatusText));
                        }
                        else if (filter.SubTabStatus.Equals("SENT_CQT", StringComparison.OrdinalIgnoreCase))
                        {
                            query = query.Where(x => !string.IsNullOrEmpty(x.CqtStatusText) || !string.IsNullOrEmpty(x.TaxAuthorityCode));
                        }
                    }
                }
            }

            // Filter by CompanyUnit
            if (!string.IsNullOrWhiteSpace(filter.CompanyUnit) && filter.CompanyUnit != "Tất cả")
            {
                query = query.Where(x => x.CompanyUnit == filter.CompanyUnit);
            }

            // Filter by InvoiceType
            if (!string.IsNullOrWhiteSpace(filter.InvoiceType) && filter.InvoiceType != "Tất cả")
            {
                query = query.Where(x => x.InvoiceType == filter.InvoiceType);
            }

            // Filter by Pattern Symbol
            if (!string.IsNullOrWhiteSpace(filter.PatternSymbol) && filter.PatternSymbol != "Tất cả")
            {
                query = query.Where(x => x.PatternSymbol == filter.PatternSymbol);
            }

            // Filter by Invoice Symbol
            if (!string.IsNullOrWhiteSpace(filter.InvoiceSymbol) && filter.InvoiceSymbol != "Tất cả")
            {
                query = query.Where(x => x.InvoiceSymbol == filter.InvoiceSymbol);
            }

            // Date Range
            if (filter.FromDate.HasValue)
            {
                query = query.Where(x => x.InvoiceDate.Date >= filter.FromDate.Value.Date);
            }
            if (filter.ToDate.HasValue)
            {
                query = query.Where(x => x.InvoiceDate.Date <= filter.ToDate.Value.Date);
            }

            // Search Keyword
            if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
            {
                var kw = filter.SearchKeyword.Trim();
                if (filter.SearchBy == "Number")
                {
                    query = query.Where(x => x.InvoiceNumber.Contains(kw, StringComparison.OrdinalIgnoreCase));
                }
                else if (filter.SearchBy == "LookupCode")
                {
                    query = query.Where(x => x.LookupCode.Contains(kw, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    query = query.Where(x => x.InvoiceNumber.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                                             x.BuyerName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                                             x.BuyerTaxCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                                             x.LookupCode.Contains(kw, StringComparison.OrdinalIgnoreCase));
                }
            }

            // MANDATORY Requirement from Plan: Sort InvoiceDate ASC, then InvoiceNumber ASC
            var ordered = query
                .OrderBy(x => x.InvoiceDate)
                .ThenBy(x => x.InvoiceNumber)
                .ToList();

            return Task.FromResult<IReadOnlyList<Invoice>>(ordered);
        }

        public Task<Invoice?> GetByIdAsync(long id)
        {
            _invoices.TryGetValue(id, out var invoice);
            return Task.FromResult(invoice);
        }

        public Task<string> GetXmlAsync(long invoiceId)
        {
            if (_invoices.TryGetValue(invoiceId, out var invoice))
            {
                return Task.FromResult(invoice.XmlContent);
            }
            throw new KeyNotFoundException($"Invoice #{invoiceId} not found");
        }

        public Task<bool> UpdateSignedXmlAsync(long invoiceId, string signedXml, string? signerSubject)
        {
            if (_invoices.TryGetValue(invoiceId, out var invoice))
            {
                invoice.SignedXmlContent = signedXml;
                invoice.Status = "ĐÃ KÝ SỐ";
                invoice.SignedDate = DateTime.Now;
                invoice.SignerSubject = signerSubject ?? "CN=NACENCOMM CA2";
                invoice.ErrorMessage = null;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> UpdateInvoiceStatusAsync(long invoiceId, string status, string? errorMessage)
        {
            if (_invoices.TryGetValue(invoiceId, out var invoice))
            {
                invoice.Status = status;
                invoice.ErrorMessage = errorMessage;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public async Task<byte[]> ExportToExcelAsync(InvoiceFilter filter)
        {
            var invoices = await GetInvoicesAsync(filter);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Danh sách hóa đơn");

            // Title
            worksheet.Cell(1, 1).Value = "DANH SÁCH HÓA ĐƠN";
            worksheet.Range(1, 1, 1, 16).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(16)
                .Font.SetFontColor(XLColor.FromHtml("#0066cc"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            worksheet.Cell(2, 1).Value = $"Đơn vị: {filter.CompanyUnit} | Ngày xuất b/c: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            worksheet.Range(2, 1, 2, 16).Merge().Style
                .Font.SetItalic()
                .Font.SetFontSize(10)
                .Font.SetFontColor(XLColor.DarkGray)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Headers
            string[] headers = new string[]
            {
                "STT", "IDHD", "Ngày hóa đơn", "Mẫu số", "Ký hiệu", "Số HĐ",
                "Người mua", "Mã số thuế", "Số điện thoại", "Email",
                "Tổng tiền thanh toán", "Loại tiền", "Mã tra cứu", "Hình thức", "Trạng thái", "Mã CQT"
            };

            int headerRow = 4;
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cell(headerRow, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#337ab7");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            int row = 5;
            int stt = 1;
            foreach (var inv in invoices)
            {
                worksheet.Cell(row, 1).SetValue(stt++);
                worksheet.Cell(row, 2).SetValue(inv.Id);
                worksheet.Cell(row, 3).SetValue(inv.InvoiceDate.ToString("dd/MM/yyyy"));
                worksheet.Cell(row, 4).SetValue(inv.PatternSymbol);
                worksheet.Cell(row, 5).SetValue(inv.InvoiceSymbol);
                worksheet.Cell(row, 6).SetValue(inv.InvoiceNumber);
                worksheet.Cell(row, 7).SetValue(inv.BuyerName);
                worksheet.Cell(row, 8).SetValue(inv.BuyerTaxCode);
                worksheet.Cell(row, 9).SetValue(inv.BuyerPhone);
                worksheet.Cell(row, 10).SetValue(inv.BuyerEmail);

                var amountCell = worksheet.Cell(row, 11);
                amountCell.SetValue(inv.TotalAmount);
                amountCell.Style.NumberFormat.Format = "#,##0";

                worksheet.Cell(row, 12).SetValue(inv.Currency);
                worksheet.Cell(row, 13).SetValue(inv.LookupCode);
                worksheet.Cell(row, 14).SetValue(inv.FormType);
                worksheet.Cell(row, 15).SetValue(inv.Status);
                worksheet.Cell(row, 16).SetValue(inv.TaxAuthorityCode ?? "");

                // Alignment
                worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                worksheet.Cell(row, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                row++;
            }

            // Total Summary Row
            if (invoices.Count > 0)
            {
                worksheet.Cell(row, 1).Value = "Tổng cộng";
                worksheet.Range(row, 1, row, 10).Merge().Style
                    .Font.SetBold()
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                var totalSumCell = worksheet.Cell(row, 11);
                totalSumCell.Value = invoices.Sum(x => x.TotalAmount);
                totalSumCell.Style.Font.Bold = true;
                totalSumCell.Style.NumberFormat.Format = "#,##0";
                totalSumCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Range(row, 1, row, 16).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");
            }
            else
            {
                worksheet.Cell(row, 1).Value = "Không có dữ liệu phù hợp.";
                worksheet.Range(row, 1, row, 16).Merge().Style
                    .Font.SetItalic()
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                row++;
            }

            // Apply borders
            int endRow = invoices.Count > 0 ? row : row - 1;
            var dataRange = worksheet.Range(headerRow, 1, endRow, headers.Length);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
