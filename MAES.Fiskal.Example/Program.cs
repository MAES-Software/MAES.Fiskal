using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using MAES.Fiskal;

var invoice = new RacunType
{
	BrRac = new BrojRacunaType
	{
		BrOznRac = "1", // Invoice number (incremental for each receipt)
		OznPosPr = "POSL1", // Workspace code
		OznNapUr = "1" // Cash reegister number
	},
	DatVrijeme = DateTime.Now.ToString("dd.MM.yyyyTHH:mm:ss"), // DateTime of invoice
	IznosUkupno = "12.50", // Total amount (must be format 0.00)
	NakDost = false,
	Oib = "18945722090", // Identification number of company
	OibOper = "18945722090", // Odentitfication numer of person operating POS
	OznSlijed = OznakaSlijednostiType.N,
	Pdv = new PorezType[]
	{ // Taxes list
		new PorezType
		{
			Stopa = "25.00", // Tax percentage (must be format 0.00)
			Osnovica = "10.00", // Tax base (must be format 0.00)
			Iznos = "2.50" // Tax amount (must be format 0.00)
		}
	},
    USustPdv = true, // Does company falls under tax obligation laws
    NacinPlac = NacinPlacanjaType.G // Type of payment (G - Cash, K - Cards, etc...)
};

// !! Disable certificate validation (for testing purposes only, do not use in production) !!
ReferenceTypeExtensions.SslCertificateAuthentification = new()
{
    CertificateValidationMode = X509CertificateValidationMode.None,
    RevocationMode = X509RevocationMode.NoCheck
};

// Load certificate from file (ensure you have a valid .p12 in execution directory)
X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile("cert.p12", "");

// Send invoice to fiskalization api (ensure you have access to test api and correct url)
var res = await invoice.SendAsync(certificate, "https://cistest.apis-it.hr:8449/FiskalizacijaServiceTest");

// Check for errors and print JIR
if(res.Greske != null && res.Greske.Length != 0) Console.WriteLine(string.Join(", ", res.Greske.Select(g => g.PorukaGreske)));
else Console.WriteLine($"JIR: {res.Jir}");