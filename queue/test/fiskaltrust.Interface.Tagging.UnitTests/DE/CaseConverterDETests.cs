﻿using fiskaltrust.Interface.Tagging.DE;
using fiskaltrust.Interface.Tagging.Interfaces;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Models.V2;

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
            var request = new ReceiptRequest { ftReceiptCase = 0x4445200000010002 };
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100010011);
            request.ftReceiptCase = 0x4445200000000000;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000000);
            request.ftReceiptCase = 0x4445200000000001;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000001);
            request.ftReceiptCase = 0x4445200000000002;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000011);
            request.ftReceiptCase = 0x4445200000000005;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x444500010000000F);
            request.ftReceiptCase = 0x4445200000001002;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x444500010000000C);
            request.ftReceiptCase = 0x4445200000001001;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x444500010000000D);
            request.ftReceiptCase = 0x4445200000002000;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000002);
            request.ftReceiptCase = 0x4445200000002011;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000007);
            request.ftReceiptCase = 0x4445200000002012;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000005);
            request.ftReceiptCase = 0x4445200000002013;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000006);
            request.ftReceiptCase = 0x4445200000003000;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000014);
            request.ftReceiptCase = 0x4445200000003003;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000012);
            request.ftReceiptCase = 0x4445200000003004;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000010);
            request.ftReceiptCase = 0x4445200000004001;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000003);
            request.ftReceiptCase = 0x4445200000004002;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000004);
            request.ftReceiptCase = 0x4445200000004011;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000017);
            request.ftReceiptCase = 0x4445200000004012;
            _caseConverterDE.ConvertftReceiptCaseToV1(request);
            request.ftReceiptCase.Should().Be(0x4445000100000018);
        }

        [Fact]
        public void Chargeitem_ShouldreturnCorrect()
        {
            var chargeItem = new ChargeItem { ftChargeItemCase = 0x4445200000000003 };
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000001);
            chargeItem.ftChargeItemCase=0x4445200000000001;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000002);
            chargeItem.ftChargeItemCase = 0x4445200000000004;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000003);
            chargeItem.ftChargeItemCase = 0x4445200000000005;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000004);
            chargeItem.ftChargeItemCase = 0x4445200000000008;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000005);
            chargeItem.ftChargeItemCase = 0x4445200000000007;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000006);
            chargeItem.ftChargeItemCase = 0x4445200000005000;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x44450000000000A1);
            chargeItem.ftChargeItemCase = 0x4445200000000060;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x44450000000000A2);
            chargeItem.ftChargeItemCase = 0x4445200000000013;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000011);
            chargeItem.ftChargeItemCase = 0x4445200000000011;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000012);
            chargeItem.ftChargeItemCase = 0x4445200000000014;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000013);
            chargeItem.ftChargeItemCase = 0x4445200000000015;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000014);
            chargeItem.ftChargeItemCase = 0x4445200000000018;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000015);
            chargeItem.ftChargeItemCase = 0x4445200000000017;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000016);
            chargeItem.ftChargeItemCase = 0x4445200000000010;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000017);
            chargeItem.ftChargeItemCase = 0x4445200000000023;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000019);
            chargeItem.ftChargeItemCase = 0x4445200000000021;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000001A);
            chargeItem.ftChargeItemCase = 0x4445200000000024;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000001B);
            chargeItem.ftChargeItemCase = 0x4445200000000025;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000001C);
            chargeItem.ftChargeItemCase = 0x4445200000000028;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000001D);
            chargeItem.ftChargeItemCase = 0x4445200000000027;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000001E);
            chargeItem.ftChargeItemCase = 0x4445200000000020;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000001F);
            chargeItem.ftChargeItemCase = 0x4445200000000025;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000001C);
            chargeItem.ftChargeItemCase = 0x4445200000100003;
            chargeItem.Amount = -5;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000021);
            chargeItem.ftChargeItemCase = 0x4445200000100001;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000022);
            chargeItem.ftChargeItemCase = 0x4445200000100004;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000023);
            chargeItem.ftChargeItemCase = 0x4445200000100005;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000024);
            chargeItem.ftChargeItemCase = 0x4445200000100008;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000025);
            chargeItem.ftChargeItemCase = 0x4445200000100007;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000026);
            chargeItem.ftChargeItemCase = 0x4445200000100000;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000027);
            chargeItem.Amount = 5;
            chargeItem.ftChargeItemCase = 0x4445200000100003;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000029);
            chargeItem.ftChargeItemCase = 0x4445200000100001;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000002A);
            chargeItem.ftChargeItemCase = 0x4445200000100004;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000002B);
            chargeItem.ftChargeItemCase = 0x4445200000100005;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000002C);
            chargeItem.ftChargeItemCase = 0x4445200000100008;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000002D);
            chargeItem.ftChargeItemCase = 0x4445200000100007;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000002E);
            chargeItem.ftChargeItemCase = 0x4445200000100000;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000002F);
            chargeItem.Amount = -5;
            chargeItem.ftChargeItemCase = 0x4445200000040003;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000031);
            chargeItem.ftChargeItemCase = 0x4445200000040001;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000032);
            chargeItem.ftChargeItemCase = 0x4445200000040004;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000033);
            chargeItem.ftChargeItemCase = 0x4445200000040005;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000034);
            chargeItem.ftChargeItemCase = 0x4445200000040008;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000035);
            chargeItem.ftChargeItemCase = 0x4445200000040007;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000036);
            chargeItem.ftChargeItemCase = 0x4445200000040000;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000037);
            chargeItem.Amount = 5;
            chargeItem.ftChargeItemCase = 0x4445200000040003;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000039);
            chargeItem.ftChargeItemCase = 0x4445200000040001;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000003A);
            chargeItem.ftChargeItemCase = 0x4445200000040004;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000003B);
            chargeItem.ftChargeItemCase = 0x4445200000040005;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000003C);
            chargeItem.ftChargeItemCase = 0x4445200000040008;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000003D);
            chargeItem.ftChargeItemCase = 0x4445200000040007;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000003E);
            chargeItem.ftChargeItemCase = 0x4445200000040000;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000003F);
            chargeItem.ftChargeItemCase = 0x4445200000000083;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000041);
            chargeItem.ftChargeItemCase = 0x4445200000000081;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000042);
            chargeItem.ftChargeItemCase = 0x4445200000000084;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000043);
            chargeItem.ftChargeItemCase = 0x4445200000000085;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000044);
            chargeItem.ftChargeItemCase = 0x4445200000000087;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000046);
            chargeItem.ftChargeItemCase = 0x4445200000000080;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000047);
            chargeItem.ftChargeItemCase = 0x4445200000000088;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000049);
            chargeItem.ftChargeItemCase = 0x4445200000000033;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000051);
            chargeItem.ftChargeItemCase = 0x4445200000000031;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000052);
            chargeItem.ftChargeItemCase = 0x4445200000000034;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000053);
            chargeItem.ftChargeItemCase = 0x4445200000000035;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000054);
            chargeItem.ftChargeItemCase = 0x4445200000000038;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000059);
            chargeItem.ftChargeItemCase = 0x4445200000000037;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000056);
            chargeItem.ftChargeItemCase = 0x4445200000000030;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000057);
            chargeItem.ftChargeItemCase = 0x4445200000000043;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000061);
            chargeItem.ftChargeItemCase = 0x4445200000000041;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000062);
            chargeItem.ftChargeItemCase = 0x4445200000000044;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000063);
            chargeItem.ftChargeItemCase = 0x4445200000000045;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000064);
            chargeItem.ftChargeItemCase = 0x4445200000000048;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000060);
            chargeItem.ftChargeItemCase = 0x4445200000000047;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000066);
            chargeItem.Amount = -5;
            chargeItem.ftChargeItemCase = 0x4445200000000040;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000006F);
            chargeItem.ftChargeItemCase = 0x4445200000000043;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000069);
            chargeItem.ftChargeItemCase = 0x4445200000000041;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000006A);
            chargeItem.ftChargeItemCase = 0x4445200000000044;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000006B);
            chargeItem.ftChargeItemCase = 0x4445200000000045;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000006C);
            chargeItem.ftChargeItemCase = 0x4445200000000047;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000006E);
            chargeItem.ftChargeItemCase = 0x4445200000000093;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000071);
            chargeItem.ftChargeItemCase = 0x4445200000000091;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000072);
            chargeItem.ftChargeItemCase = 0x4445200000000094;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000073);
            chargeItem.ftChargeItemCase = 0x4445200000000095;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000074);
            chargeItem.ftChargeItemCase = 0x4445200000000098;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000075);
            chargeItem.ftChargeItemCase = 0x4445200000000097;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000076);
            chargeItem.ftChargeItemCase = 0x4445200000000090;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000077);
            chargeItem.Amount = 5;
            chargeItem.ftChargeItemCase = 0x4445200000000093;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000079);
            chargeItem.ftChargeItemCase = 0x4445200000000091;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000007A);
            chargeItem.ftChargeItemCase = 0x4445200000000094;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000007B);
            chargeItem.ftChargeItemCase = 0x4445200000000095;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000007C);
            chargeItem.ftChargeItemCase = 0x4445200000000098;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000007D);
            chargeItem.ftChargeItemCase = 0x4445200000000097;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000007E);
            chargeItem.ftChargeItemCase = 0x4445200000000090;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000007F);
            chargeItem.ftChargeItemCase = 0x4445200000080003;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000081);
            chargeItem.ftChargeItemCase = 0x4445200000080001;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000082);
            chargeItem.ftChargeItemCase = 0x4445200000080004;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000083);
            chargeItem.ftChargeItemCase = 0x4445200000080005;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000084);
            chargeItem.ftChargeItemCase = 0x4445200000080008;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000085);
            chargeItem.ftChargeItemCase = 0x4445200000080007;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000086);
            chargeItem.ftChargeItemCase = 0x4445200000080000;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000087);
            chargeItem.Amount = -5;
            chargeItem.ftChargeItemCase = 0x4445200000080003;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000089);
            chargeItem.ftChargeItemCase = 0x4445200000080001;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000008A);
            chargeItem.ftChargeItemCase = 0x4445200000080004;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000008B);
            chargeItem.ftChargeItemCase = 0x4445200000080005;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000008C);
            chargeItem.ftChargeItemCase = 0x4445200000080008;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000008D);
            chargeItem.ftChargeItemCase = 0x4445200000080007;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000008E);
            chargeItem.ftChargeItemCase = 0x4445200000080000;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x444500000000008F);
            chargeItem.ftChargeItemCase = 0x44452000000000A8;
            _caseConverterDE.ConvertftChargeItemCaseToV1(chargeItem);
            chargeItem.ftChargeItemCase.Should().Be(0x4445000000000097);

        }


        [Fact]
        public void Payitem_ShouldreturnCorrect()
        {
            var payItem = new PayItem { ftPayItemCase = 0x4445200000000000 };
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4445000000000000);
            payItem.ftPayItemCase = 0x4445200000000001;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4445000000000001);
            payItem.ftPayItemCase = 0x4445200000100001;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4445000000000002);
            payItem.ftPayItemCase = 0x4445200000000003;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4445000000000003);
            payItem.ftPayItemCase = 0x4445200000800004;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4445000000000004);
            payItem.ftPayItemCase = 0x4445200000800005;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4445000000000005);
            payItem.ftPayItemCase = 0x4445200000000006;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x444500000000000D);
            payItem.ftPayItemCase = 0x4445200000800007;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4445000000000006);
            payItem.ftPayItemCase = 0x4445200000800008;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4445000000000007);
            payItem.ftPayItemCase = 0x4445200000000009;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x444500000000000E);
            payItem.ftPayItemCase = 0x444520000080000A;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4445000000000008);
            payItem.ftPayItemCase = 0x444520000080000B;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4445000000000009);
            payItem.ftPayItemCase = 0x4445200000800009;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x444500000000000F);
            payItem.ftPayItemCase = 0x444520000000000D;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x444500000000000A);
            payItem.ftPayItemCase = 0x4445200000400001;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4445000000000010);
            payItem.ftPayItemCase = 0x4445200000200001;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x444500000000000B);
            payItem.ftPayItemCase = 0x444520000000000E;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4445000000000011);
            payItem.ftPayItemCase = 0x444520000000000C;
            _caseConverterDE.ConvertftPayItemCaseToV1(payItem);
            payItem.ftPayItemCase.Should().Be(0x4445000000000014);
        }

        [Fact]
        public void JournalTypes_ShouldreturnCorrect()
        {
            var journal = new JournalRequest { ftJournalType = 0x4445200000001000 };
            _caseConverterDE.ConvertftJournalTypeToV1(journal);
            journal.ftJournalType.Should().Be(0x4445000000000000);
            journal.ftJournalType = 0x4445200000001001;
            _caseConverterDE.ConvertftJournalTypeToV1(journal);
            journal.ftJournalType.Should().Be(0x4445000000000001);
            journal.ftJournalType = 0x4445200000001002;
            _caseConverterDE.ConvertftJournalTypeToV1(journal);
            journal.ftJournalType.Should().Be(0x4445000000000002);
            journal.ftJournalType = 0x4445200000001003;
            _caseConverterDE.ConvertftJournalTypeToV1(journal);
            journal.ftJournalType.Should().Be(0x4445000000000003);
        }
    }
}