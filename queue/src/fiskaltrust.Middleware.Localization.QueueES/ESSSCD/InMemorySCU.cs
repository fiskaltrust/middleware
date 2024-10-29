﻿using System.Security.Cryptography;
using System.Text;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Factories;
using fiskaltrust.Middleware.Localization.QueueES.Exports;
using fiskaltrust.Middleware.Localization.QueueES.Factories;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueES.ESSSCD;

public class ESSSCDInfo
{
}

public class InMemorySCUConfiguration
{

}

public class InMemorySCU : IESSSCD
{
    private readonly ftSignaturCreationUnitES _signaturCreationUnitES;

    private readonly VeriFactuMapping _veriFactuMapping;

    public InMemorySCU(ftSignaturCreationUnitES signaturCreationUnitES, MasterDataConfiguration masterData)
    {
        _signaturCreationUnitES = signaturCreationUnitES;
        _veriFactuMapping = new VeriFactuMapping(masterData);
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        if (request.ReceiptRequest.IsVoid())
        {
            throw new NotImplementedException();
        }
        else
        {
            var journalES = _veriFactuMapping.CreateRegistroFacturacionAlta(request.ReceiptRequest, request.ReceiptResponse, request.PreviousReceiptRequest is null || request.PreviousReceiptResponse is null ? null : (
                new IDFacturaExpedidaType
                {
                    IDEmisorFactura = request.PreviousReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (long) SignatureTypesES.IDEmisorFactura).Data,
                    NumSerieFactura = request.PreviousReceiptResponse.ftReceiptIdentification.Split('#')[1],
                    FechaExpedicionFactura = request.PreviousReceiptRequest.cbReceiptMoment.ToString("dd-MM-yyy")
                },
                request.PreviousReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (long) SignatureTypesES.PosReceipt).Data
            ));

            request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateESQRCode(journalES.Huella));
            if (journalES.Encadenamiento.Item is EncadenamientoFacturaAnteriorType encadenamiento)
            {
                request.ReceiptResponse.AddSignatureItem(new SignatureItem
                {
                    Caption = "IDEmisorFactura",
                    Data = encadenamiento.IDEmisorFactura,
                    ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.Text,
                    ftSignatureType = (long) SignatureTypesES.IDEmisorFactura
                });
            }
            return await Task.FromResult(new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse,
            });
        }
    }

    public Task<ESSSCDInfo> GetInfoAsync() => throw new NotImplementedException();
}
