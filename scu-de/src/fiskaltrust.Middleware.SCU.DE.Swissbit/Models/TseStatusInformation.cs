using System;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Models
{
    public class TseStatusInformation
    {
        public string CustomizationIdentifier { get; set; }
        public bool IsDevelopmentFirmware { get; set; }
        public uint CapacityInBlocks { get; set; }
        public uint SizeInBlocks { get; set; }
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
        public uint TimeUntilNextSelfTest { get; set; }
        public uint StartedTransactions { get; set; }
        public uint MaxStartedTransactions { get; set; }
        public uint CreatedSignatures { get; set; }
        public uint MaxSignatures { get; set; }
        public uint RemainingSignatures { get; set; }
        public uint MaxTimeSynchronizationDelay { get; set; }
        public uint MaxUpdateDelay { get; set; }
        public byte[] TsePublicKey { get; set; }
        public byte[] TseSerialNumber { get; set; }
        public string TseDescription { get; set; }
        public uint RegisteredClients { get; set; }
        public uint MaxRegisteredClients { get; set; }
        public ulong CertificateExpirationDate { get; set; }
        public ulong TarExportSizeInSectors { get; set; }
        public ulong TarExportSizeInBytes { get; set; }
        public uint HardwareVersion { get; set; }
        public uint SoftwareVersion { get; set; }
        public string FormFactor { get; set; }
    }
}
