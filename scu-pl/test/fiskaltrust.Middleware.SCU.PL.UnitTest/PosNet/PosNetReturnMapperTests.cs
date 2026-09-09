using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

public class PosNetReturnMapperTests
{
    private const ulong Goods23 = 0x504C_2000_0000_0003;
    private const ulong Cash = 0x504C_2000_0000_0001;

    private static ReceiptRequest Return(params (decimal Amount, ulong Case)[] items) => new()
    {
        ftReceiptCase = (ReceiptCase)(0x504C_2000_0000_0001UL | (ulong)ReceiptCaseFlags.Refund),
        cbChargeItems = items.Select(i => new ChargeItem { Description = "Woda", Amount = i.Amount, Quantity = -1m, ftChargeItemCase = (ChargeItemCase)i.Case }).ToList(),
        cbPayItems = [new PayItem { Description = "Gotówka", Amount = items.Sum(i => i.Amount), ftPayItemCase = (PayItemCase)Cash }],
    };

    [Fact]
    public void MapReturn_SumsTheReturnedPositions_IntoOneGoodsReturn()
    {
        var (amount, command) = PosNetReturnMapper.MapReturn(Return((-3.69m, Goods23), (-1.31m, Goods23)));

        amount.Should().Be(500);
        command.Mnemonic.Should().Be("stocash");
        command.Parameters.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, string>("kw", "500"));
    }

    [Fact]
    public void MapReturn_ReadsARefundFlaggedPosition_BySign()
    {
        // A POS may flag the position instead of negating it; the flag says what it is.
        var (amount, _) = PosNetReturnMapper.MapReturn(new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)(0x504C_2000_0000_0001UL | (ulong)ReceiptCaseFlags.Refund),
            cbChargeItems = [new ChargeItem { Description = "Woda", Amount = 3.69m, Quantity = 1m, ftChargeItemCase = (ChargeItemCase)(Goods23 | (ulong)ChargeItemCaseFlags.Refund) }],
            cbPayItems = [new PayItem { Description = "Gotówka", Amount = -3.69m, ftPayItemCase = (PayItemCase)Cash }],
        });

        amount.Should().Be(369);
    }

    [Fact]
    public void MapReturn_WithASalePosition_Refuses()
    {
        var act = () => PosNetReturnMapper.MapReturn(Return((-3.69m, Goods23), (2.00m, Goods23)));

        act.Should().Throw<PLValidationException>().WithMessage("*sale position*");
    }

    [Fact]
    public void MapReturn_WhosePaymentsDoNotHandBackTheValue_Refuses()
    {
        var request = Return((-3.69m, Goods23));
        request.cbPayItems[0].Amount = -3.00m;

        var act = () => PosNetReturnMapper.MapReturn(request);

        act.Should().Throw<PLValidationException>().WithMessage("*3.00*3.69*");
    }

    [Fact]
    public void MapReturn_WithoutAnyPosition_Refuses()
    {
        var act = () => PosNetReturnMapper.MapReturn(new ReceiptRequest { ftReceiptCase = (ReceiptCase)(0x504C_2000_0000_0001UL | (ulong)ReceiptCaseFlags.Refund) });

        act.Should().Throw<PLValidationException>().WithMessage("*no returned position*");
    }
}
