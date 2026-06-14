using Org.BouncyCastle.Cms;

namespace PrimaNota.Infrastructure.Integrazioni;

/// <summary>
/// Extracts the FatturaPA XML from a downloaded invoice file. Electronic invoices stored on SdI
/// channels are frequently CAdES-signed (<c>.p7m</c>): the inner signed content is unwrapped here;
/// plain XML is returned unchanged.
/// </summary>
public static class FatturaXmlExtractor
{
    /// <summary>Returns the XML payload, unwrapping a CAdES/PKCS#7 envelope when present.</summary>
    /// <param name="data">Raw downloaded bytes.</param>
    /// <param name="fileName">Original file name (used as a hint).</param>
    /// <returns>The FatturaPA XML bytes.</returns>
    public static byte[] ExtractXml(byte[] data, string? fileName)
    {
        ArgumentNullException.ThrowIfNull(data);

        var isSignedHint = fileName?.EndsWith(".p7m", StringComparison.OrdinalIgnoreCase) ?? false;
        if (!isSignedHint && LooksLikeXml(data))
        {
            return data;
        }

        try
        {
            var cms = new CmsSignedData(data);
            using var ms = new MemoryStream();
            cms.SignedContent.Write(ms);
            return ms.ToArray();
        }
        catch (CmsException)
        {
            // Not a CMS envelope after all — return the original bytes.
            return data;
        }
    }

    private static bool LooksLikeXml(byte[] data)
    {
        var i = 0;
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
        {
            i = 3; // skip UTF-8 BOM
        }

        while (i < data.Length && (data[i] == 0x20 || data[i] == 0x09 || data[i] == 0x0A || data[i] == 0x0D))
        {
            i++;
        }

        return i < data.Length && data[i] == (byte)'<';
    }
}
