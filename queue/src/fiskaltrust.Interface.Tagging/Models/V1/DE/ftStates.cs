﻿using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.DE
{
    [CaseExtensions(OnType = typeof(ReceiptResponse), OnField = nameof(ReceiptResponse.ftState), Mask = 0xFFFF, Prefix = "V1", CaseName = "State")]
    public enum ftStates : long
    {
        Ready0x0000 = 0x0000,
        SecurityMechanismOutOfService0x0001 = 0x0001,
        ScuTemporaryOutOfService0x0002 = 0x0002,
        LateSigningMode0x0008 = 0x0008,
        ScuSwitch0x0100 = 0x0100,
    }
}