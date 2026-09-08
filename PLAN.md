# Nacencomm – Technical Implementation Plan

## 1. Mục tiêu

Xây web .NET quản lý hóa đơn và ký số qua WinForms hiện có, **không sửa backend trong V1** nếu API hiện tại đáp ứng đủ nghiệp vụ.

## 2. Kiến trúc đề xuất

```text
Browser
  │ HTTPS
  ▼
ASP.NET Web
  ├──────────────► Existing Nacencomm Backend/API
  │
  │ localhost HTTP
  ▼
E-invoiceCA2 WinForms
  ▼
Existing SignXml / Certificate logic
  ▼
CSP/KSP/Token
```

### Quyết định
- Browser ↔ WinForms: local HTTP trên `127.0.0.1` ở V1; chưa cần WebSocket.
- Signing concurrency: **1** ở V1.
- Một job: tối đa **50 hóa đơn**.
- Queue sort `InvoiceDate ASC`, group theo ngày.
- Web không nhận private key/PIN/key material.
- Không dùng `Promise.all()` cho signing.
- Một invoice lỗi không làm mất các invoice đã thành công.

## 3. Luồng

```text
Select ≤ 50
  ↓
Validate
  ↓
Sort ngày ASC
  ↓
Group theo ngày
  ↓
Create Signing Job
  ↓
Worker(1)
  ↓
Get XML
  ↓
POST /api/sign → WinForms
  ↓
Receive signed XML/result
  ↓
Flow cập nhật backend hiện tại
  ↓
Success / Failed
```

## 4. WinForms API đề xuất

```text
GET  http://127.0.0.1:<port>/api/status
GET  http://127.0.0.1:<port>/api/certificate
POST http://127.0.0.1:<port>/api/sign
```

`/api/sign`:

```json
{
  "requestId": "uuid",
  "invoiceId": 123456,
  "xml": "<HDon>...</HDon>"
}
```

Response:

```json
{
  "requestId": "uuid",
  "invoiceId": 123456,
  "success": true,
  "signedXml": "<HDon>...</HDon>",
  "errorCode": null,
  "errorMessage": null
}
```

Đây là **proposed contract**, cần map với code WinForms thật.

## 5. Phân tích WinForms đã có

Static analysis trước đó cho thấy executable là WinForms .NET Framework và có các thành phần liên quan `X509Certificate2`, `X509Store`, `RSACryptoServiceProvider`, `CspParameters`, `SignedXml`, `SignXml`, `GetXML`, `Capnhatxmlhd_daky`, `GetSignedText`, `SignApp`, `VerifyApp`, `CheckCTS` và SOAP proxy `WSHoadonCA2.asmx`.

Có dấu hiệu RSA/SHA-256 XML signing và certificate/provider usage. **Chưa kết luận hệ thống chỉ dùng CSP**; provider thực tế có thể phụ thuộc certificate/token/driver/KSP/CSP. Không thay đổi logic crypto hiện tại.

## 6. Code structure

```text
src/
├── Controllers/
│   ├── InvoiceController.cs
│   └── SigningController.cs
├── Services/
│   ├── InvoiceService.cs
│   ├── SigningService.cs
│   └── WinFormsClient.cs
├── Models/
│   ├── Invoice.cs
│   ├── InvoiceFilter.cs
│   ├── SigningRequest.cs
│   ├── SigningResult.cs
│   └── CertificateInfo.cs
├── ViewModels/
└── wwwroot/
```

Controller không tự quản lý queue/WinForms HTTP.

## 7. Các phase

1. UI quản lý hóa đơn.
2. Existing backend/API integration.
3. WinForms local signer POC.
4. Signing queue/batch.
5. Progress/error/reliability.
6. Security/production hardening.
7. Testing/benchmark/UAT.

## 8. Definition of Done

- UI list/filter/pagination.
- Sort và group ngày ASC.
- Select All + max 50.
- Detect WinForms.
- Lấy certificate metadata.
- Ký được 1 XML.
- Batch 50 tuần tự.
- Progress + per-invoice result.
- Retry/cancel có kiểm soát.
- Loopback only và không lộ secrets.
- Benchmark CPU/memory.
