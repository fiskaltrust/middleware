using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers;

public class ChargeItemFactory
{
    private readonly Dictionary<ChargeItemCase, decimal> _vatRates;
    public ChargeItemFactory(Dictionary<ChargeItemCase, decimal> vatRates)
    {
        _vatRates = vatRates;
    }

    public ChargeItemBuilder Builder { get => new ChargeItemBuilder(_vatRates); }

    public class ChargeItemBuilder
    {
        private readonly Dictionary<ChargeItemCase, decimal> _vatRates;
        private ChargeItem _chargeItem { get; set; } = new();

        internal ChargeItemBuilder(Dictionary<ChargeItemCase, decimal> vatRates)
        {
            _vatRates = vatRates;
        }

        public ChargeItemBuilder WithDescription(string description)
        {
            _chargeItem.Description = description;
            return this;
        }

        public ChargeItemBuilder WithQuantity(decimal quantity)
        {
            _chargeItem.Quantity = quantity;
            return this;
        }

        public ChargeItemBuilder WithAmount(decimal amount)
        {
            _chargeItem.Amount = amount;
            return this;
        }

        public ChargeItemBuilder WithCase(ChargeItemCase chargeItemCase)
        {
            _chargeItem.ftChargeItemCase = chargeItemCase;
            return this;
        }

        public ChargeItem Build()
        {
            _chargeItem.VATRate = _vatRates[_chargeItem.ftChargeItemCase.Vat()];
            _chargeItem.VATAmount = _chargeItem.Amount * _chargeItem.VATRate / (_chargeItem.VATRate + 1);
            return _chargeItem;
        }
    }
}