using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pl;
using FluentAssertions;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest;

/// <summary>
/// A Polish SCU never throws out of ProcessReceiptAsync: a failure is an error response with a
/// FAILURE signature saying what went wrong and what to do next.
/// </summary>
internal static class ProcessResponseFailure
{
    public static string FailureMessage(this ProcessResponse response)
    {
        response.ReceiptResponse.ftState.IsState(State.Error).Should().BeTrue("the SCU reports failures as an error response");
        return response.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "FAILURE").Which.Data;
    }
}
