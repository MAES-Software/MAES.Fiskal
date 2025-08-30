# MAES.Fiskal

[![Contributors](https://img.shields.io/github/contributors/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/graphs/contributors)
[![Forks](https://img.shields.io/github/forks/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/network/members)
[![Stars](https://img.shields.io/github/stars/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/stargazers)
[![Issues](https://img.shields.io/github/issues/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/issues)
[![License](https://img.shields.io/github/license/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/LICENSE)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-Profile-0077B5?logo=linkedin&logoColor=white)](YOUR_LINKEDIN_URL_HERE)

**MAES.Fiskal** is a fiscalization tool for invoices developed in **C#** using **.NET 8**. It enables automatic generation and submission of fiscal data according to current regulations.

## Features
- ZKI Generation
- Invoice fiscalization
- Tip fiscalization
- Free and open source

## Installation
**Nuget:** https://www.nuget.org/packages/MAES.Fiskal

or

```bash
git clone https://github.com/MAES-Software/MAES.Fiskal.git
```

## Prerequirements

### Load X509Certificate2

1. From file (You can use relative path eg. "./cert.p12")
    ```csharp
    var certificate = new X509Certificate2("filename");
    ```
2. From some data stream with byte[] bytes
    ```csharp
    var certificate = new X509Certificate2(bytes);
    ```

You must also supply password for given certificate if it has one (by default certificates given from goverment are locked but they can be repackaged)
```csharp
var certificate = new X509Certificate2("filename", "password");
```

## Usage Example

### Define using MAES.Fiskal to get all classes for fiscalization
```csharp
using MAES.Fiskal;
```

### Create an invoice
```csharp
var invoice = new RacunType
{
	BrRac = new BrojRacunaType
	{
		BrOznRac = "1", // Invoice number (incremental for each receipt)
		OznPosPr = "POSL_1", // Workspace code
		OznNapUr = "1" // Cash reegister number
	},
	DatVrijeme = DateTime.Now.ToString("dd.MM.yyyyTHH:mm:ss"), // DateTime of invoice
	IznosUkupno = "12.50", // Total amount (must be format 0.00)
	NakDost = false,
	Oib = "51560545524", // Identification number of company
	OibOper = "51560545524", // Odentitfication numer of person operating POS
	OznSlijed = OznakaSlijednostiType.N,
    Pdv = [ // Taxes list
        new ()
        {
            Stopa = "25.00", // Tax percentage (must be format 0.00)
            Osnovica = "10.00", // Tax base (must be format 0.00)
            Iznos = "2.50" // Tax amount (must be format 0.00)
        }
    ],
    Pnp = [], // Fill tax on spending if nececary :S
    USustPdv = true, // Does company falls under tax obligation laws
    NacPlac = NacinPlacanjaType.G // Type of payment (G - Cash, K - Cards, etc...)
};
```

### Send invoice
```csharp
// Call to service
var res = await invoice.SendAsync(certificate, url);

// Check if there are errors
if(res.Greske.Length != 0) Console.WriteLine(res.Greske.Join(','));

// Get jir to store
string jir = res.Jir;
```

### Create an invoiceTipType from invoiceType
```csharp
RacunNapojnicaType invoiceTip = invoice.ToInvoiceTipAsnyc(new ()
{
    iznosNapojnice = "1.00", // Tip amount
    nacinPlacanjaNapojnice = acinPlacanjaType.G // Tip type of payment (G - Cash, K - Cards, etc...)
});
```

### Send invoice tip
```csharp
// Call to service
var res = await invoiceTip.SendAsync(certificate, url);

// Check if there are errors
if(res.Greske.Length != 0) Console.WriteLine(res.Greske.Join(','));
```

### Generate ZKI
```csharp
string zki = invoice.ZKI(certificate);
```
> Both invoice and invoiceTip have .ZKI(certificate) methods

### Disabling SSL Certificate Validation (Not Recommended)

If you encounter issues with SSL certificate validation, you can disable certificate checks as follows:

```csharp
ReferenceTypeExtensions.SslCertificateAuthentication = new()
{
    CertificateValidationMode = X509CertificateValidationMode.None,
    RevocationMode = X509RevocationMode.NoCheck
};
```

> **Warning:** Disabling SSL certificate validation is **not recommended** for production environments, as it reduces security and exposes your application to potential risks. Use this option only for testing or troubleshooting purposes.