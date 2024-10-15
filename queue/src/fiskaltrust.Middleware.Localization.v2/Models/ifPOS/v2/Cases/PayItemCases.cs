namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum PayItemCases : long
{
    UnknownPaymentType = 0x00,
    CashPayment = 0x01,
    NonCash = 0x02,
    CrossedCheque = 0x03,
    DebitCardPayment = 0x04,
    CreditCardPayment = 0x05,
    VoucherPaymentCouponVoucherByMoneyValue = 0x06,
    OnlinePayment = 0x07,
    LoyaltyProgramCustomerCardPayment = 0x08,
    AccountsReceivable = 0x09,
    SEPATransfer = 0x0A,
    OtherBankTransfer = 0x0B,
    TransferToCashbookVaultOwnerEmployee = 0x0C,
    InternalMaterialConsumption = 0x0D,
    Grant = 0x0E,
    TicketRestaurant = 0x0F
}
