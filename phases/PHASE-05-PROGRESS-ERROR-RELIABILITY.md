# PHASE 05 – Progress, Error & Reliability

## Job model

```csharp
public class SigningJob
{
    public Guid JobId { get; set; }
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
}
```

## UI

```text
Đang ký hóa đơn
██████████████░░░░░ 32/50
Thành công: 31 | Lỗi: 1 | Đang xử lý: 1 | Chờ: 17
```

## Error codes

```text
SIGNER_NOT_RUNNING
CERTIFICATE_NOT_FOUND
CERTIFICATE_EXPIRED
TOKEN_NOT_AVAILABLE
SIGN_FAILED
INVALID_XML
BACKEND_TIMEOUT
BACKEND_UNAVAILABLE
AUTH_FAILED
BUSINESS_ERROR
ALREADY_PROCESSED
MAX_LIMIT
CANCELLED
```

## Logging

Log `jobId`, `requestId`, `invoiceId`, stage, duration, result, errorCode. Không log private key/PIN/raw XML nếu không cần.

## Timeout

Tách timeout cho GetXML, Sign và Submit.

## Refresh

V1 có thể chấp nhận mất progress UI sau refresh nếu business không yêu cầu resume. Nếu cần resume phải có durable job state.

## Acceptance

- [ ] Per-invoice result.
- [ ] Progress.
- [ ] Clear errors.
- [ ] Timeout.
- [ ] Partial success.
- [ ] UI không freeze.
