# PHASE 07 – Testing, Benchmark & Acceptance

## Unit

Selection: 0, 1, 49, 50, 51 (51 phải reject).

Sorting input `03/09, 01/09, 02/09, 01/09` → `01/09, 01/09, 02/09, 03/09`.

## Integration

- List/filter.
- Get XML.
- Sign XML.
- Submit/update flow.
- Signer status.
- Certificate.

## Signing cases

1. Valid XML.
2. Invalid XML.
3. Empty XML.
4. Unexpected root.
5. Certificate missing.
6. Token removed.
7. Certificate expired.
8. Sign timeout.
9. Backend timeout after signing.
10. Duplicate request.

## Batch

Test 10 và 50 invoice; verify concurrency = 1, order, results và UI responsiveness.

## Benchmark

Đo idle CPU, CPU khi signing 1/10/50, peak CPU, memory, average/p95 signing duration, token/driver errors.

Không dùng CPU polling để tự động tăng concurrency.

## Browser

Chrome, Edge, refresh, close tab, multiple tabs, signer stopped/restarted, backend unavailable, network loss.

## Security

Unauthorized origin, oversized XML, malformed XML, LAN access, DevTools inspection, log inspection.

## UAT

- [ ] UI approved.
- [ ] Backend unchanged.
- [ ] Signer detected.
- [ ] Certificate shown.
- [ ] 1 invoice signed.
- [ ] 10 invoices signed.
- [ ] 50 invoices signed.
- [ ] Date ordering verified.
- [ ] Failure handling approved.
- [ ] CPU benchmark approved.
- [ ] Security approved.
