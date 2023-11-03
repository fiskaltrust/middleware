using System;
using System.Text;
using System.Security.Cryptography;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public static class GlobalTools
{
    public static string GetSHA256(string input)
    {
        using var shA256Managed = SHA256.Create();
        var stringBuilder = new StringBuilder();
        var bytes = Encoding.UTF8.GetBytes(input);
        var byteCount = Encoding.UTF8.GetByteCount(input);
        return Convert.ToBase64String(shA256Managed.ComputeHash(bytes, 0, byteCount));
    }

    private static byte[] HashHMAC(byte[] key, byte[] message) => new HMACSHA256(key).ComputeHash(message);

    private static byte[] ConvertStringToByteArray(string text) => Encoding.UTF8.GetBytes(text);

    public static string CreateHMAC(byte[] key, string source)
    {
        var byteArray = GlobalTools.ConvertStringToByteArray(source);
        return Convert.ToBase64String(GlobalTools.HashHMAC(key, byteArray));
    }

    public static bool VerifyHMAC(byte[] key, string hmacValue, string source)
    {
        var flag = false;
        var numArray1 = Convert.FromBase64String(hmacValue);
        var byteArray = GlobalTools.ConvertStringToByteArray(source);
        var numArray2 = GlobalTools.HashHMAC(key, byteArray);
        for (var index = 0; index < numArray1.Length; ++index)
        {
            if (numArray2[index] != numArray1[index])
            {
                flag = true;
            }
        }
        return !flag;
    }
}
