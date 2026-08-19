using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

/// <summary>
/// Encodes and decodes POSNET protocol frames: STX data TAB ... '#' CRC16(4 hex) ETX, where the
/// checksum covers everything between STX and '#' (both exclusive). Text travels as WINDOWS-1250 —
/// the code page in which the printer renders all Polish characters.
/// </summary>
public static class PosNetFrame
{
    public const byte Stx = 0x02;
    public const byte Etx = 0x03;
    public const byte Tab = 0x09;
    private const byte Hash = (byte)'#';

    private static readonly Encoding s_encoding;

    static PosNetFrame()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        s_encoding = Encoding.GetEncoding(1250, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);
    }

    public static byte[] Encode(PosNetCommand command)
    {
        var payload = new StringBuilder(command.Mnemonic).Append('\t');
        foreach (var parameter in command.Parameters)
        {
            payload.Append(parameter.Key).Append(parameter.Value).Append('\t');
        }

        var payloadBytes = s_encoding.GetBytes(payload.ToString());
        var crc = PosNetCrc16.Compute(payloadBytes);

        var frame = new byte[payloadBytes.Length + 7];
        frame[0] = Stx;
        payloadBytes.CopyTo(frame, 1);
        frame[payloadBytes.Length + 1] = Hash;
        var crcHex = crc.ToString("X4", CultureInfo.InvariantCulture);
        Encoding.ASCII.GetBytes(crcHex, 0, 4, frame, payloadBytes.Length + 2);
        frame[^1] = Etx;
        return frame;
    }

    public static PosNetResponse Decode(byte[] frame)
    {
        if (frame.Length < 3 || frame[0] != Stx || frame[^1] != Etx)
        {
            throw new PosNetProtocolException("The device response is not a valid POSNET frame (missing STX/ETX).");
        }

        var content = new ArraySegment<byte>(frame, 1, frame.Length - 2);
        var hashIndex = FindLast(content, Hash);
        if (hashIndex < 0 || content.Count - hashIndex != 5)
        {
            throw new PosNetProtocolException("The device response does not carry a '#'-prefixed CRC16 checksum.");
        }

        var payload = content.Slice(0, hashIndex);
        var crcText = Encoding.ASCII.GetString(content.Slice(hashIndex + 1, 4));
        if (!ushort.TryParse(crcText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var declaredCrc)
            || PosNetCrc16.Compute(payload) != declaredCrc)
        {
            throw new PosNetProtocolException("The CRC16 checksum of the device response does not match its content.");
        }

        var fields = s_encoding.GetString(payload).Split('\t', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length == 0)
        {
            throw new PosNetProtocolException("The device response frame is empty.");
        }

        var commandId = fields[0];
        int? errorCode = null;
        var parameters = new Dictionary<string, string>();
        for (var i = 1; i < fields.Length; i++)
        {
            var field = fields[i];
            if (field.StartsWith('?') && int.TryParse(field.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var code))
            {
                errorCode = code;
            }
            else if (field.StartsWith('@'))
            {
                // Tokens are not used by this client; an echoed token is ignored.
            }
            else if (field.Length >= 2)
            {
                parameters[field[..2]] = field[2..];
            }
        }

        return new PosNetResponse(commandId, errorCode, parameters);
    }

    private static int FindLast(ArraySegment<byte> segment, byte value)
    {
        for (var i = segment.Count - 1; i >= 0; i--)
        {
            if (segment[i] == value)
            {
                return i;
            }
        }
        return -1;
    }
}

/// <summary>The device answered, but not with a decodable POSNET frame — a wire-level defect, not a device error.</summary>
public class PosNetProtocolException : PLSSCDException
{
    public PosNetProtocolException(string message) : base(message) { }
}
