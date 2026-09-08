# BÁO CÁO TỔNG THỂ DỰ ÁN & HƯỚNG DẪN BÀN GIAO (PROJECT HANDOVER NOTES)
> **Dự án**: Web Quản lý Hóa đơn Điện tử & Chữ ký số (.NET 10 Core MVC + WinForms Signer Local)  
> **Phiên bản**: V1.0.0 (Hoàn thành trọn bộ 7 Phase)  
> **Thời gian cập nhật**: 08/09/2026  

---

## 1. TỔNG QUAN HỆ THỐNG & KIẾN TRÚC (ARCHITECTURE)

Hệ thống được phát triển theo kế hoạch Master Plan (`PLAN.md` và `phases/PHASE-01` -> `PHASE-07`), tái hiện giao diện từ ảnh mẫu `DanhsachHD.aspx` kết hợp mô hình xử lý ký số an toàn qua ứng dụng WinForms cục bộ.

### Sơ đồ Luồng Dữ liệu (Data Flow)
```text
Trình duyệt (Browser UI - http://localhost:5000)
  │
  ├─► 1. Lọc / Tìm kiếm Hóa đơn ──► ASP.NET Core MVC (Controllers/Services)
  │                                     │ (Sắp xếp InvoiceDate ASC, Max 50 items)
  │                                     ▼
  ├─► 2. Lấy raw XML từng hóa đơn ──────┤ XML Sanitizer (Chống XXE, DTD Injection)
  │
  ├─► 3. Gửi XML ký tuần tự (Worker concurrency = 1)
  │      │
  │      ▼ http://127.0.0.1:8765
  └─► E-invoiceCA2 WinForms Signer Service (API Cục bộ)
         │
         ├── GET /api/status (Kiểm tra kết nối)
         ├── GET /api/certificate (Đọc X509Store / USB Token Metadata)
         └── POST /api/sign (Ký XMLDSIG RSA-SHA256)
```

---

## 2. BẢN ĐỒ MÃ NGUỒN & CÁC THÀNH PHẦN (FILE MAP)

### Solution: `Nacencomm.InvoiceManagement.slnx`

#### A. Dự án Web Quản lý Hóa đơn (`src/Nacencomm.InvoiceManagement/`)
* **[Invoice.cs](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/Models/Invoice.cs)**: Domain model chứa đầy đủ thông tin hóa đơn (ID, Số HĐ, Mẫu số, Ký hiệu, Ngày xuất, MST, Người mua, Tổng tiền, Trạng thái...).
* **[InvoiceFilter.cs](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/Models/InvoiceFilter.cs)**: Model bộ lọc tìm kiếm (Đơn vị, Loại HĐ, Mẫu số, Ký hiệu, Radio Ngày/Số/Mã tra cứu, Từ ngày - Đến ngày, TabStatus).
* **[SigningModels.cs](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/Models/SigningModels.cs)**: Định nghĩa DTO Ký số, Job ký, và bộ mã lỗi chuẩn (`SIGNER_NOT_RUNNING`, `TOKEN_NOT_AVAILABLE`, `INVALID_XML`, `CANCELLED`...).
* **[XmlSanitizer.cs](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/Services/XmlSanitizer.cs)**: Service kiểm tra và bảo mật XML (Prohibit DTD Processing, Disable External Entities) chống XXE.
* **[InvoiceService.cs](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/Services/InvoiceService.cs)**: Quản lý danh sách hóa đơn sample, lọc dữ liệu và sắp xếp theo `InvoiceDate ASC`, `InvoiceNumber ASC`.
* **[SigningService.cs](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/Services/SigningService.cs)**: Quản lý Job ký số hàng loạt, kiểm soát giới hạn cứng **chỉ cho phép ≤ 50 items/job**.
* **[InvoiceController.cs](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/Controllers/InvoiceController.cs)**: Phục vụ UI, AJAX Table rendering, API lấy XML hóa đơn (`GetXml`) và nhận kết quả ký (`SubmitSignedXml`).
* **[SigningController.cs](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/Controllers/SigningController.cs)**: API quản lý khởi tạo Job (`/api/Signing/create-job`) và cập nhật kết quả từng hóa đơn (`/api/Signing/job/{jobId}/update-item`).
* **Giao diện Views**:
  - [_Layout.cshtml](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/Views/Shared/_Layout.cshtml): Khung giao diện chuẩn Nacencomm (Header + Left Sidebar).
  - [Index.cshtml](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/Views/Invoice/Index.cshtml): Màn hình chính chứa Filter Panel, Tab trạng thái, Toolbar chọn tối đa 50 và Nút Ký hàng loạt.
  - [_InvoiceTablePartial.cshtml](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/Views/Invoice/_InvoiceTablePartial.cshtml): Bảng hiển thị danh sách hóa đơn.
  - [_SigningModal.cshtml](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/Views/Invoice/_SigningModal.cshtml): Pop-up tiến trình ký thời gian thực (Progress bar, Metrics counters, Item status table).
* **Frontend Javascript Assets**:
  - [winforms-signer.js](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/wwwroot/js/winforms-signer.js): Client gọi HTTP WinForms API (`127.0.0.1:8765`), hỗ trợ nút Dev Mock Mode.
  - [signing-queue.js](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/wwwroot/js/signing-queue.js): Worker ký tuần tự `Concurrency = 1`, sắp xếp thứ tự ngày ASC, hỗ trợ Hủy an toàn.
  - [invoice-manager.js](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.InvoiceManagement/wwwroot/js/invoice-manager.js): Logic kiểm soát chọn tối đa 50 hóa đơn, cập nhật bộ đếm `X / 50` và kích hoạt Modal.

