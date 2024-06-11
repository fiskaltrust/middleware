using Xunit;
using fiskaltrust.Interface.Tagging.FR;
using fiskaltrust.Interface.Tagging.Interfaces;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
namespace fiskaltrust.Interface.Tagging.UnitTests.DE
{
    public class CaseConverterFRTests
    {
        private readonly ICaseConverter _caseConverterFR;
        public CaseConverterFRTests()
        {
            _caseConverterFR = new CaseConverterFR();
        }
        [Fact]
        public void ConvertftReceiptCaseToV1_ShouldreturnCorrect()
        {
            var request = new ReceiptRequest { ftReceiptCase = 0x4652200000010002 };
            _caseConverterFR.ConvertftReceiptCaseToV1(request);

            request.ftReceiptCase.Should().Be(0x465220000001000C);
        }
    }
}