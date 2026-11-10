using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Helpers
{
#pragma warning disable IDE0042 // Deconstruct variable declaration
    public class TseIdProviderTests
    {
        [Fact]
        public void GetIdByGuid_ShouldReturn_SameResults_WhenCalledMultipleTimes()
        {
            var tse1 = (Guid: "BDtPHNfQi9mFA59OVTEg38uM0l8nfL45j2MT7TbPnnEInK+Redm8Jm0xLi3UooIy/FwExUjNibb43JG8e91osusawLqCUQBmkAfyIWuNQBMbj2pwZ20KAlAbOXM/1IMzbA==", Id: 1);
            var tse2 = (Guid: "AAAAANfQi9mFA59OVTEg38uM0l8nfL45j2MT7TbPnnEInK+Redm8Jm0xLi3UooIy/FwExUjNibb43JG8e91osusawLqCUQBmkAfyIWuNQBMbj2pwZ20KAlAbOXM/1IMzbA==", Id: 2);
            
            TseIdProvider.GetIdByPublicKey(tse1.Guid).Should().Be(tse1.Id);
            TseIdProvider.GetIdByPublicKey(tse1.Guid).Should().Be(tse1.Id);

            TseIdProvider.GetIdByPublicKey(tse2.Guid).Should().Be(tse2.Id);
            TseIdProvider.GetIdByPublicKey(tse2.Guid).Should().Be(tse2.Id);

            TseIdProvider.GetIdByPublicKey(tse1.Guid).Should().Be(tse1.Id);
        }
    }
}
