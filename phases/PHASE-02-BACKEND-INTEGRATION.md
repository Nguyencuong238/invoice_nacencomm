# PHASE 02 – Existing Backend/API Integration

## Mục tiêu

Dùng API hiện tại, không sửa nghiệp vụ backend.

## Adapter

```csharp
public interface IInvoiceService
{
    Task<IReadOnlyList<Invoice>> GetInvoicesAsync(InvoiceFilter filter);
    Task<string> GetXmlAsync(long invoiceId);
}
```

Không bind UI trực tiếp vào backend DTO; map qua domain model riêng.

## XML

Khuyến nghị chỉ lấy XML khi bắt đầu signing, không tải XML của toàn bộ danh sách.

## Flow cần xác nhận

### A
```text
Web → GetXML → WinForms SignXml → WinForms cập nhật backend
```

### B
```text
Web → GetXML → WinForms SignXml → Web submit signed XML
```

Các method đã thấy như `GetXML`, `SignXml`, `Capnhatxmlhd_daky` khiến Flow A hoặc biến thể của A đáng nghiêng về hơn, nhưng phải trace code/runtime trước khi chốt.

## Idempotency

Mỗi item dùng `jobId + requestId + invoiceId`. Không retry mù sau timeout nếu chưa biết trạng thái server.

## Acceptance

- [ ] List/filter hoạt động.
- [ ] XML lấy đúng invoice.
- [ ] Error mapping.
- [ ] Backend không regression.
