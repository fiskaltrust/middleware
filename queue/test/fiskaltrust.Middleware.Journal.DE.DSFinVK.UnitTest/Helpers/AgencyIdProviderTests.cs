using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Helpers
{
#pragma warning disable IDE0042 // Deconstruct variable declaration
    public class AgencyIdProviderTests
    {
        [Fact]
        public void GetIdByGuid_ShouldReturn_SameResults_WhenCalledMultipleTimes()
        {
            var agency1 = (Guid: "4360b32a-d242-475d-8786-e6436b63fce4", Id: 1);
            var agency2 = (Guid: "b4cc38ab-5d60-443f-a3e3-e95f475ed98f", Id: 2);
            
            AgencyIdProvider.GetIdByGuid(agency1.Guid).Should().Be(agency1.Id);
            AgencyIdProvider.GetIdByGuid(agency1.Guid).Should().Be(agency1.Id);

            AgencyIdProvider.GetIdByGuid(agency2.Guid).Should().Be(agency2.Id);
            AgencyIdProvider.GetIdByGuid(agency2.Guid).Should().Be(agency2.Id);

            AgencyIdProvider.GetIdByGuid(agency1.Guid).Should().Be(agency1.Id);
        }

        [Fact]
        public void GetIdByGuid_ShouldReturn_0_ForEmptyGuid()
        {
            AgencyIdProvider.GetIdByGuid(null).Should().Be(0);
            AgencyIdProvider.GetIdByGuid(string.Empty).Should().Be(0);
        }
    }
}
