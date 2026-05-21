# MAES.Fiskal

[![CI/CD](https://github.com/MAES-Software/MAES.Fiskal/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/MAES-Software/MAES.Fiskal/actions/workflows/dotnet-ci.yml)
[![Contributors](https://img.shields.io/github/contributors/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/graphs/contributors)
[![Issues](https://img.shields.io/github/issues/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/issues)
[![NuGet](https://img.shields.io/nuget/v/MAES.Fiskal.svg)](https://www.nuget.org/packages/MAES.Fiskal/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MAES.Fiskal)](https://www.nuget.org/packages/MAES.Fiskal/)

## Overview

**MAES.Fiskal** is a .NET fiscalization library for Croatian invoicing. It supports invoice creation, ZKI generation, and fiscalization service submission.

## Features

- Create fiscal invoices with `RacunType`
- Generate ZKI signatures for fiscal documents
- Submit invoices and tips to the fiscalization service
- Support for certificate-based authentication

## Requirements

- .NET 8+
- Fiscalization WSDL version 2.6

## Installation

Install from NuGet:

```bash
dotnet add package MAES.Fiskal
```

Or clone the repository:

```bash
git clone https://github.com/MAES-Software/MAES.Fiskal.git
```

## Configuration

### Fiscalization endpoint

Use the test endpoint during development:

```csharp
string url = "https://cistest.apis-it.hr:8449/FiskalizacijaServiceTest";
```

Use the production endpoint in live environments:

```csharp
string url = "https://cis.porezna-uprava.hr:8449/FiskalizacijaService";
```

### Load X509 certificate

Load a certificate from a file:

```csharp
var certificate = new X509Certificate2("cert.p12", "password");
```

Load from raw bytes:

```csharp
var certificate = new X509Certificate2(bytes, "password");
```

## Usage

Add the library namespace:

```csharp
using MAES.Fiskal;
```

### Create and send an invoice

```csharp
var invoice = new RacunType
{
    BrRac = new BrojRacunaType
    {
        BrOznRac = "1",
        OznPosPr = "POSL1",
        OznNapUr = "1"
    },
    DatVrijeme = DateTime.Now.ToString("dd.MM.yyyyTHH:mm:ss"),
    IznosUkupno = "12.50",
    NakDost = false,
    Oib = "51560545524",
    OibOper = "51560545524",
    OznSlijed = OznakaSlijednostiType.N,
    Pdv = new[]
    {
        new PorezType
        {
            Stopa = "25.00",
            Osnovica = "10.00",
            Iznos = "2.50"
        }
    },
    USustPdv = true,
    NacinPlac = NacinPlacanjaType.G
};

var response = await invoice.SendAsync(certificate, url);

if (response.Greske.Length > 0)
{
    Console.WriteLine(string.Join(", ", response.Greske.Select(g => g.PorukaGreske)));
}

string jir = response.Jir;
```

> If you do not need consumption tax (`Pnp`), omit the property entirely. An empty `Pnp` collection may produce invalid XML.

### Create and send a tip invoice

```csharp
var tipData = new NapojnicaType
{
    iznosNapojnice = "1.00",
    nacinPlacanjaNapojnice = NacinPlacanjaType.G
};

var invoiceTip = invoice.ToRacunNapojnicaType(tipData);
var tipResponse = await invoiceTip.SendAsync(certificate, url);

if (tipResponse.Greske.Length > 0)
{
    Console.WriteLine(string.Join(", ", tipResponse.Greske.Select(g => g.PorukaGreske)));
}
```

### Generate ZKI

```csharp
string zki = invoice.ZKI(certificate);
```

## SSL validation

If you encounter certificate validation issues during development, you can temporarily disable SSL validation:

```csharp
ReferenceTypeExtensions.SslCertificateAuthentication = new()
{
    CertificateValidationMode = X509CertificateValidationMode.None,
    RevocationMode = X509RevocationMode.NoCheck
};
```

> **Warning:** Disabling SSL validation is not recommended for production. Use only for local testing or troubleshooting.

## Contribution

Contributions are welcome. Please open an issue or submit a pull request on GitHub.
