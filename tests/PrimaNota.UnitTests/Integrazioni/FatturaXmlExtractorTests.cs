using System.Text;
using Org.BouncyCastle.Cms;
using PrimaNota.Infrastructure.Integrazioni;

namespace PrimaNota.UnitTests.Integrazioni;

public sealed class FatturaXmlExtractorTests
{
    private static readonly byte[] Xml =
        Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><FatturaElettronica>ok</FatturaElettronica>");

    [Fact]
    public void PlainXml_IsReturnedUnchanged()
    {
        var result = FatturaXmlExtractor.ExtractXml(Xml, "IT123_abc.xml");
        result.Should().Equal(Xml);
    }

    [Fact]
    public void PlainXml_WithBom_IsReturnedUnchanged()
    {
        var withBom = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Xml).ToArray();
        var result = FatturaXmlExtractor.ExtractXml(withBom, null);
        result.Should().Equal(withBom);
    }

    [Fact]
    public void Cades_P7m_Envelope_IsUnwrappedToInnerXml()
    {
        // Build a valid CMS SignedData with encapsulated content and no signers (no cert needed).
        var generator = new CmsSignedDataGenerator();
        var signed = generator.Generate(new CmsProcessableByteArray(Xml), encapsulate: true);
        var p7m = signed.GetEncoded();

        var result = FatturaXmlExtractor.ExtractXml(p7m, "IT123_abc.xml.p7m");

        result.Should().Equal(Xml);
    }

    [Fact]
    public void NonCms_NonXml_Bytes_AreReturnedUnchanged()
    {
        var garbage = new byte[] { 1, 2, 3, 4, 5 };
        var result = FatturaXmlExtractor.ExtractXml(garbage, "weird.bin");
        result.Should().Equal(garbage);
    }
}
