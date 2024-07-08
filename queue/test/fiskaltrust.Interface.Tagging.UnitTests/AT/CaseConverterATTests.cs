using fiskaltrust.Interface.Tagging.AT;
using fiskaltrust.Interface.Tagging.Interfaces;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Models.V2.Extensions;
using V2 = fiskaltrust.Interface.Tagging.Models.V2;
using V2AT = fiskaltrust.Interface.Tagging.Models.V2.AT;
using fiskaltrust.Interface.Tagging.Models.Extensions;
using fiskaltrust.Interface.Tagging.Models.V2;
using Xunit;

namespace fiskaltrust.Interface.Tagging.UnitTests.AT
{
    public class CaseConverterATTests
    {
        private readonly ICaseConverter _caseConverterAT;

        public CaseConverterATTests()
        {
            _caseConverterAT = new CaseConverterAT();
        }

        [Theory]
        [InlineData(0x4154200080020000, 0x4154800000020000)]
        [InlineData(0x4154200080000001, 0x4154800000000001)]
        [InlineData(0x4154200000002000, 0x4154000000000002)] 
        [InlineData(0x4154200000004001, 0x4154000000000003)]
        [InlineData(0x4154200000004002, 0x4154000000000004)]
        [InlineData(0x4154200000002010, null)]
        [InlineData(0x4154200000002011, null)]
        [InlineData(0x4154200000002012, 0x4154000000000005)]
        [InlineData(0x4154200000002013, 0x4154000000000006)]
        [InlineData(0x4154200080000003, 0x4154000000000007)]
        [InlineData(0x4154200000001001, null)]
        [InlineData(0x4154200000001002, null)]
        [InlineData(0x4154200000001003, null)]
        [InlineData(0x4154200000001000, 0x4154000000000008)]
        [InlineData(0x4154200000000005, 0x4154000000000009)]
        [InlineData(0x4154200000010002, null)]
        [InlineData(0x4154200000003001, null)]
        [InlineData(0x4154200000003002, null)]
        [InlineData(0x4154200000003000, 0x415400000000000D)]
        [InlineData(0x4154200000003003, 0x415400000000000E)]
        [InlineData(0x4154200000000004, 0x415400000000000F)]

        public void ConvertftReceiptCaseToV1_ShouldreturnCorrect(long v2FtReceiptCase, long? v1FtReceiptCase)
        {
            var request = new ReceiptRequest { ftReceiptCase = v2FtReceiptCase };

            if (v1FtReceiptCase == null)
            {
                Assert.Throws<NotImplementedException>(() => _caseConverterAT.ConvertftReceiptCaseToV1(request));
            }
            else
            {
                _caseConverterAT.ConvertftReceiptCaseToV1(request);
                request.ftReceiptCase.Should().Be(v1FtReceiptCase);
            }
        }
        
        [Theory]
        [InlineData(0x4154200000000000, 0x4154000000000000)]
        [InlineData(0x4154200000000003, 0x4154000000000003)]
        [InlineData(0x4154200000000001, 0x4154000000000001)]
        [InlineData(0x4154200000000002, 0x4154000000000002)]
        [InlineData(0x4154200000000004, 0x4154000000000004)]
        [InlineData(0x4154200000000007, 0x4154000000000005)]
        [InlineData(0x4154200000005000, 0x4154000000000006)]
        [InlineData(0x4154200000000060, 0x4154000000000007)]
        [InlineData(0x4154200000000013, 0x415400000000000A)]
        [InlineData(0x4154200000000011, 0x4154000000000008)]
        [InlineData(0x4154200000000012, 0x4154000000000009)]
        [InlineData(0x4154200000000014, 0x415400000000000B)]
        [InlineData(0x4154200000000017, 0x415400000000000C)]
        [InlineData(0x4154200000000023, 0x415400000000000F)]
        [InlineData(0x4154200000000021, 0x415400000000000D)]
        [InlineData(0x4154200000000022, 0x415400000000000E)]
        [InlineData(0x4154200000000024, 0x4154000000000010)]
        [InlineData(0x4154200000000027, 0x4154000000000011)]
        [InlineData(0x4154200000000053, 0x4154000000000014)]
        [InlineData(0x4154200000000051, 0x4154000000000012)]
        [InlineData(0x4154200000000052, 0x4154000000000013)]
        [InlineData(0x4154200000000054, 0x4154000000000015)]
        [InlineData(0x4154200000000057, 0x4154000000000016)]
        [InlineData(0x4154200000000073, 0x4154000000000019)]
        [InlineData(0x4154200000000071, 0x4154000000000017)]
        [InlineData(0x4154200000000072, 0x4154000000000018)]
        [InlineData(0x4154200000000074, 0x415400000000001A)]
        [InlineData(0x4154200000000077, 0x415400000000001B)]
        [InlineData(0x4154200000080003, 0x415400000000001E)]
        [InlineData(0x4154200000080001, 0x415400000000001C)]
        [InlineData(0x4154200000080002, 0x415400000000001D)]
        [InlineData(0x4154200000080004, 0x415400000000001F)]       
        [InlineData(0x4154200000080007, 0x4154000000000020)]      
        [InlineData(0x4154200000000068, 0x4154000000000021)]
        [InlineData(0x4154200000000090, 0x4154000000000022)]      
        public void ConvertftChargeItemCaseToV1_ShouldReturnCorrect(long v2FtChargeItemCase, long v1FtChargeItemCase)
        {
            var chargeItem = new ChargeItem { ftChargeItemCase = v2FtChargeItemCase };

            _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(v1FtChargeItemCase);
        }


