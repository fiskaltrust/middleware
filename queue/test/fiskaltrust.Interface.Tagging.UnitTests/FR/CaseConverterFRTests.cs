using Xunit;
using fiskaltrust.Interface.Tagging.FR;
using fiskaltrust.Interface.Tagging.Interfaces;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
namespace fiskaltrust.Interface.Tagging.UnitTests.DE
{
    public class CaseConverterFRTests
    {
        //private readonly ICaseConverter _caseConverterFR;
        public CaseConverterFRTests()
        {
            //_caseConverterFR = new CaseConverterFR();
        }
        //[Theory]
        //[InlineData(0x4652200000000000, )]
        //[InlineData(0x4652200000000001)]
        //[InlineData(0x4652200000000002)]
        //[InlineData(0x4652200000000003)]
        //[InlineData(0x4652200000000004)]
        //[InlineData(0x4652200000000005)]
        //[InlineData(0x4652200000001000)]
        //[InlineData(0x4652200000001001)]
        //[InlineData(0x4652200000001002)]
        //[InlineData(0x4652200000001003)]
        //[InlineData(0x4652200000002000)]
        //[InlineData(0x4652200000002001)]
        //[InlineData(0x4652200000002010)]
        //[InlineData(0x4652200000002011)]
        //[InlineData(0x4652200000002012)]
        //[InlineData(0x4652200000002013)]
        //[InlineData(0x4652200000003000)]
        //[InlineData(0x4652200000003001)]
        //[InlineData(0x4652200000003002)]
        //[InlineData(0x4652200000003003)]
        //[InlineData(0x4652200000003004)]
        //[InlineData(0x4652200000003005)]
        //[InlineData(0x4652200000003010)]
        //[InlineData(0x4652200000004001)]
        //[InlineData(0x4652200000004002)]
        //[InlineData(0x4652200000004011)]
        //[InlineData(0x4652200000004012)]
        //public void ConvertftReceiptCaseToV1_ShouldreturnCorrect(long v2ftReceiptCase, long v1ftReceiptCase)
        //{
        //    var request = new ReceiptRequest { ftReceiptCase = data };
        //    var result? = data switch
        //    {
        //        0x4652200000010002 => 0x465220000001000C,
        //        _ => null,
        //    }


        //    _caseConverterFR.ConvertftReceiptCaseToV1(request);

        //    request.ftReceiptCase.Should().Be(result);
        //}
    }
}