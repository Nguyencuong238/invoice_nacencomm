# PHASE 04 – Signing Queue & Batch

## Mục tiêu

Ký tối đa 50 invoice với tải được kiểm soát.

## Pipeline

```text
Selected ≤ 50
 ↓ Validate
 ↓ Sort Date ASC
 ↓ Group Date
 ↓ Queue
 ↓ Worker(1)
```

## Worker

```csharp
foreach (var invoice in orderedInvoices)
    await SignOneAsync(invoice);
```

Không dùng `Task.WhenAll` cho signing.

## Per invoice

```text
Pending → GettingXml → Signing → Submitting → Success
```

Lỗi ở bất kỳ stage nào → `Failed`.

## Cancellation

Current request được hoàn tất nếu có thể; sau đó worker không lấy item mới. Không kill process/crypto operation.

## Retry

Retry transient timeout/network. Không retry mù certificate expired, token missing, invalid XML, duplicate hoặc business error.

## Acceptance

- [ ] 50 invoice.
- [ ] Concurrency 1.
- [ ] Date ASC.
- [ ] Partial success.
- [ ] Cancel.
- [ ] Retry policy.
