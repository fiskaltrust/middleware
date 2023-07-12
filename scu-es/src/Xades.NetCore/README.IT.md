Xades.NetCore
=============

 
INTRODUZIONE
-------------
FirmaXadesNet è una libreria scritta in C# per .NET 5.0  per la creazione di firme XAdES. È stata realizzata dal Dipartimento delle Nuove Tecnologie del Dipartimento di Urbanistica del Comune Spagnolo di Cartagena (Murcia). Si basa su una modifica dello starter kit XAdES sviluppato da Microsoft France.


CARATTERISTICHE
---------------

- Creazione di firme of XAdES-BES, XAdES-EPES, XAdES-T y XAdES-XL.

- Utilizzo di tutti i tipi di certificati supportati da Windows, anche su Smart Card, Token Usb, CNS

- Formati Supportati: Externally Detached, Internally Detached, Enveloped,Enveloping.

- Validazione dei Certificati tramite Authority OCSP o liste di revoca.

- Supporto di cofirmatari e controfirmatari.

- Supporto di RSA-SHA1, RSA-SHA256 y RSA-SHA512.

All'interno della soluzione è presente un progetto con esempi di utilizzo della libreria. Alcuni degli esempi fanno uso del timestamp server  ACCV (Agencia de Tecnología y Certificación Electrónica, Spagna). 

Come esempio di uitlizzo avviare il progetto TextFirmaXades, che consente di firmare digitalmente i file Xml.


**Esempio di utilizzo per firma Enveloped:**

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
