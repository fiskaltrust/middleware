using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

/// <summary>
/// Builds the receipts the mapping tests send through <see cref="PosNetReceiptMapper"/> and renders
/// what comes out: one place for how a PL charge item case is put together (country, version, the
/// VAT case, the flags), shared by the discount and the reversal tests.
/// </summary>
internal static class MappingFixture
{
    /// <summary>23%, PTU slot A of the default table.</summary>
    public const long NormalRate = 0x0003;

    /// <summary>8%, PTU slot B.</summary>
    public const long ReducedRate = 0x0001;

    /// <summary>UnknownService — the POS sent no VAT case at all.</summary>
    public const long NoRate = 0x0000;

    private const long VoidFlag = 0x0001_0000;
    private const long RefundFlag = 0x0002_0000;
    private const long ExtraOrDiscountFlag = 0x0004_0000;

    /// <summary>Maps a cash sale; <paramref name="paidInCash"/> 0 sends no payment at all.</summary>
    public static IReadOnlyList<PosNetCommand> MapSale(List<ChargeItem> chargeItems, decimal paidInCash)
        => PosNetReceiptMapper.MapSale(
            new ReceiptRequest
            {
                ftReceiptCase = (ReceiptCase)0x504C_2000_0000_0001,
                Currency = Currency.PLN,
                cbChargeItems = chargeItems,
                cbPayItems = paidInCash == 0m
                    ? []
                    : [new PayItem { Description = "Gotówka", Amount = paidInCash, ftPayItemCase = (PayItemCase)0x504C_2000_0000_0001, Currency = Currency.PLN }],
            },
            new PtuSlotResolver(PosNetConfiguration.DefaultVatRateTable()));

    public static ChargeItem Position(string description, decimal amount, decimal quantity = 1m, decimal position = 0m, long vatCase = NormalRate)
        => Item(description, amount, quantity, position, vatCase, flags: 0);

    public static ChargeItem Modifier(string description, decimal amount, decimal position = 0m, long vatCase = NormalRate)
        => Item(description, amount, quantity: 1m, position, vatCase, flags: ExtraOrDiscountFlag);

    public static ChargeItem Voided(string description, decimal amount, decimal quantity = 0m, decimal position = 0m, long vatCase = NormalRate)
        => Item(description, amount, quantity, position, vatCase, flags: VoidFlag);

    public static ChargeItem VoidedModifier(string description, decimal amount)
        => Item(description, amount, quantity: 1m, position: 0m, NormalRate, flags: VoidFlag | ExtraOrDiscountFlag);

    public static ChargeItem Refunded(string description, decimal amount)
        => Item(description, amount, quantity: 1m, position: 0m, NormalRate, flags: RefundFlag);

    private static ChargeItem Item(string description, decimal amount, decimal quantity, decimal position, long vatCase, long flags) => new()
    {
        Description = description,
        Amount = amount,
        Quantity = quantity,
        Position = position,
        ftChargeItemCase = (ChargeItemCase)(0x504C_2000_0000_0010 | flags | vatCase),
        Currency = Currency.PLN,
    };

    /// <summary>Each command as one line: the mnemonic followed by its parameters, key and value joined.</summary>
    public static List<string> Render(IReadOnlyList<PosNetCommand> commands)
        => commands.Select(c => string.Join(' ', new[] { c.Mnemonic }.Concat(c.Parameters.Select(p => $"{p.Key}{p.Value}")))).ToList();
}
