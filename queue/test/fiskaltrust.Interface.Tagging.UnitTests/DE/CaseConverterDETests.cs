using Xunit;
using fiskaltrust.Interface.Tagging.DE;
using fiskaltrust.Interface.Tagging.Interfaces;
using FluentAssertions;
namespace fiskaltrust.Interface.Tagging.UnitTests.DE
{
    public class CaseConverterDETests
    {
        private readonly ICaseConverter _caseConverterDE;
        public CaseConverterDETests()
        {
            _caseConverterDE = new CaseConverterDE();
        }
        [Fact]
        public void ConvertftReceiptCaseToV1_ShouldreturnCorrect()
        {
            var ftReceiptCaseV2 = 4919338167972200450;
            var result = _caseConverterDE.ConvertftReceiptCaseToV1(ftReceiptCaseV2);

            result.Should().Be(4919338167972200465);
        }
    }
}