        [Theory]
        [InlineData(0x4154200000000000, 0x4154000000000000)]
        [InlineData(0x4154200000400001, 0x4154000000000012)]
        [InlineData(0x4154200000100001, 0x4154000000000002)]
        [InlineData(0x4154200000000001, 0x4154000000000001)]
        [InlineData(0x4154200000000002, null)]
        [InlineData(0x4154200000000003, 0x4154000000000003)]
        [InlineData(0x4154200000000004, 0x4154000000000004)]
        [InlineData(0x4154200000000005, 0x4154000000000005)]
        [InlineData(0x4154200000000006, 0x4154000000000006)]
        [InlineData(0x4154200000000007, 0x4154000000000007)]
        [InlineData(0x4154200000000008, 0x4154000000000008)]
        [InlineData(0x4154200000080009, 0x4154000000000010)]
        [InlineData(0x4154200000000009, 0x415400000000000B)]
        [InlineData(0x415420000000000A, 0x415400000000000C)]
        [InlineData(0x415420000000000B, 0x415400000000000D)]
        [InlineData(0x415420000000000C, 0x415400000000000E)]
        [InlineData(0x415420000000000D, 0x4154000000000011)]
        [InlineData(0x415420000000000E, null)]
        [InlineData(0x415420000000000F, null)]
        public void ConvertftPayItemCaseToV1_ShouldreturnCorrect(long v2FtPayItemCase, long? v1FtPayItemCase)
        {
            var payItem = new PayItem { ftPayItemCase = v2FtPayItemCase };
           
            if(v1FtPayItemCase == null)
            {
                Assert.Throws<NotImplementedException>(() => _caseConverterAT.ConvertftPayItemCaseToV1(payItem));
            }
            else
            {
                _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
                payItem.ftPayItemCase.Should().Be(v1FtPayItemCase);
            }            
        }

        [Fact]
        public void JournalTypes_ShouldreturnCorrect()
        {
            var journal = new JournalRequest { ftJournalType = 0x4154200000001000 };
            _caseConverterAT.ConvertftJournalTypeToV1(journal);
            journal.ftJournalType.Should().Be(0x4154000000000000);
            journal.ftJournalType = 0x4154200000001001;
            _caseConverterAT.ConvertftJournalTypeToV1(journal);
            journal.ftJournalType.Should().Be(0x4154000000000001);
        }

