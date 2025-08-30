using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.ServiceModel.Security;

namespace MAES.Fiskal;

/// <summary>
/// This is main class for fiscalization
/// </summary>
public static class Fiskalizacija
{
    static ZaglavljeType newZaglavlje => new()
    {
        DatumVrijeme = DateTime.Now.ToString("dd.MM.yyyyTHH:mm:ss"),
        IdPoruke = Guid.NewGuid().ToString()
    };

    /// <summary>
    /// Ova metoda pošalje račun na server porezne uprave
    /// </summary>
    /// <param name="invoice">Invoice to be reported</param>
    /// <param name="certificate">Fiscalization certificate</param>
    /// <param name="url">URL Endpoint</param>
    /// <returns>RacunOdgovor</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task<RacunOdgovor> SendInvoiceAsync(RacunType invoice, X509Certificate2 certificate, string url)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(certificate);

        if (string.IsNullOrEmpty(invoice.ZastKod)) invoice.ZastKod = invoice.ZKI(certificate);

        var request = new RacunZahtjev { Racun = invoice, Zaglavlje = newZaglavlje };

        sign(request, certificate);

        using var client = new FiskalizacijaPortTypeClient(new FiskalizacijaPortTypeClient.EndpointConfiguration(), url);

        // TODO: zapali!
        client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication = new()
        {
            CertificateValidationMode = X509CertificateValidationMode.None,
            RevocationMode = X509RevocationMode.NoCheck
        };

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
    public async static Task<napojnicaResponse> SendInvoiceTipAsync(RacunNapojnicaType invoiceTip, X509Certificate2 certificate, string url)
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
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication()
                {
                    CertificateValidationMode = X509CertificateValidationMode.None,
                    RevocationMode = X509RevocationMode.NoCheck
                }; // WTF JE OVO!
            res = await client.napojnicaAsync(request);
        }

        throwOnResponseErrors(res.NapojnicaOdgovor);

        return res;
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
        var certSerial = (X509IssuerSerial)keyInfoData.IssuerSerials[0]; // TODO: WTF JE OVO!
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
        if (response.Greske is not GreskaType[] greske || greske.Any()) return;
        throw new Exception($"Greška u fiskalizaciji: {string.Join("\n", greske.Select(x => $"{x.SifraGreske}: {x.PorukaGreske}"))}");
    }
}