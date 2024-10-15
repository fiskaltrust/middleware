using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models
{
    public class TseDto
    {
        [JsonProperty("createdSignaturesThisMonth")]
        public int CreatedSignaturesThisMonth { get; set; }

        [JsonProperty("productType")]
        public string ProductType { get; set; }

        [JsonProperty("creditClientId")]
        public string CreditClientId { get; set; }

        [JsonProperty("healthState")]
        public string HealthState { get; set; }

        [JsonProperty("storageCapacity")]
        public int StorageCapacity { get; set; }

        [JsonProperty("storageUsed")]
        public int StorageUsed { get; set; }

        [JsonProperty("initializationState")]
        public string InitializationState { get; set; }

        [JsonProperty("timeUntilNextSelfTest")]
        public int TimeUntilNextSelfTest { get; set; }

        [JsonProperty("numStartedTransactions")]
        public int NumberOfStartedTransactions { get; set; }

        [JsonProperty("maxStartedTransactions")]
        public int MaxNumberOfStartedTransactions { get; set; }

        [JsonProperty("createdSignatures")]
        public int CreatedSignatures { get; set; }

        [JsonProperty("numRegisteredClients")]
        public int NumberOfRegisteredClients { get; set; }

        [JsonProperty("maxRegisteredClients")]
        public int MaxNumberOfRegisteredClients { get; set; }

        [JsonProperty("certificateExpirationDate")]
        public int CertificateExpirationDate { get; set; }

        [JsonProperty("maxUpdateDelay")]
        public int MaxofUpdateDelay { get; set; }

        [JsonProperty("softwareVersion")]
        public string SoftwareVersion { get; set; }

        [JsonProperty("certificateChain")]
        public string CertificateChain { get; set; }

        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }

        [JsonProperty("metadata")]
        public string Metadata { get; set; }
    }
}
