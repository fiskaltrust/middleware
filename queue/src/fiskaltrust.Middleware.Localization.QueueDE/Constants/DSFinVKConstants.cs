namespace fiskaltrust.Middleware.Localization.QueueDE.Constants
{
    public static class DSFinVKConstants
    {
        public const string PROCESS_TYPE_KASSENBELEG_V1 = "Kassenbeleg-V1";
        public const string PROCESS_TYPE_BESTELLUNG_V1 = "Bestellung-V1";
        public const string PROCESS_TYPE_SONSTIGER_VORGANG = "SonstigerVorgang";
        public const string PROCESS_TYPE_EMPTY = "";

        public const string PROCESS_DATA_PAYMENT_CASH_TEXT = "Bar";
        public const string PROCESS_DATA_PAYMENT_NON_CASH_TEXT = "Unbar";

        public const string BON_TYP_RECEIPT = "Beleg";
        public const string BON_TYP_OTHERACTION_ELSE = "AVSonstige";
        public const string BON_TYP_OTHERACTION_FAILED = "AVBelegabbruch";
        public const string BON_TYP_OTHERACTION_INVOICE = "AVRechnung";
        public const string BON_TYP_OTHERACTION_TRANSFER = "AVTransfer";
        public const string BON_TYP_OTHERACTION_ORDER = "AVBestellung";
        public const string BON_TYP_OTHERACTION_CONSUMPTION = "AVSachbezug";
        public const string BON_TYP_OTHERACTION_TRAINING = "AVTraining";
        public const string BON_TYP_OTHERACTION_VOIDED = "AVBelegstorno";
        public const string BON_TYP_NONE = "";

        public static class Delimiters
        {
            public const string ORDER_LINE = "\r";
        }
    }
}
