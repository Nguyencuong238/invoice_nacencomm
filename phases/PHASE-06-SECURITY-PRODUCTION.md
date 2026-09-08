# PHASE 06 – Security & Production Hardening

## Local signer

- Chỉ bind `127.0.0.1`.
- Không expose trên LAN.

## Origin protection

Dùng Origin allowlist/CORS phù hợp; nếu threat model yêu cầu, thêm local handshake/session nonce. Không coi CORS là authentication duy nhất.

## XML

- Giới hạn payload.
- Reject malformed XML.
- Disable external entities/XXE.
- Không resolve external resources.
- Validate expected invoice XML.

## Secrets

Private key/PIN không xuất hiện trong browser, database, response hoặc logs.

## Existing handshake

`SignApp`, `VerifyApp`, `GetRandom` nếu có thể liên quan handshake/authentication; phải kiểm tra runtime/source trước khi thiết kế bỏ qua.

## Acceptance

- [ ] Loopback only.
- [ ] Origin protection.
- [ ] XML hardened.
- [ ] No secret leakage.
- [ ] Production logging.
