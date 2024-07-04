using Xunit;
using fiskaltrust.Interface.Tagging.FR;
using fiskaltrust.Interface.Tagging.Interfaces;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
namespace fiskaltrust.Interface.Tagging.UnitTests.FR
{
    public class CaseConverterFRTests
    {
        private readonly ICaseConverter _caseConverterFR;
        public CaseConverterFRTests()
        {
            _caseConverterFR = new CaseConverterFR();
        }

        [Theory]
        [InlineData(0x4652200000000000, 0x4652000000000000)]
        [InlineData(0x4652200000000003, 0x4652000000000003)]
        [InlineData(0x4652200000000001, 0x4652000000000001)]
        [InlineData(0x4652200000000002, 0x4652000000000002)]
        [InlineData(0x4652200000000004, 0x4652000000000004)]
        [InlineData(0x4652200000000005, null)]
        [InlineData(0x4652200000000007, 0x4652000000000005)]
        [InlineData(0x4652200000000008, null)]
        [InlineData(0x4652200000005000, 0x4652000000000006)]
        [InlineData(0x4652200000000060, 0x4652000000000007)]
        [InlineData(0x4652200000000013, 0x465200000000000A)]
        [InlineData(0x4652200000000011, 0x4652000000000008)]
        [InlineData(0x4652200000000012, 0x4652000000000009)]
        [InlineData(0x4652200000000014, 0x465200000000000B)]
        [InlineData(0x4652200000000015, null)]
        [InlineData(0x4652200000000018, null)]
        [InlineData(0x4652200000000017, 0x465200000000000C)]
        [InlineData(0x4652200000000010, null)]
        [InlineData(0x4652200000000023, 0x465200000000000F)]
        [InlineData(0x4652200000000021, 0x465200000000000D)]
        [InlineData(0x4652200000000022, 0x465200000000000E)]
        [InlineData(0x4652200000000024, 0x4652000000000010)]
        [InlineData(0x4652200000000025, null)]
        [InlineData(0x4652200000000028, null)]
        [InlineData(0x4652200000000027, 0x4652000000000011)]
        [InlineData(0x4652200000000020, null)]
        [InlineData(0x4652200000000033, null)]
        [InlineData(0x4652200000000031, null)]
        [InlineData(0x4652200000000032, null)]
        [InlineData(0x4652200000000034, null)]
        [InlineData(0x4652200000000035, null)]
        [InlineData(0x4652200000000037, null)]
        [InlineData(0x4652200000000030, null)]
        [InlineData(0x4652200000000043, null)]
        [InlineData(0x4652200000000041, null)]
        [InlineData(0x4652200000000042, null)]
        [InlineData(0x4652200000000044, null)]
        [InlineData(0x4652200000000045, null)]
        [InlineData(0x4652200000000047, null)]
        [InlineData(0x4652200000000040, null)]
        [InlineData(0x4652200000000048, null)]
        [InlineData(0x4652200000000053, 0x4652000000000014)]
        [InlineData(0x4652200000000051, 0x4652000000000012)]
        [InlineData(0x4652200000000052, 0x4652000000000013)]
        [InlineData(0x4652200000000054, 0x4652000000000015)]
        [InlineData(0x4652200000000055, null)]
        [InlineData(0x4652200000000058, null)]
        [InlineData(0x4652200000000057, 0x4652000000000016)]
        [InlineData(0x4652200000000050, null)]
        [InlineData(0x4652200000000068, 0x4652000000000021)]
        [InlineData(0x4652200000000073, 0x4652000000000019)]
        [InlineData(0x4652200000000071, 0x4652000000000017)]
        [InlineData(0x4652200000000072, 0x4652000000000018)]
        [InlineData(0x4652200000000074, 0x465200000000001A)]
        [InlineData(0x4652200000000075, null)]
        [InlineData(0x4652200000000078, null)]
        [InlineData(0x4652200000000077, 0x465200000000001B)]
        [InlineData(0x4652200000000070, null)]
        [InlineData(0x4652200000000083, null)]
        [InlineData(0x4652200000000081, null)]
        [InlineData(0x4652200000000082, null)]
        [InlineData(0x4652200000000084, null)]
        [InlineData(0x4652200000000085, null)]
        [InlineData(0x4652200000000088, null)]
        [InlineData(0x4652200000000087, null)]
        [InlineData(0x4652200000000080, null)]
        [InlineData(0x4652200000000093, null)]
        [InlineData(0x4652200000000091, null)]
        [InlineData(0x4652200000000092, null)]
        [InlineData(0x4652200000000094, null)]
        [InlineData(0x4652200000000095, null)]
        [InlineData(0x4652200000000098, null)]
        [InlineData(0x4652200000000097, null)]
        [InlineData(0x4652200000000090, 0x4652000000000022)]
        [InlineData(0x46522000000000A8, null)]
        [InlineData(0x4652200000080000, null)]
        [InlineData(0x4652200000080003, 0x465200000000001E)]
        [InlineData(0x4652200000080001, 0x465200000000001C)]
        [InlineData(0x4652200000080002, 0x465200000000001D)]
        [InlineData(0x4652200000080004, 0x465200000000001F)]
        [InlineData(0x4652200000080005, null)]
        [InlineData(0x4652200000080007, 0x4652000000000020)]
        [InlineData(0x4652200000080008, null)]
        public void ConvertftChargeItemCaseToV1_ShouldreturnCorrect(long v2FtChargeItemCase, long? v1FtChargeItemCase)
        {
            var chargeItem = new ChargeItem { ftChargeItemCase = v2FtChargeItemCase };

            if (v1FtChargeItemCase == null)
            {
                Assert.Throws<NotImplementedException>(() => _caseConverterFR.ConvertftChargeItemCaseToV1(chargeItem));
            }
            else
            {
                _caseConverterFR.ConvertftChargeItemCaseToV1(chargeItem);
                chargeItem.ftChargeItemCase.Should().Be(v1FtChargeItemCase);
            }
        }

