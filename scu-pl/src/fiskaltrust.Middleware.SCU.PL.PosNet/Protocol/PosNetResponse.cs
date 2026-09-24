using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

/// <summary>
/// A decoded POSNET response frame. A response is either confirmed (the command id echoed back,
/// possibly with result parameters) or a confirmed error (<c>?nnnn</c> after the command id, or a
/// standalone <c>ERR</c> frame for protocol-level rejections).
/// </summary>
public sealed class PosNetResponse
{
    public PosNetResponse(string commandId, int? errorCode, IReadOnlyDictionary<string, string> parameters)
    {
        CommandId = commandId;
        ErrorCode = errorCode;
        Parameters = parameters;
    }

    /// <summary>The echoed command mnemonic, or <c>ERR</c> for a frame-level error response.</summary>
    public string CommandId { get; }

    /// <summary>The decimal error number from a <c>?nnnn</c> field, if the device reported one.</summary>
    public int? ErrorCode { get; }

    public bool IsError => ErrorCode.HasValue || IsFrameError;

    public bool IsFrameError => CommandId == "ERR";

    /// <summary>Result parameters keyed by their two-letter mnemonic (e.g. scomm's fs/tz/ts/hr/nu).</summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }
}
