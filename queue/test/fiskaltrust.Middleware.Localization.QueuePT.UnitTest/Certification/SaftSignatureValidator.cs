using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;
public class OpenSslSigner
{
    public static string SignTextWithOpenSSL(string textToSign, string privateKeyPath, string opensslPath)
    {
        string tempInput = Path.GetTempFileName();
        string tempSig = Path.GetTempFileName();
        string tempBase64 = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempInput, textToSign);
            RunOpenSsl(opensslPath, $"dgst -sha1 -sign \"{privateKeyPath}\" -out \"{tempSig}\" \"{tempInput}\"");
            RunOpenSsl(opensslPath, $"enc -base64 -in \"{tempSig}\" -out \"{tempBase64}\" -A");
            return File.ReadAllText(tempBase64);
        }
        finally
        {
            // Clean up temp files
            File.Delete(tempInput);
            File.Delete(tempSig);
            File.Delete(tempBase64);
        }
    }

    private static void RunOpenSsl(string opensslPath, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = opensslPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"OpenSSL failed: {stderr}");
    }
}

public class SaftSignatureValidator
{

    [Fact]
    public void TestSignature()
    {
        var scu = new InMemorySCU(new storage.V0.ftSignaturCreationUnitPT
        {
            PrivateKey = File.ReadAllText("C:\\Users\\stefa\\OneDrive\\Desktop\\Portugal_Registration\\PrivateKey.pem"),

            SoftwareCertificateNumber = "9999"
        });
        var data = scu.GetHashForItem(new PTInvoiceElement
        {
            InvoiceDate = DateTime.Parse("2025-03-06"),
            SystemEntryDate = new DateTime(2025, 03, 06, 07, 34, 12, DateTimeKind.Utc),
            InvoiceNo = "FS ft2024/0001",
            GrossTotal = 0.50m,
            Hash = ""
        });

        var hash = scu.SignData(data);
        var hash2 = OpenSslSigner.SignTextWithOpenSSL(data, "C:\\Users\\stefa\\OneDrive\\Desktop\\Portugal_Registration\\PrivateKey.pem", "C:\\Program Files\\Git\\usr\\bin\\openssl");
    }

    [Fact]
    public void ValidateInvoiceSignatures_ValidSignature_ReturnsEmptyList()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        // Arrange
        string xmlPath = "C:\\Users\\stefa\\OneDrive\\Desktop\\Portugal_Registration\\20250729\\SAFT_journal.xml"; // Replace with actual path
        string publicKeyPem = "-----BEGIN PUBLIC KEY-----\r\nMIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDj7eh82hMWdN538YwQZCeYmZ6D\r\nUG/1KVLZqytOX0IshND32CvyurLRDJyyeII3rc0YOwhNKcJDvL30e+sYzgVZQSyJ\r\nlQlvD0vdYBLkyiPQhibUM5+dxQZao1lw6hNQoIB1JpSHrfIZBSRJt0MB+v7FzxdC\r\nE+jh0ntxiA5VyyBndwIDAQAB\r\n-----END PUBLIC KEY-----\r\n"; // Replace with actual PEM key
        // Act
        var result = ValidateInvoiceSignatures(xmlPath, publicKeyPem);
        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Validates the digital signatures (Hash) of sales invoices in a SAF‑T (PT) XML file.
    /// </summary>
    /// <param name="xmlPath">Path to the SAF‑T file.</param>
    /// <param name="publicKeyPem">
    /// PEM-encoded RSA public key that corresponds to the private key used to sign the documents.
    /// </param>
    /// <returns>List of invoice numbers whose signatures are invalid; empty if all are valid.</returns>
    public static List<string> ValidateInvoiceSignatures(string xmlPath, string publicKeyPem)
    {
        var invalidInvoices = new List<string>();

        // Load the SAF‑T file with LINQ to XML.
        XDocument doc = XDocument.Load(xmlPath);
        XNamespace ns = doc.Root!.GetDefaultNamespace();

        // Navigate to the SalesInvoices section.
        var invoices = doc.Descendants(ns + "Invoice");

        // Keep track of the hash from the previous record (for chaining).
        string previousHash = "0";

        var dictionary = new Dictionary<string, string>();

        foreach (var invoice in invoices)
        {
            string invoiceNo = invoice.Element(ns + "InvoiceNo")?.Value?.Trim() ?? "";
            string invoiceDate = invoice.Element(ns + "InvoiceDate")?.Value?.Trim() ?? "";
            string systemEntryDate = invoice.Element(ns + "SystemEntryDate")?.Value?.Trim() ?? "";
            string grossTotal = invoice.Element(ns + "DocumentTotals")?
                                       .Element(ns + "GrossTotal")?.Value?.Trim() ?? "";

            string hash = invoice.Element(ns + "Hash")?.Value?.Trim() ?? "";
            string hashCtrl = invoice.Element(ns + "HashControl")?.Value?.Trim() ?? "";
            string invoiceType = invoice.Element(ns + "InvoiceType")?.Value?.Trim() ?? "";
            
            // If the software is not certified (both fields "0"), no signature to validate
            //:contentReference[oaicite:1]{index=1}.
            if (hash == "0" && hashCtrl == "0")
            {
                dictionary[invoiceType] = hash;
                continue;
            }

            // Build the string that should have been signed.  According to
            // Ofício Circulado 50 001/2013, the string is composed of the
            // concatenation of key fields separated by semicolons and ending
            // with a semicolon:contentReference[oaicite:2]{index=2}.
            // For the first record in a series, previousHash = "0".
            string messageToSign = $"{invoiceDate};{systemEntryDate};{invoiceNo};{grossTotal};";
            if (dictionary.ContainsKey(invoiceType) && dictionary[invoiceType] != "0")
            {
                // In many implementations the previous document's hash is prepended
                // to the string (consult your software documentation if chaining applies).
                messageToSign = $"{messageToSign}{dictionary[invoiceType]}";
            }

            // Convert the stored Hash (Base‑64) into a byte array.
            byte[] signatureBytes;
            try
            {
                signatureBytes = Convert.FromBase64String(hash);
            }
            catch (FormatException)
            {
                invalidInvoices.Add(invoiceNo);
                dictionary[invoiceType] = hash;
                continue;
            }

            byte[] dataBytes = Encoding.UTF8.GetBytes(messageToSign);

            // Verify signature using RSA public key and SHA‑1.
            bool signatureValid;
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportFromPem(publicKeyPem.ToCharArray());
                signatureValid = rsa.VerifyData(
                    dataBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA1,
                    RSASignaturePadding.Pkcs1);
            }

            if (!signatureValid)
            {
                invalidInvoices.Add(invoiceNo);
            }

            // Update previous hash for chaining.
            dictionary[invoiceType] = hash;
        }

        return invalidInvoices;
    }
}