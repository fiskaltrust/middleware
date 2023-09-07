namespace fiskaltrust.ifPOS.v2
{
    public class ReceiptCaseBuilder
    {
        private long _case;

        public ReceiptCaseBuilder(long baseCase)
        {
            _case = baseCase;
        }

        public static ReceiptCaseBuilder IT() => new ReceiptCaseBuilder(0x4954_2000_0000_0000);

        public ReceiptCaseBuilder DefineGlobalFlags(ftReceiptCaseGlobalFlags globalFlag)
        {
            _case |= (long) globalFlag;
            return this;
        }

        public ReceiptCaseBuilder DefineReceiptCase(ftReceiptCases receiptCase)
        {
            _case |= (long) receiptCase;
            return this;
        }

        public long Build() => _case;
    }


    public class PayItemCaseBuilder
    {
        private long _case;

        public PayItemCaseBuilder(long baseCase)
        {
            _case = baseCase;
        }

        public static PayItemCaseBuilder IT() => new PayItemCaseBuilder(0x4954_2000_0000_0000);

        public PayItemCaseBuilder DefineGlobalFlags(ftPayItemGlobalFlags globalFlag)
        {
            _case |= (long) globalFlag;
            return this;
        }

        public PayItemCaseBuilder DefinePayItemTypes(ftPayItemPaymentTypes paymentType)
        {
            _case |= (long) paymentType;
            return this;
        }


        public long Build() => _case;
    }

    public class ChargeItemCaseBuilder
    {
        private long _case;

        public ChargeItemCaseBuilder(long baseCase)
        {
            _case = baseCase;
        }

        public static ChargeItemCaseBuilder IT() => new ChargeItemCaseBuilder(0x4954_2000_0000_0000);

        public ChargeItemCaseBuilder DefineGlobalFlags(ftChargeItemGlobalFlags globalFlag)
        {
            _case |= (long) globalFlag;
            return this;
        }

        public ChargeItemCaseBuilder DefineServiceType(ftChargeItemServiceTypes serviceType)
        {
            _case |= (long) serviceType;
            return this;
        }

        public ChargeItemCaseBuilder DefineVatRate(ftVatRates vatRate)
        {
            _case |= (long) vatRate;
            return this;
        }

        public long Build() => _case;
    }

    public enum ftChargeItemGlobalFlags : long
    {
        IsVoid = 0x0001_0000,
        IsReturnOrRefund = 0x0002_0000,
        Discount = 0x0004_0000,
        Downpayment = 0x0008_0000,
        Returnable = 0x0010_0000,
        TakeAway = 0x0020_0000,
        ShowInPayments = 0x8000_0000
    }

    public enum ftChargeItemServiceTypes : long
    {
        Unknown = 0x00,
        DeliveryOrSupplyOfGoods = 0x10,
        OtherServiceOrSupplyOfService = 0x20,
        Tip = 0x30,
        Voucher = 0x40,
        CatalogService = 0x50,
        NotOwnSalesOrAgencyBusiness = 0x60,
        OwnConsumption = 0x70,
        Grant = 0x80,
        Receivable = 0x90,
        CashTransfer = 0xA0
    }

    public enum ftVatRates : long
    {
        Unknown_G = 0x0,
        Discounted1_B = 0x1,
        Discounted2_C = 0x2,
        Normal_A = 0x3,
        SuperReduced1_D = 0x4,
        SuperReduced2_E = 0x5,
        Parting_F = 0x6,
        Zero_H = 0x7,
        NotTaxable_I = 0x8
    }

    public enum ftReceiptCases : long
    {
        // Receipt types
        Unknown = 0x0000,
        PointOfSalesReceipt = 0x0001,
        CashBookTransaction = 0x0002, // name ist evtl noch verbesserungswürdoig
        PointOfSalesWithoutFiscalization = 0x0003,
        ECommerce = 0x0004,
        Protocol = 0x0005,

        // Invoice types
        InvoiceUnknown = 0x1000,
        InvoiceB2C = 0x1001,
        InvoiceB2B = 0x1002,
        InvoiceB2G = 0x1003,

        // Daily operations
        Zero = 0x2000,
        ShiftClosing = 0x2010,
        DailyClosing = 0x2011,
        MonthlyClosing = 0x2012,
        YearlyClosing = 0x2013,

        // Log
        ProtocolUnspecified = 0x3000,
        ProtocolTechnicalEvent = 0x3001,
        ProtocolAuditEvent = 0x3002,
        InternalUsageMaterialConsumption = 0x3003,
        Order = 0x3004,

        // Lifecycle
        QueueStartReceipt = 0x4001,
        QueueStopReceipt = 0x4002
    }

    public enum ftReceiptCaseGlobalFlags : long
    {
        ProcessAsLateSigning = 0x0001_0000,
        TrainingReceipt = 0x0002_0000,
        IsVoid = 0x0004_0000,
        ProcessHandwrittenReceipt = 0x0008_0000,
        IssureIsSmallBusiness = 0x0010_0000,
        ReceiverIsBusiness = 0x0020_0000,
        ReceiverIsKnown = 0x0040_0000,
        IsSaleInForeignCountry = 0x0080_0000,
        IsReturnOrRefund = 0x0100_0000,
        ReceiptRequest = 0x8000_0000
    }

    public enum ftPayItemGlobalFlags : long
    {
        IsVoid = 0x0001_0000,
        IsReturnOrRefund = 0x0002_0000,
        Reserved = 0x0004_0000,
        Downpayment = 0x0008_0000,
        IsForeignCurrency = 0x0010_0000,
        IsChange = 0x0020_0000,
        IsTip = 0x0040_0000,
        IsElectronicalOrIsDigital = 0x0080_0000,
        ShowInChargeItems = 0x8000_0000
    }

    public enum ftPayItemPaymentTypes : long
    {
        Unknown = 0x00,
        Cash = 0x01,
        NonCash = 0x02,
        CrossedCheque = 0x03,
        DebitCard = 0x04,
        CreditCard = 0x05,
        Voucher = 0x06,
        Online = 0x07,
        LoyaltyProgram = 0x08,
        AccountsReceivable = 0x09,
        SEPATransfer = 0x0A,
        OtherBankTransfer = 0x0B,
        MoneyTransfer = 0x0C, // Transfer to Cashbook / Vault / Owner / Employee 
        InternalOrMaterialConsumption = 0x0D,
        Grant = 0x0E
    }
}