        [Theory]
        [InlineData(0x4652200000000000, 0x4652000000000000)]
        [InlineData(0x4652200000400001, 0x4652000000000012)]
        [InlineData(0x4652200000100001, 0x4652000000000002)]
        [InlineData(0x4652200000000001, 0x4652000000000001)]
        [InlineData(0x4652200000000002, null)]
        [InlineData(0x4652200000000003, 0x4652000000000003)]
        [InlineData(0x4652200000000004, 0x4652000000000004)]
        [InlineData(0x4652200000000005, 0x4652000000000005)]
        [InlineData(0x4652200000000006, 0x4652000000000006)]
        [InlineData(0x4652200000000007, 0x4652000000000007)]
        [InlineData(0x4652200000000008, 0x4652000000000008)]
        [InlineData(0x4652200000080009, 0x4652000000000010)]
        [InlineData(0x4652200000000009, 0x465200000000000B)]
        [InlineData(0x465220000000000A, 0x465200000000000C)]
        [InlineData(0x465220000000000B, 0x465200000000000D)]
        [InlineData(0x465220000000000C, 0x465200000000000E)]
        [InlineData(0x465220000000000D, 0x4652000000000011)]
        [InlineData(0x465220000000000E, null)]
        [InlineData(0x465220000000000F, null)]
        public void ConvertftPayItemCaseToV1_ShouldreturnCorrect(long v2FtPayItemCase, long? v1FtPayItemCase)
        {
            var payItem = new PayItem { ftPayItemCase = v2FtPayItemCase };
           
            if(v1FtPayItemCase == null)
            {
                Assert.Throws<NotImplementedException>(() => _caseConverterFR.ConvertftPayItemCaseToV1(payItem));
            }
            else
            {
                _caseConverterFR.ConvertftPayItemCaseToV1(payItem);
                payItem.ftPayItemCase.Should().Be(v1FtPayItemCase);
            }            
        }

        [Theory]
        [InlineData(0x4652200080020000, 0x4652800000020000)]
        [InlineData(0x4652200080040001, 0x4652800000040001)]
        [InlineData(0x4652200000010002, 0x465200000001000C)]
        [InlineData(0x4652200080000003, null)]
        [InlineData(0x4652200000000004, null)]
        [InlineData(0x4652200000000005, 0x4652000000000009)]
        [InlineData(0x4652200000001000, 0x4652000000000003)]
        [InlineData(0x4652200000001001, null)]
        [InlineData(0x4652200000001002, null)]
        [InlineData(0x4652200000001003, null)]
        [InlineData(0x4652200000002000, 0x465200000000000F)]
        [InlineData(0x4652200000002001, null)]
        [InlineData(0x4652200000002010, 0x4652000000000004)]
        [InlineData(0x4652200000002011, 0x4652000000000005)]
        [InlineData(0x4652200000002012, 0x4652000000000006)]
        [InlineData(0x4652200000002013, 0x4652000000000007)]
        [InlineData(0x4652200000003000, 0x4652000000000014)]
        [InlineData(0x4652200000003001, 0x4652000000000012)]
        [InlineData(0x4652200000003002, 0x4652000000000013)]
        [InlineData(0x4652200000003003, 0x465200000000000D)]
        [InlineData(0x4652200000003004, null)]
        [InlineData(0x4652200000003005, null)]
        [InlineData(0x4652200000003010, null)]
        [InlineData(0x4652200000004001, 0x4652000000000010)]
        [InlineData(0x4652200000004002, 0x4652000000000011)]
        [InlineData(0x4652200000004011, null)]
        [InlineData(0x4652200000004012, null)]
        public void ConvertftReceiptCaseToV1_ShouldreturnCorrect(long v2FtReceiptCase, long? v1FtReceiptCase)
        {
            var request = new ReceiptRequest { ftReceiptCase = v2FtReceiptCase };

            if (v1FtReceiptCase == null)
            {
                Assert.Throws<NotImplementedException>(() => _caseConverterFR.ConvertftReceiptCaseToV1(request));
            }
            else
            {
                _caseConverterFR.ConvertftReceiptCaseToV1(request);
                request.ftReceiptCase.Should().Be(v1FtReceiptCase);
            }
        }

