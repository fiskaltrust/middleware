using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.PostFiscalization;

/// <summary>The two read-side predicates of RFC 712 and what each one generalizes.</summary>
public class FiscalizationPredicateTests
{
    private static readonly PostFiscalizationStateData _finalizeFailed = new() { FiscalizationSucceeded = true, EReporting = PostFiscalizationStateData.Failed };

    private static ReceiptResponse Response(State state, PostFiscalizationStateData? outcome = null) => new()
    {
        ftState = state,
        ftStateData = outcome is null ? null : new MiddlewareStateData { PostFiscalization = outcome },
    };

    [Fact]
    public void IsFiscalized_GeneralizesNotError()
    {
        Response(State.Success).IsFiscalized().Should().BeTrue();
        Response(State.Success.WithFlag(StateFlags.SecurityMechanismDeactivated)).IsFiscalized().Should().BeTrue("the reference lookups never excluded flagged answers");
        Response(State.Error).IsFiscalized().Should().BeFalse();
        Response(State.Error, _finalizeFailed).IsFiscalized().Should().BeTrue("the finalize failure does not undo the fiscalization");
        Response(State.Fail).IsFiscalized().Should().BeFalse("a response without a queue item was never fiscalized");
    }

    [Fact]
    public void IsSuccessfullyFiscalized_GeneralizesIsStateSuccess()
    {
        Response(State.Success).IsSuccessfullyFiscalized().Should().BeTrue();
        Response(State.Success.WithFlag(StateFlags.SecurityMechanismDeactivated)).IsSuccessfullyFiscalized().Should().BeFalse("the PT read paths always left flagged answers out");
        Response(State.Error).IsSuccessfullyFiscalized().Should().BeFalse();
        Response(State.Error, _finalizeFailed).IsSuccessfullyFiscalized().Should().BeTrue();
        Response(State.Fail).IsSuccessfullyFiscalized().Should().BeFalse();
    }
}
