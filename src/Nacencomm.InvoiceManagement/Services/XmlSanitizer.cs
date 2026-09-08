using System;
using System.IO;
using System.Xml;

namespace Nacencomm.InvoiceManagement.Services
{
    public static class XmlSanitizer
    {
        /// <summary>
        /// Validates and sanitizes raw invoice XML to prevent XXE, DTD expansion, and malformed structures (Phase 06 requirement)
        /// </summary>
        public static bool ValidateAndSanitizeXml(string rawXml, out string sanitizedXml, out string? errorMessage)
        {
            sanitizedXml = rawXml;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(rawXml))
            {
                errorMessage = "XML hóa đơn rỗng.";
                return false;
            }

            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersFromEntities = 1024,
                    IgnoreComments = false
                };

                using var stringReader = new StringReader(rawXml);
                using var xmlReader = XmlReader.Create(stringReader, settings);
                
                var xmlDoc = new XmlDocument { XmlResolver = null };
                xmlDoc.Load(xmlReader);

                // Verify expected root element
                if (xmlDoc.DocumentElement == null || !xmlDoc.DocumentElement.Name.Equals("HDon", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "Cấu trúc XML hóa đơn không đúng định dạng chuẩn (Root element phải là <HDon>).";
                    return false;
                }

                sanitizedXml = xmlDoc.OuterXml;
                return true;
            }
            catch (XmlException ex)
            {
                errorMessage = $"XML không hợp lệ hoặc bị lỗi cú pháp: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi xử lý XML: {ex.Message}";
                return false;
            }
        }
    }
}
