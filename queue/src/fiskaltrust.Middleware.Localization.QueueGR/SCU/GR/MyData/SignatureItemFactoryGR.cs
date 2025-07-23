using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;
using fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public static class SignatureItemFactoryGR
{
    public static void AddTransmissionFailure1Signature(ProcessRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Data = $"Απώλεια Διασύνδεσης Οντότητας - Παρόχου",
            Caption = "Transmission Failure_1",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypeGR.MyDataInfo.As<SignatureType>()
        });
    }


    public static void AddMarksForConnectedMarks(ProcessRequest request, InvoicesDoc doc)
    {
        request.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Data = string.Join(",", doc.invoice[0].invoiceHeader.multipleConnectedMarks),
            Caption = "",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypeGR.MyDataInfo.As<SignatureType>()
        });
    }

    public static void AddOrderReceiptSignature(ProcessRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Data = $"ΤΟ ΠΑΡΟΝ ΕΙΝΑΙ ΠΛΗΡΟΦΟΡΙΑΚΟ ΣΤΟΙΧΕΙΟ ΚΑΙ ΔΕΝ ΑΠΟΤΕΛΕΙ ΝΟΜΙΜΗ ΦΟΡΟΛΟΓΙΚΗ ΑΠΟΔΕΙΞΗ/ΤΙΜΟΛΟΓΙΟ. THE PRESENT DOCUMENT IS ISSUED ONLY FOR INFORMATION PURPOSES AND DOES NOT STAND FOR A VALID TAX RECEIPT/INVOICE",
            Caption = "",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypeGR.MyDataInfo.As<SignatureType>()
        });
    }

    public static SignatureItem CreateGRQRCode(string qrCode)
    {
        return new SignatureItem()
        {
            Caption = "[www.fiskaltrust.gr]",
            Data = qrCode,
            ftSignatureFormat = SignatureFormat.QRCode,
            ftSignatureType = SignatureTypeGR.PosReceipt.As<SignatureType>()
        };
    }

    public static void AddVivaFiscalProviderSignature(ProcessRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Data = $"2024_12_126VIVA_001_ Viva Fiscal_V1_23122024",
            Caption = "www.viva.com",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypeGR.MyDataInfo.As<SignatureType>()
        });
    }

    public static void AddInvoiceSignature(ProcessRequest request, InvoicesDoc doc)
    {
        request.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Data = $"{doc.invoice[0].issuer.vatNumber}|{doc.invoice[0].invoiceHeader.issueDate.ToString("dd/MM/yyyy")}|{doc.invoice[0].issuer.branch}|{doc.invoice[0].invoiceHeader.invoiceType}|{doc.invoice[0].invoiceHeader.series}|{doc.invoice[0].invoiceHeader.aa}",
            Caption = "Μοναδικός αριθμός παραστατικού",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypeGR.MyDataInfo.As<SignatureType>()
        });
    }
}