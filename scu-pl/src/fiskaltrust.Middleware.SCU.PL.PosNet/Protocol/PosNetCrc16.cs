using System;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

/// <summary>
/// CRC16-CCITT as specified by the POSNET protocol: poly 0x1021, init 0x0000, no input/output
/// reflection, no final xor (a.k.a. CRC-16/XMODEM). Spec check vector: CRC("123456789") = 0x31C3.
/// </summary>
public static class PosNetCrc16
{
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = 0x0000;
        foreach (var b in data)
        {
            crc ^= (ushort)(b << 8);
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
            }
        }
        return crc;
    }
}
