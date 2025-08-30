using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.ServiceModel.Security;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MAES.Fiskal;

/// <summary>
/// This is a static class that adds all methods to wrap service in c#
/// </summary>
public static class ReferenceTypeExtensions
{
    static ZaglavljeType newZaglavlje => new()
    {
        DatumVrijeme = DateTime.Now.ToString("dd.MM.yyyyTHH:mm:ss"),
        IdPoruke = Guid.NewGuid().ToString()
    };

    static readonly X509ServiceCertificateAuthentication SslCertificateAuthentification = new();

    /// <summary>
    /// This method signs and sends invoice to specified url
    /// </summary>
    /// <param name="invoice">Invoice to be reported</param>
    /// <param name="certificate">Fiscalization certificate</param>
    /// <param name="url">URL Endpoint</param>
    /// <returns>RacunOdgovor</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task<RacunOdgovor> SendAsync(this RacunType invoice, X509Certificate2 certificate, string url)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(certificate);

        if (string.IsNullOrEmpty(invoice.ZastKod)) invoice.ZastKod = invoice.ZKI(certificate);

        var request = new RacunZahtjev { Racun = invoice, Zaglavlje = newZaglavlje };

        sign(request, certificate);

        using var client = new FiskalizacijaPortTypeClient(new FiskalizacijaPortTypeClient.EndpointConfiguration(), url);

        client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication = SslCertificateAuthentification;

        var res = await client.racuniAsync(request) ?? new();

        throwOnResponseErrors(res.RacunOdgovor);

