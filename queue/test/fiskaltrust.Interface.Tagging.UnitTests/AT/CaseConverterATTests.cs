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

        [Fact]
        public void ConvertftReceiptCaseToV1_ShouldReturnCorrect()
        {
            var request = new ReceiptRequest { ftReceiptCase = 0x4154200000000000 };
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000000000);

            request.ftReceiptCase = 0x4154200000000001;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000000001);

            request.ftReceiptCase = 0x4154200000000002;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x415400000000000A);

            request.ftReceiptCase = 0x4154200000000002;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x415400000000000B);

            request.ftReceiptCase = 0x4154200000000002;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x415400000000000C);

            request.ftReceiptCase = 0x4154200000000003;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000000007);

            request.ftReceiptCase = 0x4154200000000004;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x415400000000000F);

            request.ftReceiptCase = 0x4154200000000005;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000000009);

            request.ftReceiptCase = 0x4154200000001000;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000000008);

            request.ftReceiptCase = 0x4154200000001002;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000002000);

            request.ftReceiptCase = 0x4154200000001001;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000002001);

            request.ftReceiptCase = 0x4154200000001003;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000002002);

            request.ftReceiptCase = 0x4154200000002000;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000002003);

            request.ftReceiptCase = 0x4154200000002010;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000003000);

            request.ftReceiptCase = 0x4154200000002011;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000003001);

            request.ftReceiptCase = 0x4154200000002012;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000003002);

            request.ftReceiptCase = 0x4154200000002013;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000003003);

            request.ftReceiptCase = 0x4154200000003000;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000003004);

            request.ftReceiptCase = 0x4154200000003001;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000004001);

            request.ftReceiptCase = 0x4154200000003002;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000004002);

            request.ftReceiptCase = 0x4154200000003003;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000004011);

            request.ftReceiptCase = 0x4154200000003004;
            _caseConverterAT.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4154000000004012);
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
        [InlineData(0x4652200000080003, 0x465200000000001E)]
        [InlineData(0x4652200000080001, 0x465200000000001C)]
        [InlineData(0x4652200000080002, 0x465200000000001D)]
        [InlineData(0x4652200000080004, 0x465200000000001F)]       
        [InlineData(0x4652200000080007, 0x4652000000000020)]      
        [InlineData(0x4154200000000068, 0x4154000000000021)]
        [InlineData(0x4154200000000090, 0x4154000000000022)]      
        public void ConvertftChargeItemCaseToV1_ShouldReturnCorrect(long v2FtChargeItemCase, long v1FtChargeItemCase)
        {
            var chargeItem = new ChargeItem { ftChargeItemCase = v2FtChargeItemCase };

            _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(v1FtChargeItemCase);
        }


        [Fact]
        public void Payitem_ShouldReturnCorrect()
        {
            var payItem = new PayItem { ftPayItemCase = 0x4154200000000000 };
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4154000000000000);

            payItem.ftPayItemCase = 0x4154200000000001;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4154000000000001);

            payItem.ftPayItemCase = 0x4154200000100001;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4154000000000002);

            payItem.ftPayItemCase = 0x4154200000000003;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4154000000000003);

            payItem.ftPayItemCase = 0x4154200080000004;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4154000000000004);

            payItem.ftPayItemCase = 0x4154200080000005;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4154000000000005);

            payItem.ftPayItemCase = 0x4154200000000006;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4154000000000006);

            payItem.ftPayItemCase = 0x4154200080000007;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4154000000000007);

            payItem.ftPayItemCase = 0x4154200080000008;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4154000000000008);

            payItem.ftPayItemCase = 0x4154200080000004;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4154000000000009);

            payItem.ftPayItemCase = 0x4154200080000005;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x415400000000000A);

            payItem.ftPayItemCase = 0x4154200000000009;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x415400000000000B);

            payItem.ftPayItemCase = 0x415420008000000A;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x415400000000000C);

            payItem.ftPayItemCase = 0x415420008000000B;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x415400000000000D);

            payItem.ftPayItemCase = 0x415420000000000C;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x415400000000000E);

            payItem.ftPayItemCase = 0x415420000000000C;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x415400000000000F);

            payItem.ftPayItemCase = 0x4154200008000009;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4154000000000010);

            payItem.ftPayItemCase = 0x415420000000000D;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4154000000000011);

            payItem.ftPayItemCase = 0x4154200040000001;
            _caseConverterAT.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4154000000000012);
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


        [Fact]
        public void ConvertftReceiptState_ShouldreturnCorrect()
        {
            var request = new ReceiptResponse { ftState = 0x4154000000000001 };
            _caseConverterAT.ConvertftStateToV2(request);
            request.ftState.Should().Be(0x4154200000010000);  // ScuPermamentOutofService -> OutOfService0x0001

            request.ftState = 0x4154000000000080;
            _caseConverterAT.ConvertftStateToV2(request);
            request.ftState.Should().Be(0x4154200000020000);  // ScuBackup -> BackupSSCDInUse0x0080

            request.ftState = 0x4154000000000010;
            _caseConverterAT.ConvertftStateToV2(request);
            request.ftState.Should().Be(0x4154200010000000);  // MonthlyClosing -> MonthlyReportDue0x0010

            request.ftState = 0x4154000000000020;
            _caseConverterAT.ConvertftStateToV2(request);
            request.ftState.Should().Be(0x4154200020000000);  // YearlyClosing -> AnnualReportDue0x0020
        }
    }
}