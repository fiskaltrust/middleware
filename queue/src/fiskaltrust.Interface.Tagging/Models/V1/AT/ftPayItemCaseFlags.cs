﻿using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.AT
{
    [FlagExtensions(OnType = typeof(PayItem), OnField = nameof(PayItem.ftPayItemCase))]
    public enum ftPayItemCaseFlags : long
    {

    }
}