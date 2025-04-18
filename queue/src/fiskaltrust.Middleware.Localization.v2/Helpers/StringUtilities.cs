﻿using System;

namespace fiskaltrust.Middleware.Localization.v2.Helpers;

public static class StringUtilities
{
    public static string ToBase64UrlString(byte[] bytes)
    {
        var base64 = Convert.ToBase64String(bytes);
        return base64.TrimEnd(new char[] { '=' }).Replace('+', '-').Replace('/', '_');
    }
}
