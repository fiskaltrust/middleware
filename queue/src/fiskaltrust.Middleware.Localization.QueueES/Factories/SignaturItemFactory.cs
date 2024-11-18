﻿using System.Web;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueES.Factories;

public static class SignaturItemFactory
{
    public static SignatureItem CreateInitialOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureType = (long) SignatureTypesES.InitialOperationReceipt,
            ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.Text,
            Caption = $"Initial-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignatureItem CreateOutOfOperationSignature(ftQueue queue)
    {
        return new SignatureItem()
        {
            ftSignatureType = (long) SignatureTypesES.OutOfOperationReceipt,
            ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.Text,
            Caption = $"Out-of-operation receipt",
            Data = $"Queue-ID: {queue.ftQueueId}"
        };
    }

    public static SignatureItem CreateESQRCode(string baseUrl, RegistroFacturacionAltaType registroFacturacionAlta)
    {
        var query = HttpUtility.ParseQueryString(String.Empty);
        query.Add("nif", registroFacturacionAlta.IDFactura.IDEmisorFactura);
        query.Add("numserie", registroFacturacionAlta.IDFactura.NumSerieFactura);
        query.Add("fecha", registroFacturacionAlta.IDFactura.FechaExpedicionFactura);
        query.Add("importe", registroFacturacionAlta.ImporteTotal);

        var uriBuider = new UriBuilder(baseUrl)
        {
            Query = query.ToString()
        };

        return new SignatureItem()
        {
            Caption = "[www.fiskaltrust.es]",
            Data = uriBuider.Uri.ToString(),
            ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.QR_Code,
            ftSignatureType = (long) SignatureTypesES.PosReceipt
        };
    }

    public static SignatureItem CreateESQRCode(string qrCode)
    {
        return new SignatureItem()
        {
            Caption = "[www.fiskaltrust.es]",
            Data = qrCode,
            ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.QR_Code,
            ftSignatureType = (long) SignatureTypesES.PosReceipt
        };
    }
}
