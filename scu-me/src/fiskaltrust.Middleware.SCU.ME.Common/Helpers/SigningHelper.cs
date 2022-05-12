using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.SCU.ME.Common.Configuration;

namespace fiskaltrust.Middleware.SCU.ME.Common.Helpers
{
    public static class SigningHelper
    {
        public static string CreateIIC(ScuMEConfiguration configuration, RegisterInvoiceRequest registerInvoiceRequest)
        {
            var iicInput = string.Join("|", new List<object>
            {
                configuration.TIN,
                registerInvoiceRequest.Moment,
                registerInvoiceRequest.InvoiceDetails.YearlyOrdinalNumber,
                registerInvoiceRequest.BusinessUnitCode,
                registerInvoiceRequest.TcrCode,
                registerInvoiceRequest.SoftwareCode,
                registerInvoiceRequest.InvoiceDetails.GrossAmount
            }.Select(o => o.ToString()));

            var iicSignature = configuration.Certificate.GetRSAPrivateKey()!.SignData(Encoding.ASCII.GetBytes(iicInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var iic = ((HashAlgorithm) CryptoConfig.CreateFromName("MD5")!).ComputeHash(iicSignature);

            return BitConverter.ToString(iic).Replace("-", string.Empty);
        }

    }
}
