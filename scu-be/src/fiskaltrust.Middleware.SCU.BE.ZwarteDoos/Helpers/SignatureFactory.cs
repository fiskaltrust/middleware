using System;
using System.Collections.Generic;
using System.Globalization;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Helpers;

public static class SignatureFactory
{
    public static List<SignatureItem> CreateInitialOperationSignatures() => new List<SignatureItem>();

    public static List<SignatureItem> CreateOutOfOperationSignatures() => new List<SignatureItem>();

    public static List<SignatureItem> CreateZeroReceiptSignatures() => new List<SignatureItem>();
}
