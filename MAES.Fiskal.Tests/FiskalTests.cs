namespace MAES.Fiskal.Tests;

using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;

public class FiskalTests
{
    X509Certificate2 certificate => X509CertificateLoader.LoadPkcs12FromFile("cert.p12", "");

    string testUrl => "https://cistest.apis-it.hr:8449/FiskalizacijaServiceTest";

    RacunType invoice = new ()
    {
        BrRac = new BrojRacunaType
        {
            BrOznRac = "1",
            OznPosPr = "POSL1",
            OznNapUr = "1"
        },
        DatVrijeme = DateTime.Now.ToString("dd.MM.yyyyTHH:mm:ss"),
        IznosUkupno = "100.00",
        Oib = "18945722090",
        OibOper = "18945722090",
        OznSlijed = OznakaSlijednostiType.N,
        Pdv = [ new() { Stopa = "25.00", Osnovica = "80.00", Iznos = "20.00" } ],
        USustPdv = true,
        NacinPlac = NacinPlacanjaType.G
    };

    [Fact]
    public void GenerateZKI()
    {
        var zki = invoice.ZKI(certificate);

        Assert.NotNull(zki);
        Assert.NotEmpty(zki);
        Assert.True(zki.Length > 20);
    }

    [Fact]
    public async Task SendInvoice()
    {
        ReferenceTypeExtensions.SslCertificateAuthentification = new()
        {
            CertificateValidationMode = X509CertificateValidationMode.None,
            RevocationMode = X509RevocationMode.NoCheck
        };

        var response = await invoice.SendAsync(certificate, testUrl);

        Assert.NotNull(response);
        Assert.NotEmpty(response.Jir);
    }

    // This test cannot succeed because test api doesnt have invoices 
    // [Fact]
    // public async Task FiscalizeInvoiceTip_WithCertificate_ShouldReturnJir()
    // {
    //     ReferenceTypeExtensions.SslCertificateAuthentification = new()
    //     {
    //         CertificateValidationMode = X509CertificateValidationMode.None,
    //         RevocationMode = X509RevocationMode.NoCheck
    //     };

    //     var napojnicaData = new NapojnicaType
    //     {
    //         iznosNapojnice = "10.00",
    //         nacinPlacanjaNapojnice = NacinPlacanjaType.G
    //     };

    //     var response = await invoice.ToRacunNapojnicaType(napojnicaData).SendAsync(certificate, testUrl);

    //     Assert.NotNull(response);

    //     if(response.NapojnicaOdgovor.Greske != null && response.NapojnicaOdgovor.Greske.Length > 0) throw new Exception($"{string.Join(", ", response.NapojnicaOdgovor.Greske.Select(g => g.PorukaGreske))}");
    // }
}