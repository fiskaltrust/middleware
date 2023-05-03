namespace fiskaltrust.Middleware.Localization.QueueIT.Constants
{
    public class States
    {
        public const long ToOldForLateSigning = Cases.BASE_STATE + 0x03;
    }

    public class StateFlags
    {
        public const long ReceiptsNeedManualUpload = 0x0000_0000_0001_0000;
    }
}