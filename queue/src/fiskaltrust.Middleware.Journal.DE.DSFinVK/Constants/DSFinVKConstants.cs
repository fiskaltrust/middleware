namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants
{
    public static class DSFinVKConstants
    {
        public static class VatDefinition
        {
            public static string GetVatDefinition(int id)
            {
                switch (id)
                {
                    case int n when n >= 1000:
                        return "individuelle Sachverhalte (Altsteuersätze, § 13b UStG, o.ä.)";
                    case int n when n >= 8:
                        return "reserviert für Änderungen DSFinV-K";
                    case 7:
                        return "0,00 % UmsatzsteuerNichtErmittelbar";
                    case 6:
                        return "0,00 % Umsatzsteuerfrei";
                    case 5:
                        return "0,00 % Nicht Steuerbar";
                    case 4:
                        return "5,50 % Durchschnittsatz(§ 24 Abs. 1 Nr. 1 UStG)";
                    case 3:
                        return "10,70 % Durchschnittsatz(§ 24 Abs. 1 Nr. 3 UStG) übrige Fälle";
                    case 2:
                        return "7,00 % Ermäßigter Steuersatz";
                    case 1: 
                        return "19,00 % Allgemeiner Steuersatz";
                    default:
                        return "Not defined";
                }
            }
        }

        public static class ProcessTypes
        {
            public const string KassenbelegV1 = "Kassenbeleg-V1";
            public const string BestellungV1 = "Bestellung-V1";
            public const string SonstigerVorgang = "SonstigerVorgang";
            public const string Empty = "";
        }

        public static class PaymentTypes
        {
            public const string Cash = "Bar";
            public const string NonCash = "Unbar";
            public const string EcCard = "ECKarte";
            public const string CreditCard = "Kreditkarte";
            public const string ElectronicPaymentProvider = "ElZahlungsdienstleister";
            public const string PrepaidCard = "Guthabenkarte";
        }

        public static class BonTypes
        {
            public const string Receipt = "Beleg";
            public const string OtherActionElse = "AVSonstige";
            public const string OtherActionFailed = "AVBelegabbruch";
            public const string OtherActionInvoice = "AVRechnung";
            public const string OtherActionTransfer = "AVTransfer";
            public const string OtherActionOrder = "AVBestellung";
            public const string OtherActionConsumption = "AVSachbezug";
            public const string OtherActionTraining = "AVTraining";
            public const string OtherActionVoided = "AVBelegstorno";
            public const string None = "";
        }

        public static class BusinessCaseTypes
        {
            public const string Revenue = "Umsatz";
            public const string ReturnableNormal = "Pfand";
            public const string ReturnableReverse = "PfandRueckzahlung";
            public const string Discount = "Rabatt";
            public const string ExtraCharge = "Aufschlag";
            public const string GrantNotReal = "ZuschussUnecht";
            public const string GrantReal = "ZuschussEcht";
            public const string TipEmployer = "TrinkgeldAG";
            public const string TipEmployee = "TrinkgeldAN";
            public const string VoucherPurchase = "MehrzweckgutscheinKauf";
            public const string CouponPurchase = "EinzweckgutscheinKauf";
            public const string VoucherRedemption = "MehrzweckgutscheinEinloesung";
            public const string CouponRedemption = "EinzweckgutscheinEinloesung";
            public const string ReceivableCreation = "Forderungsentstehung";
            public const string ReceivableReduction = "Forderungsaufloesung";
            public const string DownPaymentCreation = "Anzahlungseinstellung";
            public const string DownPaymentReduction = "Anzahlungsaufloesung";
            public const string CashTransferToEmptyTill = "Anfangsbestand";
            public const string CashTransferToOwner = "Privatentnahme";
            public const string CashTransferFromOwner = "Privateinlage";
            public const string CashTransferFromOrToTill = "Geldtransit";
            public const string CashTransferToEmployee = "Lohnzahlung";
            public const string CashTransferToCashBook = "Einzahlung";
            public const string CashTransferFromCashbook = "Auszahlung";
            public const string CashAmountDifference = "DifferenzSollIst";
        }

        public static class VatKeys
        {
            /// <summary>
            /// Zum Zeitpunkt der Erfassung des Geschäftsvorfalls geltender allgemeiner Steuersatz (§ 12 Abs. 1 UStG)
            /// </summary>
            public const int General = 1;
            /// <summary>
            /// Zum Zeitpunkt der Erfassung des Geschäftsvorfalls geltender ermäßigter Steuersatz(§ 12 Abs. 2 UStG)
            /// </summary>
            public const int Reduced = 2;
            /// <summary>
            /// 10,70% - Durchschnittsatz (§ 24 Abs. 1 Nr. 3 UStG) übrige Fälle
            /// </summary>
            public const int AverageRate = 3;
            /// <summary>
            /// 5,50% - Durchschnittsatz (§ 24 Abs. 1 Nr. 1 UStG)
            /// </summary>
            public const int AverageRateReduced = 4;
            /// <summary>
            /// 0,00% - Nicht Steuerbar
            /// </summary>
            public const int NotTaxable = 5;
            /// <summary>
            /// 0,00% - Umsatzsteuerfrei
            /// </summary>
            public const int Excempt = 6;
            /// <summary>
            /// 0,00% - UmsatzsteuerNichtErmittelbar
            /// </summary>
            public const int NotDetectable = 7;
            /// <summary>
            /// 19,00% - Historischer allgemeiner Steuersatz (§ 12 Abs. 1 UStG)
            /// </summary>
            public const int GeneralHistoric = 11;
            /// <summary>
            /// 7,00% - Historischer ermäßigter Steuersatz (§ 12 Abs. 2 UStG)
            /// </summary>
            public const int ReducedHistoric = 12;
            /// <summary>
            /// 16,00% - Historischer allgemeiner Steuersatz (§ 12 Abs. 1 UStG)
            /// </summary>
            public const int GeneralHistoricCovid = 21;
            /// <summary>
            /// 5,00% - Historischer ermäßigter Steuersatz (§ 12 Abs. 2 UStG)
            /// </summary>
            public const int ReducedHistoricCovid = 22;
        }
    }
}
