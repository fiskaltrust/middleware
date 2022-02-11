namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers
{
    public static class Constants
    {
        public const string TseIoFile = "TSE-IO.bin";
        public const string ADMINNAME = "Admin";
        public const string TIMEADMINNAME = "TimeAdmin";
        public const string EXPORT_TOTAL_EXTENSION = ".total";
        public const string EXPORT_READPOSITION_EXTENSION = ".read";
        public const string EXPORT_ERASE_EXTENSION = ".erase";
        public const string EXPORT_FAILURE_EXTENSION = ".failure";
        public const string NOEXPORT = "noexport-";
        public const int DEFAULT_TSE_IO_TIMEOUT = 40 * 1000;
        public const int DEFAULT_TSE_IO_READ_DELAY_MS = 10;
    }
}
