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
    // Fill invoice properties here
};
```

### Create an invoiceTipType from invoiceType
```csharp
RacunNapojnicaType invoiceTip = invoice.ToInvoiceTipAsnyc(new ()
{
    // Fill tip properties here
});
```

### Send invoice
```csharp
Fiscalization.SendInvoiceAsync(invoice, certificate);
```

### Send invoiceTip
```csharp
Fiscalization.SendInvoiceAsync(invoiceTip, certificate);
```

### Generate ZKI

```csharp
string zki = invoice.ZKI(certificate);
```

> Both invoice and invoiceTip have .ZKI(certificate) methods

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
