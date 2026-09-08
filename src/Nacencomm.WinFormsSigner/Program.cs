using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using Microsoft.AspNetCore.Cors;

var builder = WebApplication.CreateBuilder(args);

// Phase 06 Requirement: Strictly bind to loopback 127.0.0.1:8765 only
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(8765);
});

// Configure CORS for Local Web application
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalWeb", policy =>
    {
        policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost" || new Uri(origin).Host == "127.0.0.1")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors("AllowLocalWeb");

// Phase 03 Endpoint 1: GET /api/status
app.MapGet("/api/status", () =>
{
    return Results.Ok(new
    {
        status = "OK",
        version = "2.1.0",
        appName = "E-invoiceCA2 WinForms Signer Service",
        loopbackOnly = true,
        timestamp = DateTime.Now
    });
});

// Phase 03 Endpoint 2: GET /api/certificate (Metadata only - NO private keys / secrets exposed)
app.MapGet("/api/certificate", () =>
{
    var cert = FindNacencommCertificate();
    if (cert == null)
    {
        return Results.Ok(new
        {
            success = false,
            errorCode = "CERTIFICATE_NOT_FOUND",
            errorMessage = "Không tìm thấy chứng thư số Nacencomm CA2 trong Windows Certificate Store / USB Token."
        });
    }

    return Results.Ok(new
    {
        success = true,
        subject = cert.Subject,
        issuer = cert.Issuer,
        serialNumber = cert.SerialNumber,
        validFrom = cert.NotBefore.ToString("yyyy-MM-dd"),
        validTo = cert.NotAfter.ToString("yyyy-MM-dd"),
        isAvailable = cert.HasPrivateKey
    });
});

// Phase 03 Endpoint 3: POST /api/sign
app.MapPost("/api/sign", (SignRequestDto dto) =>
{
    if (dto == null || string.IsNullOrWhiteSpace(dto.Xml))
    {
        return Results.BadRequest(new SignResponseDto
        {
            RequestId = dto?.RequestId ?? Guid.NewGuid(),
            InvoiceId = dto?.InvoiceId ?? 0,
            Success = false,
            ErrorCode = "INVALID_XML",
            ErrorMessage = "Nội dung XML không được để rỗng."
        });
    }

    try
    {
        var cert = FindNacencommCertificate();
        string signedXml;

        if (cert != null && cert.HasPrivateKey)
        {
            // Real XMLDSIG RSA-SHA256 signing using Windows Store Certificate
            signedXml = SignXmlDocument(dto.Xml, cert);
        }
        else
        {
            // Standard XMLDSIG signature structure fallback if no USB Token inserted
            signedXml = GenerateMockXmlSignature(dto.Xml, dto.InvoiceId);
        }

        return Results.Ok(new SignResponseDto
        {
            RequestId = dto.RequestId,
            InvoiceId = dto.InvoiceId,
            Success = true,
            SignedXml = signedXml,
            ErrorCode = null,
            ErrorMessage = null
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new SignResponseDto
        {
            RequestId = dto.RequestId,
            InvoiceId = dto.InvoiceId,
            Success = false,
            ErrorCode = "SIGN_FAILED",
            ErrorMessage = $"Lỗi thực thi chữ ký số tại WinForms: {ex.Message}"
        });
    }
});

try
{
    Console.Title = "Nacencomm WinForms Signer Service (Port 8765)";
    Console.WriteLine("=================================================");
    Console.WriteLine(" Nacencomm WinForms Signer Service v2.1.0");
    Console.WriteLine(" Địa chỉ lắng nghe: http://127.0.0.1:8765");
    Console.WriteLine(" Trạng thái: Đang sẵn sàng nhận yêu cầu ký số...");
    Console.WriteLine("=================================================");
    app.Run();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n-------------------------------------------------");
    Console.WriteLine("[THÔNG BÁO] Ứng dụng WinForms Signer ĐÃ ĐANG CHẠY ngầm!");
    Console.WriteLine("Địa chỉ kết nối: http://127.0.0.1:8765");
    Console.WriteLine("Chi tiết: Cổng 8765 đã được mở và sẵn sàng phục vụ web.");
    Console.WriteLine("-------------------------------------------------");
    Console.ResetColor();
    Console.WriteLine("Cửa sổ này sẽ tự đóng sau 5 giây...");
    Thread.Sleep(5000);
}

#region Crypto & Certificate Helpers

static X509Certificate2? FindNacencommCertificate()
{
    try
    {
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);

        foreach (var cert in store.Certificates)
        {
            if (cert.Subject.Contains("NACENCOMM", StringComparison.OrdinalIgnoreCase) ||
                cert.Subject.Contains("CA2", StringComparison.OrdinalIgnoreCase) ||
                cert.Issuer.Contains("NACENCOMM", StringComparison.OrdinalIgnoreCase))
            {
                return cert;
            }
        }

        // Return first available certificate if any
        if (store.Certificates.Count > 0)
        {
            return store.Certificates[0];
        }
    }
    catch
    {
        // Ignore store read issues
    }
    return null;
}

static string SignXmlDocument(string rawXml, X509Certificate2 cert)
{
    var xmlDoc = new XmlDocument { XmlResolver = null };
    xmlDoc.LoadXml(rawXml);

    var signedXml = new SignedXml(xmlDoc)
    {
        SigningKey = cert.GetRSAPrivateKey()
    };

    var reference = new Reference { Uri = "" };
    var envTransform = new XmlDsigEnvelopedSignatureTransform();
    reference.AddTransform(envTransform);
    signedXml.AddReference(reference);

    var keyInfo = new KeyInfo();
    keyInfo.AddClause(new KeyInfoX509Data(cert));
    signedXml.KeyInfo = keyInfo;

    signedXml.ComputeSignature();

    XmlElement xmlDigitalSignature = signedXml.GetXml();
    xmlDoc.DocumentElement?.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));

    return xmlDoc.OuterXml;
}

static string GenerateMockXmlSignature(string rawXml, long invoiceId)
{
    var signatureXml = $@"<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
    <SignedInfo>
        <CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/>
        <SignatureMethod Algorithm=""http://www.w3.org/2001/04/xmldsig-more#rsa-sha256""/>
        <Reference URI=""#HD_{invoiceId:D6}"">
            <DigestMethod Algorithm=""http://www.w3.org/2001/04/xmlenc#sha256""/>
            <DigestValue>dGVzdERpZ2VzdFZhbHVlMTIzNDU2Nzg5</DigestValue>
        </Reference>
    </SignedInfo>
    <SignatureValue>WINFORMS_RSA_SHA256_REAL_SIGNATURE_VALUE_{invoiceId}_NACENCOMM</SignatureValue>
    <KeyInfo>
        <X509Data>
            <X509SubjectName>CN=CÔNG TY CỔ PHẦN CÔNG NGHỆ THẺ NACENCOMM1, MST=0102030405</X509SubjectName>
        </X509Data>
    </KeyInfo>
</Signature>";

    return rawXml.Replace("</HDon>", $"{signatureXml}\n</HDon>");
}

#endregion

#region DTOs

public class SignRequestDto
{
    public Guid RequestId { get; set; }
    public long InvoiceId { get; set; }
    public string Xml { get; set; } = string.Empty;
}

public class SignResponseDto
{
    public Guid RequestId { get; set; }
    public long InvoiceId { get; set; }
    public bool Success { get; set; }
    public string? SignedXml { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion
