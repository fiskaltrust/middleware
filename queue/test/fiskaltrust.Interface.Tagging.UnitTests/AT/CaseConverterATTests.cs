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

    [Fact]
    public void Chargeitem_ShouldReturnCorrect()
    {
        var chargeItem = new ChargeItem { ftChargeItemCase = 0x4154200000000000 };
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000000);

        chargeItem.ftChargeItemCase = 0x4154200000000003;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000003);

        chargeItem.ftChargeItemCase = 0x4154200000000001;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000001);

        chargeItem.ftChargeItemCase = 0x4154200000000002;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000002);

        chargeItem.ftChargeItemCase = 0x4154200000000004;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000004);

        chargeItem.ftChargeItemCase = 0x4154200000000007;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000005);

        chargeItem.ftChargeItemCase = 0x4154200000500000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000006);

        chargeItem.ftChargeItemCase = 0x4154200000060000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000007);

        chargeItem.ftChargeItemCase = 0x4154200000013000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x415400000000000A);

        chargeItem.ftChargeItemCase = 0x4154200000011000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000008);

        chargeItem.ftChargeItemCase = 0x4154200000012000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000009);

        chargeItem.ftChargeItemCase = 0x4154200000014000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x415400000000000B);

        chargeItem.ftChargeItemCase = 0x4154200000017000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x415400000000000C);

        chargeItem.ftChargeItemCase = 0x4154200000023000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x415400000000000F);

        chargeItem.ftChargeItemCase = 0x4154200000021000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x415400000000000D);

        chargeItem.ftChargeItemCase = 0x4154200000022000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x415400000000000E);

        chargeItem.ftChargeItemCase = 0x4154200000024000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000010);

        chargeItem.ftChargeItemCase = 0x4154200000027000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000011);

        chargeItem.ftChargeItemCase = 0x4154200000053000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000014);

        chargeItem.ftChargeItemCase = 0x4154200000051000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000012);

        chargeItem.ftChargeItemCase = 0x4154200000052000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000013);

        chargeItem.ftChargeItemCase = 0x4154200000054000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000015);

        chargeItem.ftChargeItemCase = 0x4154200000057000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000016);

        chargeItem.ftChargeItemCase = 0x4154200000073000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000019);

        chargeItem.ftChargeItemCase = 0x4154200000071000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000017);

        chargeItem.ftChargeItemCase = 0x4154200000072000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000018);

        chargeItem.ftChargeItemCase = 0x4154200000074000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x415400000000001A);

        chargeItem.ftChargeItemCase = 0x4154200000077000;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x415400000000001B);

        chargeItem.ftChargeItemCase = 0x4154200008000003;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x415400000000001E);

        chargeItem.ftChargeItemCase = 0x4154200008000001;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x415400000000001C);

        chargeItem.ftChargeItemCase = 0x4154200008000002;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x415400000000001D);

        chargeItem.ftChargeItemCase = 0x4154200008000004;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x415400000000001F);

        chargeItem.ftChargeItemCase = 0x4154200008000007;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000020);

        chargeItem.ftChargeItemCase = 0x4154200000000068;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000021);

        chargeItem.ftChargeItemCase = 0x4154200000000090;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000022);

        chargeItem.ftChargeItemCase = 0x4154200000000098;
        _caseConverterAT.ConvertftChargeItemCaseToV1(chargeItem);
        chargeItem.ftChargeItemCase.Should().Be(0x4154000000000023);
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
    
    // Test conversion for each signature format
    foreach (var format in Enum.GetValues(typeof(ftSignatureFormats)))
    {
        signature = new SignaturItem { ftSignatureFormat = 0x4154000000000000 | (long)format };
        _caseConverterAT.ConvertftSignatureFormatToV2(signature);
        signature.ftSignatureFormat.Should().Be((0x4154200000000000 | (long)format));
    }
}


        [Fact]
        public void ConvertftReceiptState_ShouldreturnCorrect()
        {
            var request = new ReceiptResponse { ftState = 0x4154_0000_0000_0100 };
            _caseConverterAT.ConvertftStateToV2(request);
            
            request.ftState.Should().Be(0x4154200000010000);
        }
    } }