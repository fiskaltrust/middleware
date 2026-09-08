using System;
using System.IO;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

/// <summary>
/// Collects what a transport reads until one complete response frame (STX … ETX) is there, so the
/// TCP and the serial transport frame the byte stream identically. Bytes before the STX are dropped
/// — a serial line can carry a stray byte, and the late tail of an earlier, abandoned answer must
/// not be glued onto the next one. Bytes after the ETX are dropped as well: the protocol is strictly
/// one command, one answer. Payload characters start at 0x20, so scanning for STX/ETX cannot hit
/// content bytes.
/// </summary>
public sealed class ResponseFrameBuffer
{
    private readonly MemoryStream _frame = new();
    private bool _started;

    public bool IsComplete { get; private set; }

    public void Append(ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            if (IsComplete)
            {
                return;
            }
            if (!_started)
            {
                if (value != PosNetFrame.Stx)
                {
                    continue;
                }
                _started = true;
            }
            _frame.WriteByte(value);
            if (value == PosNetFrame.Etx)
            {
                IsComplete = true;
            }
        }
    }

    public byte[] ToArray() => _frame.ToArray();
}
