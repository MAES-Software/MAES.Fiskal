# MAES.Fiskal

[![CI/CD](https://github.com/MAES-Software/MAES.Fiskal/actions/workflows/main.yml/badge.svg)](https://github.com/MAES-Software/MAES.Fiskal/actions/workflows/main.yml)
[![Contributors](https://img.shields.io/github/contributors/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/graphs/contributors)
[![Issues](https://img.shields.io/github/issues/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/issues)
[![NuGet](https://img.shields.io/nuget/v/MAES.Fiskal.svg)](https://www.nuget.org/packages/MAES.Fiskal/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MAES.Fiskal)](https://www.nuget.org/packages/MAES.Fiskal/)

MAES.Fiskal is a .NET library (C#) that helps generate and submit fiscalized invoice data according to the Croatian fiscalization service.

Using Fiscalization WSDL version 2.6 (thanks maatko)

**Key points**
- Targets .NET Standard 2.0
    * .NET Framework 4.6+
    * .NET 5+
- Provides ZKI generation, invoice submission and invoice-tip submission helpers

## Features
- ZKI generation
- Invoice fiscalization
- Tip fiscalization

## Requirements
- .NET Standard 2.0

## Installation

Via NuGet:

```powershell
dotnet add package MAES.Fiskal
```

Or clone the repository:

```bash
git clone https://github.com/MAES-Software/MAES.Fiskal.git
```

## Configuration

Endpoints:

- TEST URL: `https://cistest.apis-it.hr:8449/FiskalizacijaServiceTest`
- PROD URL: `https://cis.porezna-uprava.hr:8449/FiskalizacijaService`

Loading an X509 certificate (examples):

```csharp
// From file
var certificate = new X509Certificate2("./cert.p12", "password");

// From raw bytes
var certificate = new X509Certificate2(bytes, "password");
```

Supply the certificate password when required by the key store provided by the issuing authority.

## Usage

Import the library:

```csharp
using MAES.Fiskal;
```

Create a simple invoice and submit it (example, types simplified for readability):

```csharp
var invoice = new RacunType
{
    BrRac = new BrojRacunaType { BrOznRac = "1", OznPosPr = "POSL1", OznNapUr = "1" },
    DatVrijeme = DateTime.Now.ToString("dd.MM.yyyyTHH:mm:ss"),
    IznosUkupno = "12.50",
    NakDost = false,
    Oib = "51560545524",
    OibOper = "51560545524",
    OznSlijed = OznakaSlijednostiType.N,
    Pdv = new[] { new PdvType { Stopa = "25.00", Osnovica = "10.00", Iznos = "2.50" } },
    Pnp = Array.Empty<PnpType>(),
    USustPdv = true,
    NacinPlac = NacinPlacanjaType.G
};

var response = await invoice.SendAsync(certificate, url);
if (response.Greske != null && response.Greske.Length > 0)
{
    Console.WriteLine(string.Join(',', response.Greske));
}
else
{
    string jir = response.Jir; // store this as required
}
```

Create and send a tip (napojnica) from an existing invoice:

```csharp
var tip = invoice.ToInvoiceTip(new NapojnicaParameters { IznosNapojnice = "1.00", NacinPlacanjaNapojnice = NacinPlacanjaType.G });
var tipRes = await tip.SendAsync(certificate, url);
if (tipRes.Greske != null && tipRes.Greske.Length > 0) Console.WriteLine(string.Join(',', tipRes.Greske));
```

Generate ZKI (example):

```csharp
string zki = invoice.ZKI(certificate);
```

## Security notes

- Do not disable SSL certificate validation in production. The library provides a way to override validation for local testing, but this weakens security.

If you need to disable validation for troubleshooting (NOT recommended):

```csharp
ReferenceTypeExtensions.SslCertificateAuthentication = new()
{
    CertificateValidationMode = X509CertificateValidationMode.None,
    RevocationMode = X509RevocationMode.NoCheck
};
```