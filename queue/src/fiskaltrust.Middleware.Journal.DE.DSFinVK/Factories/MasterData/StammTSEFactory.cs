using System;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.Common.Extensions;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Journal.DE.Common.Models;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.MasterData
{
    public class StammTSEFactory
    {
        private readonly DSFinVKParameters _parameters;

        public StammTSEFactory(DSFinVKParameters parameters)
        {
            _parameters = parameters;
        }

        public StammTSE Create(ReceiptResponse dailyClosingResponse, long zNumber, DateTime dailyClosingMoment, TseData tseData)
        {
            var publicKey = dailyClosingResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (long) SignatureTypesDE.PublicKey)?.Data;
            var signAlg = dailyClosingResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (long) SignatureTypesDE.SignaturAlgorithm)?.Data;
            var timeformat = dailyClosingResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (long) SignatureTypesDE.LogTimeFormat)?.Data;

            publicKey ??= tseData?.PublicKeyBase64;
            signAlg ??= tseData?.SignAlgorithm;
            timeformat ??= tseData?.TimeFormat;
            return new StammTSE
            {
                Z_KASSE_ID = _parameters.CashboxIdentification,
                Z_ERSTELLUNG = dailyClosingMoment,
                Z_NR = zNumber,
                TSE_ID = TseIdProvider.GetIdByPublicKey(publicKey),
                TSE_SERIAL = Conversion.FromTsePublicKeyToSerialNumber(publicKey),
                TSE_SIG_ALGO = signAlg,
                TSE_ZEITFORMAT = timeformat,
                TSE_PD_ENCODING = "UTF-8",
                TSE_PUBLIC_KEY = publicKey,
                TSE_ZERTIFIKAT_I = _parameters.TSECertificateBase64.SafeSubstring(0, 1000),
                TSE_ZERTIFIKAT_II = _parameters.TSECertificateBase64.SafeSubstring(1000, 2000),
                TSE_ZERTIFIKAT_III = _parameters.TSECertificateBase64.SafeSubstring(2000, 3000),
                TSE_ZERTIFIKAT_IV = _parameters.TSECertificateBase64.SafeSubstring(3000, 4000),
                TSE_ZERTIFIKAT_V = _parameters.TSECertificateBase64.SafeSubstring(4000, 5000),
            };
        }
    }
}
