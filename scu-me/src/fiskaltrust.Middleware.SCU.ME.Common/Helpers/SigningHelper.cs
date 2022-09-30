using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static (string iic, string iicSignature) CreateIIC(ScuMEConfiguration configuration, ComputeIICRequest computeIICRequest)
        {
            var iicSignature = CreateIicSignature(configuration, computeIICRequest);
            
            var iic = ((HashAlgorithm) CryptoConfig.CreateFromName("MD5")!).ComputeHash(iicSignature);

            return (BitConverter.ToString(iic).Replace("-", string.Empty), BitConverter.ToString(iicSignature).Replace("-", string.Empty));
        }

        private static byte[] CreateIicSignature(ScuMEConfiguration configuration, ComputeIICRequest computeIICRequest)
        {
            var nfi = new NumberFormatInfo
            {
                NumberDecimalSeparator = "."
            };
            var iicInput = string.Join("|", new List<object>
            {
                configuration.TIN,
                computeIICRequest.Moment.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                computeIICRequest.YearlyOrdinalNumber,
                computeIICRequest.BusinessUnitCode,
                computeIICRequest.TcrCode,
                computeIICRequest.SoftwareCode,
                computeIICRequest.GrossAmount.ToString(nfi)
            }.Select(o => o.ToString()));
            return configuration.Certificate.GetRSAPrivateKey()!.SignData(Encoding.ASCII.GetBytes(iicInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
    }
}