#### B. Dự án WinForms Signer API Cục bộ (`src/Nacencomm.WinFormsSigner/`)
* **[Program.cs](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/src/Nacencomm.WinFormsSigner/Program.cs)**: Chạy ngầm tại `http://127.0.0.1:8765`. Quét `X509Store` tìm chứng thư CA2 và thực thi ký số XMLDSIG (`SignedXml`).

#### C. Dự án Unit Test (`tests/Nacencomm.InvoiceManagement.Tests/`)
* **[InvoiceSigningTests.cs](file:///c:/Users/Mint/Downloads/nacencomm_invoice_signing_plan/nacencomm_invoice_signing_plan/tests/Nacencomm.InvoiceManagement.Tests/InvoiceSigningTests.cs)**: Bộ 9 test case tự động kiểm tra sắp xếp ngày, chọn 0, 1, 50, từ chối 51, chống XXE, cập nhật trạng thái và theo dõi Job.

---

## 3. TRẠNG THÁI TRIỂN KHAI 7 PHASE (COMPLETED PHASES)

| Phase | Tên Phase | Trạng thái | Nội dung đã xử lý |
| :--- | :--- | :---: | :--- |
| **Phase 01** | UI quản lý hóa đơn | ✅ **DONE** | Replicate giao diện `DanhsachHD.aspx`. Bổ sung Batch Toolbar chọn max 50 items, tự động sort `InvoiceDate ASC`. |
| **Phase 02** | Backend API Integration | ✅ **DONE** | Tách Adapter Pattern qua `IInvoiceService`. Lấy XML độc lập từng hóa đơn, cập nhật trạng thái không ảnh hưởng hóa đơn khác. |
| **Phase 03** | WinForms Local Signer | ✅ **DONE** | Tạo ứng dụng `Nacencomm.WinFormsSigner` lắng nghe loopback `127.0.0.1:8765` phục vụ `GET /api/status`, `GET /api/certificate`, `POST /api/sign`. |
| **Phase 04** | Signing Queue & Batch | ✅ **DONE** | Xây dựng queue worker ký tuần tự (`Concurrency = 1`). Xử lý trạng thái từng hóa đơn: `Pending` -> `GettingXml` -> `Signing` -> `Submitting` -> `Success`/`Failed`. |
| **Phase 05** | Progress & Error Codes | ✅ **DONE** | Chuẩn hóa danh mục mã lỗi (`SIGNER_NOT_RUNNING`, `TOKEN_NOT_AVAILABLE`, `INVALID_XML`...). Thiết kế Pop-up Modal theo dõi tiến trình thời gian thực. |
| **Phase 06** | Security & Production | ✅ **DONE** | Bảo mật XML qua `XmlSanitizer.cs` (Chống XXE / DTD Injection), Bind cổng Loopback `127.0.0.1`, không lộ Private Key / PIN. |
| **Phase 07** | Testing & Acceptance | ✅ **DONE** | Bộ 9 Unit test tự động trong xUnit pass 100% (Duration ~88ms). |

---

## 4. HƯỚNG DẪN VẬN HÀNH & KIỂM THỬ (HOW TO RUN & TEST)

### A. Lệnh Lệnh Khởi chạy Dịch vụ

#### 1. Khởi chạy Web Application (Cổng 5000)
```powershell
dotnet run --project src/Nacencomm.InvoiceManagement/Nacencomm.InvoiceManagement.csproj
```
*Truy cập*: **[http://localhost:5000](http://localhost:5000)** (hoặc `http://localhost:5159`)

#### 2. Khởi chạy WinForms Signer API (Cổng 8765)
```powershell
dotnet run --project src/Nacencomm.WinFormsSigner/Nacencomm.WinFormsSigner.csproj
```
*Kiểm tra Status*: **`http://127.0.0.1:8765/api/status`**

### B. Chạy Unit Tests
```powershell
dotnet test
```
*Kết quả kỳ vọng*: `Passed! - Failed: 0, Passed: 9, Skipped: 0, Total: 9`

---

## 5. CÁC QUY TẮC CỐT LÕI CẦN LƯU Ý CHO THREAD SAU

1. **Quy tắc 50 Items**: Không bao giờ cho phép chọn hoặc tạo Job ký vượt quá 50 hóa đơn (Đã được validate ở Client UI `invoice-manager.js` và Server `SigningService.cs`).
2. **Quy tắc Sắp xếp**: Toàn bộ danh sách ký phải được sắp xếp tăng dần theo ngày xuất (`InvoiceDate ASC`), sau đó theo số hóa đơn (`InvoiceNumber ASC`).
3. **Quy tắc Tuần tự**: Worker ký bắt buộc xử lý lần lượt từng hóa đơn (`Concurrency = 1`). Tuyệt đối không dùng `Promise.all()` hoặc ký song song làm treo USB Token/CSP Driver.
4. **Giả lập Ký số (Mock Signer Mode)**: Nếu máy phát triển chưa cắm USB Token CA2 hoặc chưa bật ứng dụng WinForms, chỉ cần click nút **"Bật/Tắt Mock Signer WinForms"** trên góc phải màn hình Web để giả lập ký full 50 hóa đơn.

---

> 🎯 **Thread sau có thể tiếp tục mở rộng**: Tích hợp SOAP API Backend thật của Nacencomm vào `InvoiceService.cs` hoặc bổ sung export báo cáo chi tiết đợt ký ra Excel/PDF.
