Xades.NetCore
=============

 
INTRODUCTION
-------------
FirmaXadesNet is a C# (.NET 5.0) library for creation of XAdES signatures. Developed by the Department of New Technologies of the Urban Planning Department of the Cartagena City Council, it is based on a modification of the XAdES starter kit developed by Microsoft France.


FEATURES
--------

- Creation of XAdES-BES, XAdES-EPES, XAdES-T y XAdES-XL signatures.

- Use of Windows-Supported certificates, Smart Cards, Usb Tokens....

- Signature Formats: Externally Detached, Internally Detached, Enveloped y Enveloping.

- Certificte validation by OCSP or against revocation list.

- Support for co-signatures and countersignatures.

- Support for RSA-SHA1, RSA-SHA256 y RSA-SHA512.

Within the solution there is a project with examples of use of the library. Some of the examples make use of the ACCV timestamp server (Agencia de Tecnología y Certificación Electrónica, Spain).

As an example of use, start the TextFirmaXades project, which allows  to digitally sign Xml files.

**Enveloped Signature Example:**

```C#
private void button3_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(nomeFileXml))
        {
            MessageBox.Show("l'Xml non è pronto per la firma.");
            return;
        }

        XadesService xadesService = new XadesService();
        SignatureParameters parametri = new SignatureParameters();
        parametri.SignatureMethod = SignatureMethod.RSAwithSHA512;
        parametri.SigningDate = DateTime.Now;

        // Test SignatureCommitment
        var sc = new SignatureCommitment(SignatureCommitmentType.ProofOfOrigin);
        parametri.SignatureCommitments.Add(sc);

        parametri.SignaturePackaging = SignaturePackaging.ENVELOPED;

        using (parametri.Signer = new Signer(CertUtil.SelectCertificate()))
        {
            using (FileStream fs = new FileStream(nomeFileXml, FileMode.Open))
            {
                _signatureDocument = xadesService.Sign(fs, parametri);
            }

        }
        _signatureDocument.Save(nomeFileXmlFirmato);
        MessageBox.Show("File Firmato Correttamente.", "Firma XADES",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
      
    }
```