        [Theory]
        [InlineData(0x4652000000000000, 0x4652200000000000)]
        [InlineData(0x4652000000000001, 0x4652200000000001)]
        [InlineData(0x4652000000000040, 0x4652200000000040)]
        [InlineData(0x4652000000000008, 0x4652200000000008)]
        public void ConvertftStateToV2_ShouldreturnCorrect(long v1FtState, long? v2FtState)
        {
            var response = new ReceiptResponse { ftState = v1FtState };

            if (v2FtState == null)
            {
                Assert.Throws<NotImplementedException>(() => _caseConverterFR.ConvertftStateToV2(response));
            }
            else
            {
                _caseConverterFR.ConvertftStateToV2(response);
                response.ftState.Should().Be(v2FtState);
            }
        }

        [Theory]
        [InlineData(0x4652000000000000, 0x4652200000000000)]
        [InlineData(0x4652000000000001, 0x4652200000000001)]
        [InlineData(0x4652000000000002, 0x4652200000000010)]
        [InlineData(0x4652000000000003, 0x4652200000000011)]
        [InlineData(0x4652000000000004, 0x4652200000000012)]
        [InlineData(0x4652000000000005, 0x4652200000000013)]
        [InlineData(0x4652000000000006, 0x4652200000000014)]
        [InlineData(0x4652000000000007, 0x4652200000000015)]
        public void ConvertftSignatureTypeToV2_ShouldreturnCorrect(long v1FtSignaturType, long? v2FtSignaturType)
        {
            var signaturItem = new SignaturItem { ftSignatureType = v1FtSignaturType };

            if (v2FtSignaturType == null)
            {
                Assert.Throws<NotImplementedException>(() => _caseConverterFR.ConvertftSignatureTypeToV2(signaturItem));
            }
            else
            {
                _caseConverterFR.ConvertftSignatureTypeToV2(signaturItem);
                signaturItem.ftSignatureType.Should().Be(v2FtSignaturType);
            }
        }

        [Theory]
        [InlineData(0x00000, 0x00000)]
        [InlineData(0x00001, 0x00001)]
        [InlineData(0x00002, 0x00002)]
        [InlineData(0x00003, 0x00003)]
        [InlineData(0x00004, 0x00004)]
        [InlineData(0x00005, 0x00005)]
        [InlineData(0x00006, 0x00006)]
        [InlineData(0x00007, 0x00007)]
        [InlineData(0x00008, 0x00008)]
        [InlineData(0x00009, 0x00009)]
        [InlineData(0x0000A, 0x0000A)]
        [InlineData(0x0000B, 0x0000B)]
        [InlineData(0x0000C, 0x0000C)]
        [InlineData(0x0000D, 0x0000D)]
        [InlineData(0x0000E, null)]
        public void ConvertftSignatureFormatToV2_ShouldreturnCorrect(long v1FtSignatureFormat, long? v2FtSignatureFormat)
        {
            var signatureItem = new SignaturItem { ftSignatureFormat = v1FtSignatureFormat };

            if (v2FtSignatureFormat == null)
            {
                Assert.Throws<NotImplementedException>(() => _caseConverterFR.ConvertftSignatureFormatToV2(signatureItem));
            }
            else
            {
                _caseConverterFR.ConvertftSignatureFormatToV2(signatureItem);
                signatureItem.ftSignatureFormat.Should().Be(v2FtSignatureFormat);
            }
        }

        [Theory]
        [InlineData(0x4652200000000000, null)]
        [InlineData(0x4652200000000001, null)]
        [InlineData(0x4652200000000002, null)]
        [InlineData(0x4652200000000003, null)]
        [InlineData(0x4652200000001000, 0x4652000000000000)]
        [InlineData(0x4652200000011001, 0x4652000000010001)]
        [InlineData(0x4652200000001002, 0x4652000000000002)]
        [InlineData(0x4652200000001003, 0x4652000000000003)]
        [InlineData(0x4652200000001004, 0x4652000000000004)]
        [InlineData(0x4652200000011007, 0x4652000000010007)]
        [InlineData(0x4652200000001008, 0x4652000000000008)]
        [InlineData(0x4652200000001009, 0x4652000000000009)]
        [InlineData(0x465220000000100A, 0x465200000000000A)]
        [InlineData(0x465220000000100B, 0x465200000000000B)]
        [InlineData(0x4652200000001010, 0x4652000000000010)]
        public void ConvertftJournalTypeToV1_ShouldreturnCorrect(long v2FtJournalType, long? v1FtJournalType)
        {
            var journalRequest = new JournalRequest { ftJournalType = v2FtJournalType };

            if (v1FtJournalType == null)
            {
                Assert.Throws<NotImplementedException>(() => _caseConverterFR.ConvertftJournalTypeToV1(journalRequest));
            }
            else
            {
                _caseConverterFR.ConvertftJournalTypeToV1(journalRequest);
                journalRequest.ftJournalType.Should().Be(v1FtJournalType);
            }
        }
    }
}