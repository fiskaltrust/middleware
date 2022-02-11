using System;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.Models
{
    public class TseStatusInformation
    {
        public string CustomizationIdentifier { get; set; }
        public bool IsDevelopmentFirmware { get; set; }
        public UInt32 CapacityInBlocks { get; set; }
        public UInt32 SizeInBlocks { get; set; }
        public bool IsStoreOpen { get; set; }
        public bool HasValidTime { get; set; }
        public bool HasPassedSelfTest { get; set; }
        public bool IsCtssInterfaceActive { get; set; }
        public bool IsExportEnabledIfCspTestFails { get; set; }
        public NativeFunctionPointer.WormInitializationState initializationState { get; set; }
        public bool IsDataImportInProgress { get; set; }
        public bool IsTransactionInProgress { get; set; }
        public bool HasChangedPuk { get; set; }
        public bool HasChangedAdminPin { get; set; }
        public bool HasChangedTimeAdminPin { get; set; }
        public UInt32 TimeUntilNextSelfTest { get; set; }
        public UInt32 StartedTransactions { get; set; }
        public UInt32 MaxStartedTransactions { get; set; }
        public UInt32 CreatedSignatures { get; set; }
        public UInt32 MaxSignatures { get; set; }
        public UInt32 RemainingSignatures { get; set; }
        public UInt32 MaxTimeSynchronizationDelay { get; set; }
        public UInt32 MaxUpdateDelay { get; set; }
        public byte[] TsePublicKey { get; set; }
        public byte[] TseSerialNumber { get; set; }
        public string TseDescription { get; set; }
        public UInt32 RegisteredClients { get; set; }
        public UInt32 MaxRegisteredClients { get; set; }
        public UInt64 CertificateExpirationDate { get; set; }
        public UInt64 TarExportSizeInSectors { get; set; }
        public UInt64 TarExportSizeInBytes { get; set; }
        public UInt32 HardwareVersion { get; set; }
        public UInt32 SoftwareVersion { get; set; }
        public string FormFactor { get; set; }
    }
}
