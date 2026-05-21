using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.ServiceModel.Security;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Numerics;

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

    /// <summary>
    /// Authentification Method. To turn off (not recommended):
    /// new()
    /// {
    ///     CertificateValidationMode = X509CertificateValidationMode.None,
    ///     RevocationMode = X509RevocationMode.NoCheck
    /// }
    /// </summary>
    public static X509ServiceCertificateAuthentication SslCertificateAuthentification = new();

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
        if (invoice is null) throw new ArgumentNullException(nameof(invoice));
        if (certificate is null) throw new ArgumentNullException(nameof(certificate));

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
        if (invoiceTip is null) throw new ArgumentNullException(nameof(invoiceTip));
        if (certificate is null) throw new ArgumentNullException(nameof(certificate));

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
        if (certificate is null) throw new ArgumentNullException(nameof(certificate));

        var b = Encoding.ASCII.GetBytes(invoice.Oib + invoice.DatVrijeme + invoice.BrRac.BrOznRac + invoice.BrRac.OznPosPr + invoice.BrRac.OznNapUr + invoice.IznosUkupno);
        var signData = (certificate.GetRSAPrivateKey()?.SignData(b, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1)) ?? throw new Exception("Invalid cerrtificate. No RSA Private key.");
        var hash = MD5.Create().ComputeHash(signData);
        return string.Concat(hash.Select(x => x.ToString("x2")));
    }

    /// <summary>
    /// Generate ZKI code for invoice.
    /// </summary>
    /// <param name="invoiceTip">Invoice that needs ZKI</param>
    /// <param name="certificate">Certificate for signing</param>
    /// <returns>ZKI string (Maybe GUID idk...)</returns>
    public static string ZKI(this RacunNapojnicaType invoiceTip, X509Certificate2 certificate)
    {
        if (certificate is null) throw new ArgumentNullException(nameof(certificate));

        var b = Encoding.ASCII.GetBytes(invoiceTip.Oib + invoiceTip.DatVrijeme + invoiceTip.BrRac.BrOznRac + invoiceTip.BrRac.OznPosPr + invoiceTip.BrRac.OznNapUr + invoiceTip.IznosUkupno);
        var signData = (certificate.GetRSAPrivateKey()?.SignData(b, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1)) ?? throw new Exception("Invalid cerrtificate. No RSA Private key.");
        var hash2 = MD5.Create().ComputeHash(signData);
        return string.Concat(hash2.Select(x => x.ToString("x2")));
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

        // Use certificate values directly (avoids depending on internal X509IssuerSerial type)
        var certIssuerName = certificate.Issuer;
        // Convert hex serial to decimal string for XML schema compatibility
        string HexToDecimalString(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return string.Empty;
            var bytes = Enumerable.Range(0, hex.Length)
                .Where(i => i % 2 == 0)
                .Select(i => Convert.ToByte(hex.Substring(i, 2), 16))
                .ToArray();
            var little = bytes.Reverse().ToArray();
            var bigInt = new BigInteger(little.Concat(new byte[] { 0 }).ToArray());
            return bigInt.ToString();
        }

        var certSerialNumber = HexToDecimalString(certificate.GetSerialNumberString());

        request.Signature = new SignatureType
        {
            SignedInfo = new SignedInfoType
            {
                CanonicalizationMethod = new CanonicalizationMethodType { Algorithm = s.SignedInfo.CanonicalizationMethod },
                SignatureMethod = new SignatureMethodType { Algorithm = s.SignedInfo.SignatureMethod },
                Reference = (from x in s.SignedInfo.References.OfType<Reference>()
                             select new ReferenceType
                             {
                                 URI = x.Uri,
                                 Transforms = (from t in transforms
                                               select new TransformType { Algorithm = t.Algorithm }).ToArray(),
                                 DigestMethod = new DigestMethodType { Algorithm = x.DigestMethod },
                                 DigestValue = x.DigestValue
                             }).ToArray()
            },
            SignatureValue = new SignatureValueType { Value = s.SignatureValue },
            KeyInfo = new KeyInfoType
            {
                ItemsElementName = new ItemsChoiceType2[] { ItemsChoiceType2.X509Data },
                Items = new object[]
                {
                    new X509DataType
                    {
                        ItemsElementName = new ItemsChoiceType[] { ItemsChoiceType.X509IssuerSerial, ItemsChoiceType.X509Certificate },
                        Items = new object[]
                        {
                            new X509IssuerSerialType
                            {
                                X509IssuerName = certIssuerName,
                                X509SerialNumber = certSerialNumber
                            },
                            certificate.RawData
                        }
                    }
                }
            }
        };
    }

    static void throwOnResponseErrors(dynamic response)
    {
        if (response.Greske is not GreskaType[] greske || greske.Length == 0) return;
        throw new Exception($"Greška u fiskalizaciji: {string.Join("\n", greske.Select(x => $"{x.SifraGreske}: {x.PorukaGreske}"))}");
    }
}