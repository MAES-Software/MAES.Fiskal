# MAES.Fiskal

[![Contributors](https://img.shields.io/github/contributors/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/graphs/contributors)
[![Forks](https://img.shields.io/github/forks/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/network/members)
[![Stars](https://img.shields.io/github/stars/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/stargazers)
[![Issues](https://img.shields.io/github/issues/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/issues)
[![License](https://img.shields.io/github/license/MAES-Software/MAES.Fiskal)](https://github.com/MAES-Software/MAES.Fiskal/blob/main/LICENSE)
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

## Usage Example

### Fiscalization of an Invoice

```csharp
using MAES.Fiskal;

var fiskalization = new Fiskalization();
var invoice = new RacunType
{
    // Fill invoice properties here
};

var result = fiskalization.Fiskaliziraj(invoice);

if (result.IsSuccess)
{
    Console.WriteLine("Fiscalization successful!");
    Console.WriteLine($"JIR: {result.JIR}");
}
else
{
    Console.WriteLine("Fiscalization failed:");
    Console.WriteLine(result.ErrorMessage);
}
```

### Using Extensions for RacunType

```csharp
using MAES.Fiskal.Extensions;

var invoice = new RacunType
{
    // Fill invoice properties here
};

// Example: Generate ZKI
string zki = invoice.ZKI(certificate);

// Example: Get total amount
decimal total = invoice.GetTotalAmount();
Console.WriteLine($"Total amount: {total}");
```

### Disabling SSL Certificate Validation (Not Recommended)

If you encounter issues with SSL certificate validation, you can disable certificate checks as follows:

```csharp
Fiscalization.SslCertificateAuthentication = new()
{
    CertificateValidationMode = X509CertificateValidationMode.None,
    RevocationMode = X509RevocationMode.NoCheck
};
```

> **Warning:** Disabling SSL certificate validation is **not recommended** for production environments, as it reduces security and exposes your application to potential risks. Use this option only for testing or troubleshooting purposes.

## License

<a href="https://github.com/MAES-Software/MAES.Fiskal">MAES.Fiskal</a> © 2025 by <a href="https://github.com/ImaJosBuggova">Roko Tomović</a> is licensed under <a href="https://creativecommons.org/licenses/by-sa/4.0/">Creative Commons Attribution-ShareAlike 4.0 International</a><img src="https://mirrors.creativecommons.org/presskit/icons/cc.svg" alt="" style="max-width: 1em;max-height:1em;margin-left: .2em;"><img src="https://mirrors.creativecommons.org/presskit/icons/by.svg" alt="" style="max-width: 1em;max-height:1em;margin-left: .2em;"><img src="https://mirrors.creativecommons.org/presskit/icons/sa.svg" alt="" style="max-width: 1em;max-height:1em;margin-left: .2em;">
