# PHASE 03 – WinForms Local Signer

## Mục tiêu

Cho browser gọi được logic ký hiện tại mà không viết lại crypto.

## Static analysis đã có

Có dấu hiệu của WinForms/.NET Framework, X509 certificate store, `RSACryptoServiceProvider`, `CspParameters`, `SignedXml`, XMLDSIG và các method `SignXml`, `GetXML`, `Capnhatxmlhd_daky`, `SignApp`, `VerifyApp`, `CheckCTS`.

**CSP/KSP chưa được chốt chỉ bằng tên API.** Phải kiểm tra certificate provider/token thực tế.

## Kiến trúc WinForms

```text
Form
 ├── LocalHttpServer
 └── SigningService
      ├── CertificateProvider
      └── Existing SignXml()
```

## Endpoints

```text
GET /api/status
GET /api/certificate
POST /api/sign
```

Bind `127.0.0.1`, không bind `0.0.0.0`.

## Certificate

Chỉ trả metadata: subject, issuer, serial, validity, availability. Không trả private key/PIN.

## Signing

Reuse exact existing signing profile: signature method, digest, canonicalization, reference, transforms, KeyInfo và các field business-specific.

## Acceptance

- [ ] Status.
- [ ] Certificate metadata.
- [ ] Sign 1 XML.
- [ ] Verify signature.
- [ ] No secret leakage.