        [Fact]
        public void SignatureTypes_ShouldreturnCorrect()
        {
            var signature = new SignaturItem { ftSignatureType = 0x4154_0000_0000_0000 };
            signature.SetTypeVersion(2);
            signature.SetV2CategorySignatureType((long)V2.SignatureTypesCategory.Failure0x3);
            signature.SetV2SignatureType((long)V2AT.ftSignatureTypes.SignatureAccordingToRKSV0x0001);
            signature.ftSignatureType.Should().Be(0x4154200000003001);

            signature.ftSignatureType = 0x4154_0000_0000_0001;
            _caseConverterAT.ConvertftSignatureTypeToV2(signature);
            signature.ftSignatureType.Should().Be(0x4154_2000_0000_0001);

            signature.ftSignatureType = 0x4154_0000_0000_0002;
            _caseConverterAT.ConvertftSignatureTypeToV2(signature);
            signature.ftSignatureType.Should().Be(0x4154_2000_0000_0002);

            signature.ftSignatureType = 0x4154_0000_0000_0003;
            _caseConverterAT.ConvertftSignatureTypeToV2(signature);
            signature.ftSignatureType.Should().Be(0x4154_2000_0000_0003);
        }


        [Fact]
        public void SignatureFormat_ShouldreturnCorrect()
        {
            var signature = new SignaturItem { ftSignatureFormat = 0x4154000000000000 };

            signature.SetV2SignatureFormat((long)ftSignatureFormats.Unknown0x0000);
            signature.ftSignatureFormat.Should().Be(0x4154000000000000);

            signature.SetV2SignatureFormat((long)ftSignatureFormats.Text0x0001);
            signature.ftSignatureFormat.Should().Be(0x4154000000000001);

            signature.SetV2SignatureFormat((long)ftSignatureFormats.Link0x0002);
            signature.ftSignatureFormat.Should().Be(0x4154000000000002);

            signature.SetV2SignatureFormat((long)ftSignatureFormats.QrCode0x0003);
            signature.ftSignatureFormat.Should().Be(0x4154000000000003);

            signature.SetV2SignatureFormat((long)ftSignatureFormats.Code1280x0004);
            signature.ftSignatureFormat.Should().Be(0x4154000000000004);

            signature.SetV2SignatureFormat((long)ftSignatureFormats.OcrA0x0005);
            signature.ftSignatureFormat.Should().Be(0x4154000000000005);

            signature.SetV2SignatureFormat((long)ftSignatureFormats.Pdf4170x0006);
            signature.ftSignatureFormat.Should().Be(0x4154000000000006);

            signature.SetV2SignatureFormat((long)ftSignatureFormats.DataMatrix0x0007);
            signature.ftSignatureFormat.Should().Be(0x4154000000000007);

            signature.SetV2SignatureFormat((long)ftSignatureFormats.Aztec0x0008);
            signature.ftSignatureFormat.Should().Be(0x4154000000000008);

            signature.SetV2SignatureFormat((long)ftSignatureFormats.Ean8Barcode0x0009);
            signature.ftSignatureFormat.Should().Be(0x4154000000000009);

            signature.SetV2SignatureFormat((long)ftSignatureFormats.Ean130x000A);
            signature.ftSignatureFormat.Should().Be(0x415400000000000A);

            signature.SetV2SignatureFormat((long)ftSignatureFormats.UPCA0x000B);
            signature.ftSignatureFormat.Should().Be(0x415400000000000B);

            signature.SetV2SignatureFormat((long)ftSignatureFormats.Code390x000C);
            signature.ftSignatureFormat.Should().Be(0x415400000000000C);
        }


        [Theory]
        [InlineData(0x4154000000000001, 0x4154200000000001)]
        [InlineData(0x4154000000000002, 0x4154200000000002)]
        [InlineData(0x4154000000000004, 0x4154200000000004)]
        [InlineData(0x4154000000000008, 0x4154200000000008)]
        [InlineData(0x4154000000000010, 0x4154200000000010)]
        [InlineData(0x4154000000000020, 0x4154200000000020)]
        [InlineData(0x4154000000000040, 0x4154200000000040)]
        [InlineData(0x4154000000000080, 0x4154200000000080)]
        public void ConvertftStateToV2_ShouldreturnCorrect(long v1FtState, long? v2FtState)
        {
            var response = new ReceiptResponse { ftState = v1FtState };

            if (v2FtState == null)
            {
                Assert.Throws<NotImplementedException>(() => _caseConverterAT.ConvertftStateToV2(response));
            }
            else
            {
                _caseConverterAT.ConvertftStateToV2(response);
                response.ftState.Should().Be(v2FtState);
            }
        }
    }
}