using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MAES.Fiskal;

/// <summary>
/// This is a static class that adds RacunType helper methods.
/// </summary>
public static class RacunTypeExtensions
{
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
        return new string(MD5.HashData(signData).SelectMany(x => x.ToString("x2")).ToArray());
    }

    /// <summary>
    /// Generate ZKI code for invoice.
    /// </summary>
    /// <param name="invoice">Invoice that needs ZKI</param>
    /// <param name="certificate">Certificate for signing</param>
    /// <returns>ZKI string (Maybe GUID idk...)</returns>
    public static string ZKI(this RacunNapojnicaType invoice, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var b = Encoding.ASCII.GetBytes(invoice.Oib + invoice.DatVrijeme + invoice.BrRac.BrOznRac + invoice.BrRac.OznPosPr + invoice.BrRac.OznNapUr + invoice.IznosUkupno);
        var signData = (certificate.GetRSAPrivateKey()?.SignData(b, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1)) ?? throw new Exception("Invalid cerrtificate. No RSA Private key.");
        return new string(MD5.HashData(signData).SelectMany(x => x.ToString("x2")).ToArray());
    }
}