# PHASE 01 – UI quản lý hóa đơn

## Mục tiêu

Dựng giao diện theo màn hình hiện tại, tối ưu cho chọn và ký hàng loạt.

## Components

```text
InvoicePage
├── InvoiceFilter
├── InvoiceToolbar
│   ├── SelectAll
│   ├── SelectedCounter
│   └── SignButton
├── InvoiceDateGroup
│   └── InvoiceTable
└── SigningDialog
```

## Yêu cầu

- Filter theo các field backend thực sự hỗ trợ.
- Table có checkbox, ngày, số hóa đơn, MST, người mua, tổng tiền, trạng thái.
- Sort `InvoiceDate ASC`, sau đó `InvoiceNumber`.
- Group theo `InvoiceDate.Date`.
- Select All nhưng không vượt 50.
- Hiển thị `x/50`.
- Record không hợp lệ/đang xử lý/đã ký theo business status thì không được chọn.
- Confirm trước khi tạo signing job.

## Logic

```csharp
var ordered = invoices
    .OrderBy(x => x.InvoiceDate)
    .ThenBy(x => x.InvoiceNumber)
    .ToList();

var groups = ordered.GroupBy(x => x.InvoiceDate.Date);
```

## Acceptance

- [ ] Không reload khi tick.
- [ ] Không chọn quá 50.
- [ ] Sort/group đúng.
- [ ] Hiển thị selection count.
- [ ] Button ký đúng trạng thái.