        return res.RacunOdgovor;
    }

    /// <summary>
    /// This method reports tip for existing invoice. To get this type you can call from any RacunType you created .ToRacunNapojnicaType(X509Certficate2 cert) to converti it to RacunNapojnicaType
    /// </summary>
    /// <param name="invoiceTip">RacunNapojnica type to report</param>
    /// <param name="certificate">Fiskalni certifikat</param>
    /// <param name="url">Url adresa za slanje</param>
    /// <returns>napojnicaResponse</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async static Task<napojnicaResponse> SendAsync(this RacunNapojnicaType invoiceTip, X509Certificate2 certificate, string url)
    {
        ArgumentNullException.ThrowIfNull(invoiceTip);
        ArgumentNullException.ThrowIfNull(certificate);

        if (string.IsNullOrEmpty(invoiceTip.ZastKod)) invoiceTip.ZastKod = invoiceTip.ZKI(certificate);

        var request = new NapojnicaZahtjev
        {
            Racun = invoiceTip,
            Zaglavlje = newZaglavlje
        };

        sign(request, certificate);

        napojnicaResponse res;
        using (var client = new FiskalizacijaPortTypeClient(new FiskalizacijaPortTypeClient.EndpointConfiguration(), url))
        {
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication = SslCertificateAuthentification;
            res = await client.napojnicaAsync(request);
        }

        throwOnResponseErrors(res.NapojnicaOdgovor);

        return res;
    }
    
    /// <summary>
    /// This method converts object of type RacunType to RacunNapojnicaType with supplied parameters.
    /// </summary>
    /// <param name="invoice">RacunType of original Invoice</param>
    /// <param name="tip">tip to be added to invoice</param>
    /// <returns>RacunNapojnicaType</returns>
    public static RacunNapojnicaType ToRacunNapojnicaType(this RacunType invoice, NapojnicaType tip)
    {
        RacunNapojnicaType result = new();
        var sourceProps = invoice.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var targetProps = typeof(RacunNapojnicaType).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var sProp in sourceProps)
        {
            var tProp = targetProps.FirstOrDefault(p => p.Name == sProp.Name && p.PropertyType == sProp.PropertyType);
            if (tProp != null && tProp.CanWrite)
            {
                var value = sProp.GetValue(invoice, null);
                tProp.SetValue(result, value, null);
            }
        }

        result.Napojnica = tip;

        return result;
    }

    /// <summary>
    /// Generate ZKI code for invoice.
    /// </summary>
    /// <param name="invoice">Invoice that needs ZKI</param>
    /// <param name="certificate">Certificate for signing</param>
    /// <returns>ZKI string (Maybe GUID idk...)</returns>
    public static string ZKI(this RacunType invoice, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var b = Encoding.ASCII.GetBytes(invoice.Oib + invoice.DatVrijeme + invoice.BrRac.BrOznRac + invoice.BrRac.OznPosPr + invoice.BrRac.OznNapUr + invoice.IznosUkupno);
        var signData = (certificate.GetRSAPrivateKey()?.SignData(b, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1)) ?? throw new Exception("Invalid cerrtificate. No RSA Private key.");
        return new string([.. MD5.HashData(signData).SelectMany(x => x.ToString("x2"))]);
    }

    /// <summary>
    /// Generate ZKI code for invoice.
    /// </summary>
    /// <param name="invoiceTip">Invoice that needs ZKI</param>
    /// <param name="certificate">Certificate for signing</param>
    /// <returns>ZKI string (Maybe GUID idk...)</returns>
    public static string ZKI(this RacunNapojnicaType invoiceTip, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var b = Encoding.ASCII.GetBytes(invoiceTip.Oib + invoiceTip.DatVrijeme + invoiceTip.BrRac.BrOznRac + invoiceTip.BrRac.OznPosPr + invoiceTip.BrRac.OznNapUr + invoiceTip.IznosUkupno);
        var signData = (certificate.GetRSAPrivateKey()?.SignData(b, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1)) ?? throw new Exception("Invalid cerrtificate. No RSA Private key.");
        return new string([.. MD5.HashData(signData).SelectMany(x => x.ToString("x2"))]);
    }
    
    static void sign(dynamic request, X509Certificate2 certificate)
    {
        request.Id = request.GetType().Name;

        using var ms = new MemoryStream();
        var root = new XmlRootAttribute { Namespace = "http://www.apis-it.hr/fin/2012/types/f73", IsNullable = false };
        var ser = new XmlSerializer(request.GetType(), root);
        ser.Serialize(ms, request);

        var doc = new XmlDocument();
        doc.LoadXml(Encoding.UTF8.GetString(ms.ToArray()));
        var xml = new SignedXml(doc)
        {
            SigningKey = certificate.GetRSAPrivateKey(),
            SignedInfo = { CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl }
        };

        var keyInfo = new KeyInfo();
        var keyInfoData = new KeyInfoX509Data();
        keyInfoData.AddCertificate(certificate);
        keyInfoData.AddIssuerSerial(certificate.Issuer, certificate.GetSerialNumberString());
        keyInfo.AddClause(keyInfoData);
        xml.KeyInfo = keyInfo;

        var transforms = new Transform[]
        {
            new XmlDsigEnvelopedSignatureTransform(false),
            new XmlDsigExcC14NTransform(false)
        };

        Reference reference = new("#" + request.Id);
        foreach (var x in transforms)
            reference.AddTransform(x);
        xml.AddReference(reference);

        xml.ComputeSignature();

        var s = xml.Signature;

        if (keyInfoData.IssuerSerials[0] is not X509IssuerSerial serial) throw new Exception("There is no issuer serial in supplied certificate");

        var certSerial = serial;
        request.Signature = new SignatureType
        {
            SignedInfo = new SignedInfoType
            {
                CanonicalizationMethod = new CanonicalizationMethodType { Algorithm = s.SignedInfo.CanonicalizationMethod },
                SignatureMethod = new SignatureMethodType { Algorithm = s.SignedInfo.SignatureMethod },
                Reference =
                    (from x in s.SignedInfo.References.OfType<Reference>()
                     select new ReferenceType
                     {
                         URI = x.Uri,
                         Transforms =
                             (from t in transforms
                              select new TransformType { Algorithm = t.Algorithm }).ToArray(),
                         DigestMethod = new DigestMethodType { Algorithm = x.DigestMethod },
                         DigestValue = x.DigestValue
                     }).ToArray()
            },
            SignatureValue = new SignatureValueType { Value = s.SignatureValue },
            KeyInfo = new KeyInfoType
            {
                ItemsElementName = [ItemsChoiceType2.X509Data],
                Items =
                [
                    new X509DataType
                    {
                        ItemsElementName =
                        [
                            ItemsChoiceType.X509IssuerSerial,
                            ItemsChoiceType.X509Certificate
                        ],
                        Items =
                        [
                            new X509IssuerSerialType
                            {
                                X509IssuerName = certSerial.IssuerName,
                                X509SerialNumber = certSerial.SerialNumber
                            },
                            certificate.RawData
                        ]
                    }
                ]
            }
        };
    }

    static void throwOnResponseErrors(dynamic response)
    {
        if (response.Greske is not GreskaType[] greske || greske.Length != 0) return;
        throw new Exception($"Greška u fiskalizaciji: {string.Join("\n", greske.Select(x => $"{x.SifraGreske}: {x.PorukaGreske}"))}");
    }
}