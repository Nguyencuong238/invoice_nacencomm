# Nacencomm Invoice Signing System (.NET)

Hệ thống Web Quản lý Hóa đơn Điện tử & Ký số qua WinForms Signer Cục bộ đã được phát triển hoàn thành **trọn bộ 7 Phase**.

## 📄 Tài liệu Ghi chú Bàn giao (Handover Notes)
👉 **[PROJECT_HANDOVER_NOTES.md](PROJECT_HANDOVER_NOTES.md)**: Chứa toàn bộ thông tin kiến trúc, sơ đồ luồng dữ liệu, bản đồ mã nguồn, kết quả kiểm thử và hướng dẫn vận hành chi tiết cho các thread sau.

## Nguyên tắc
- Giữ nguyên backend hiện tại trong V1.
- Browser giao tiếp WinForms qua localhost HTTP (`http://127.0.0.1:8765`).
- Giữ nguyên logic crypto XMLDSIG RSA-SHA256.
- Concurrency = 1 (Ký tuần tự từng hóa đơn).
- Giới hạn tối đa **50 hóa đơn / đợt ký**.
- Sắp xếp thứ tự ký tăng dần theo ngày xuất (`InvoiceDate ASC`).
- Không đưa private key/PIN lên web.

## Cấu trúc Giải pháp (Solution Structure)
- `Nacencomm.InvoiceManagement.slnx`: Solution file chính.
- `src/Nacencomm.InvoiceManagement/`: Ứng dụng ASP.NET Core MVC Quản lý Hóa đơn (`http://localhost:5000`).
- `src/Nacencomm.WinFormsSigner/`: Ứng dụng WinForms Signer HTTP Listener (`http://127.0.0.1:8765`).
- `tests/Nacencomm.InvoiceManagement.Tests/`: Bộ Unit Tests tự động xUnit (Pass 9/9).

## Cách Khởi chạy
```powershell
# Chạy Web App:
dotnet run --project src/Nacencomm.InvoiceManagement/Nacencomm.InvoiceManagement.csproj

# Chạy WinForms Signer Service:
dotnet run --project src/Nacencomm.WinFormsSigner/Nacencomm.WinFormsSigner.csproj

# Chạy Unit Tests:
dotnet test
```
