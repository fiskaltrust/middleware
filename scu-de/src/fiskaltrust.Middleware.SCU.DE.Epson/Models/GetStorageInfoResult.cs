using System;

namespace fiskaltrust.Middleware.SCU.DE.Epson.ResultModels
{
    public class GetStorageInfoResult : EpsonResult
    {
        public const string TseInitializationStateUninitialized = "UNINITIALIZED";
        public const string TseInitializationStateInitialized = "INITIALIZED";
        public const string TseInitializationStateDecommissioned = "DECOMMISSIONED";
    }

    public class StorageInfoResult
    {
        public SmartInformation SmartInformation { get; set; }
        public TseInformation TseInformation { get; set; }
    }

    public class SmartInformation
    {
        public SpareBlockStatus SpareBlockStatus { get; set; }
        public EraseLifetimeStatus EraseLifetimeStatus { get; set; }
        public DataIntegrity DataIntegrity { get; set; }
        public uint RemainingTenYearsDataRetention { get; set; }
        public bool IsReplacementNeeded { get; set; }
        public string TseHealth { get; set; }
    }

    public class SpareBlockStatus
    {
        public string HealthStatus { get; set; }
        public uint RemainingSpareBlocks { get; set; }
    }

    public class DataIntegrity
    {
        public string HealthStatus { get; set; }
        public uint UncorrectableECCErrors { get; set; }
    }

    public class EraseLifetimeStatus
    {
        public string HealthStatus { get; set; }
        public uint RemainingEraseCounts { get; set; }
    }

    public class TseInformation
    {
        public string VendorType { get; set; }
        public ulong TseCapacity { get; set; }
        public int TseCurrentSize { get; set; }
        public string TseInitializationState { get; set; }
        public bool HasValidTime { get; set; }
        public bool HasPassedSelfTest { get; set; }
        public ulong TimeUntilNextSelfTest { get; set; }
        public bool IsExportEnabledIfCspTestFails { get; set; }
        public int MaxUpdateDelay { get; set; }
        public int StartedTransactions { get; set; }
        public int MaxStartedTransactions { get; set; }
        public int CreatedSignatures { get; set; }
        public ulong RemainingSignatures { get; set; }
        public int MaxSignatures { get; set; }
        public int RegisteredClients { get; set; }
        public int MaxRegisteredClients { get; set; }
        public DateTime CertificateExpirationDate { get; set; }
        public ulong TarExportSize { get; set; }
        public bool IsTransactionInProgress { get; set; }
        public bool IsTseUnlocked { get; set; }
        public string SerialNumber { get; set; }
        public string TsePublicKey { get; set; }
        public string SignatureAlgorithm { get; set; }
        public string TseDescription { get; set; }
        public ulong SoftwareVersion { get; set; }
        public int HardwareVersion { get; set; }
        public string CdcId { get; set; }
        public string CdcHash { get; set; }
        public string LastExportExecutedDate { get; set; }
    }
}