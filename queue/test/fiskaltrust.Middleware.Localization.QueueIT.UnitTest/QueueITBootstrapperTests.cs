using fiskaltrust.Middleware.Localization.v2.Interface;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest;

public class QueueITBootstrapperTests
{
    [Fact]
    public void ImplementsIV2QueueBootstrapper()
    {
        typeof(IV2QueueBootstrapper).IsAssignableFrom(typeof(QueueITBootstrapper)).Should().BeTrue();
    }
}
