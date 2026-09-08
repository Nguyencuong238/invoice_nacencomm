/**
 * WinForms Local Signer API Client
 * Connects to local E-invoiceCA2 WinForms app at http://127.0.0.1:8765
 */
const WinFormsSigner = (function () {
    let signerPort = 8765;
    let devMockMode = false; // Fallback to mock simulation if WinForms app is not running

    function getBaseUrl() {
        return `http://127.0.0.1:${signerPort}`;
    }

    return {
        setPort: function (port) {
            signerPort = port;
        },
        enableDevMock: function (enable) {
            devMockMode = enable;
        },
        isDevMockEnabled: function () {
            return devMockMode;
        },

        /**
         * Check if WinForms local signer is running
         */
        checkStatus: async function () {
            if (devMockMode) {
                return { isRunning: true, version: "2.1.0-mock", mode: "MOCK_SIMULATION" };
            }
            try {
                const controller = new AbortController();
                const timeoutId = setTimeout(() => controller.abort(), 2000);

                const response = await fetch(`${getBaseUrl()}/api/status`, {
                    method: 'GET',
                    headers: { 'Accept': 'application/json' },
                    signal: controller.signal
                });
                clearTimeout(timeoutId);

                if (response.ok) {
                    const data = await response.json();
                    return { isRunning: true, ...data };
                }
            } catch (err) {
                console.warn("WinForms signer local HTTP status check failed:", err.message);
            }
            return { isRunning: false, errorCode: "SIGNER_NOT_RUNNING", errorMessage: "Không kết nối được ứng dụng ký WinForms (127.0.0.1)" };
        },

        /**
         * Get Digital Certificate Info from WinForms Signer
         */
        getCertificate: async function () {
            if (devMockMode) {
                return {
                    success: true,
                    subject: "CN=CÔNG TY CỔ PHẦN CÔNG NGHỆ THẺ NACENCOMM1, MST=0102030405",
                    issuer: "CN=NACENCOMM CA2",
                    serialNumber: "54039812749A8F002",
                    validFrom: "2024-01-01",
                    validTo: "2027-12-31",
                    isAvailable: true
                };
            }
            try {
                const response = await fetch(`${getBaseUrl()}/api/certificate`, {
                    method: 'GET',
                    headers: { 'Accept': 'application/json' }
                });
                if (response.ok) {
                    return await response.json();
                }
            } catch (err) {
                console.error("Failed to get certificate from WinForms:", err);
            }
            return { success: false, errorCode: "CERTIFICATE_NOT_FOUND", errorMessage: "Không lấy được thông tin chứng ký số từ Token/USB CA2." };
        },

        /**
         * Send XML to WinForms Signer for signing
         * @param {string} requestId UUID
         * @param {number} invoiceId 
         * @param {string} rawXml 
         */
        signXml: async function (requestId, invoiceId, rawXml) {
            if (devMockMode) {
                // Simulate 300ms crypto signing duration
                await new Promise(r => setTimeout(r, 350));
                
                // Simulate rare token removal error if requested by mock
                const signedXml = rawXml.replace('</HDon>', `
    <Signature xmlns="http://www.w3.org/2000/09/xmldsig#">
        <SignedInfo>
            <CanonicalizationMethod Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315"/>
            <SignatureMethod Algorithm="http://www.w3.org/2001/04/xmldsig-more#rsa-sha256"/>
            <Reference URI="#HD_${invoiceId.toString().padStart(6, '0')}">
                <DigestMethod Algorithm="http://www.w3.org/2001/04/xmlenc#sha256"/>
                <DigestValue>dGVzdERpZ2VzdFZhbHVlMTIzNDU2Nzg5</DigestValue>
            </Reference>
        </SignedInfo>
        <SignatureValue>MOCK_RSA_SHA256_SIGNATURE_VALUE_${invoiceId}_NACENCOMM</SignatureValue>
        <KeyInfo>
            <X509Data>
                <X509SubjectName>CN=CÔNG TY CỔ PHẦN CÔNG NGHỆ THẺ NACENCOMM1, MST=0102030405</X509SubjectName>
            </X509Data>
        </KeyInfo>
    </Signature>
</HDon>`);
                return {
                    requestId: requestId,
                    invoiceId: invoiceId,
                    success: true,
                    signedXml: signedXml,
                    errorCode: null,
                    errorMessage: null
                };
            }

            try {
                const response = await fetch(`${getBaseUrl()}/api/sign`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json'
                    },
                    body: JSON.stringify({
                        requestId: requestId,
                        invoiceId: invoiceId,
                        xml: rawXml
                    })
                });

                if (response.ok) {
                    return await response.json();
                } else {
                    const errData = await response.json().catch(() => ({}));
                    return {
                        requestId: requestId,
                        invoiceId: invoiceId,
                        success: false,
                        signedXml: null,
                        errorCode: errData.errorCode || "SIGN_FAILED",
                        errorMessage: errData.errorMessage || `Lỗi ký số HTTP ${response.status}`
                    };
                }
            } catch (err) {
                return {
                    requestId: requestId,
                    invoiceId: invoiceId,
                    success: false,
                    signedXml: null,
                    errorCode: "CONNECT_FAILED",
                    errorMessage: `Không thể kết nối WinForms Local Signer: ${err.message}`
                };
            }
        }
    };
})();
