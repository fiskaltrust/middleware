﻿namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum PayItemCase : long
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

public static class PayItemCaseExt
{
    public static PayItemCase AsPayItemCase(this long self) => (PayItemCase) self;
    public static T As<T>(this PayItemCase self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);
    public static PayItemCase Reset(this PayItemCase self) => (PayItemCase) (0xFFFF_F000_0000_0000 & (ulong) self);

    public static PayItemCase WithVersion(this PayItemCase self, byte version) => (PayItemCase) ((((ulong) self) & 0xFFFF_0FFF_FFFF_FFFF) | ((ulong) version << (4 * 11)));
    public static byte Version(this PayItemCase self) => (byte) ((((long) self) >> (4 * 11)) & 0xF);

    public static PayItemCase WithCountry(this PayItemCase self, string country)
        => country?.Length != 2
            ? throw new Exception($"'{country}' is not ISO country code")
            : self.WithCountry((country[0] << (4 * 2)) + country[1]);


    public static PayItemCase WithCountry(this PayItemCase self, long country) => (PayItemCase) (((long) self & 0x0000_FFFF_FFFF_FFFF) | (country << (4 * 12)));
    public static long CountryCode(this PayItemCase self) => (long) self >> (4 * 12);
    public static string Country(this PayItemCase self)
    {
        var countryCode = self.CountryCode();

        return Char.ConvertFromUtf32((char) (countryCode & 0xFF00) >> (4 * 2)) + Char.ConvertFromUtf32((char) (countryCode & 0x00FF));
    }
    public static bool IsCase(this PayItemCase self, PayItemCase @case) => ((long) self & 0xFF) == (long) @case;
    public static PayItemCase WithCase(this PayItemCase self, PayItemCase state) => (PayItemCase) (((ulong) self & 0xFFFF_FFFF_FFFF_FF00) | (ulong) state);
    public static PayItemCase Case(this PayItemCase self) => (PayItemCase) ((long) self & 0xFF);
}