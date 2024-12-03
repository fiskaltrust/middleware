using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace fiskaltrust.Api.POS.Models.ifPOS.v2
{
    public class PaymentProvierResponseBase
    {
        [JsonExtensionData]
        public Dictionary<string, object?>? AdditionalProperties { get; set; }
    }

    public class VivaPaymentSession : PaymentProvierResponseBase
    {
        public Guid? sessionId { get; set; }
        public string? terminalId { get; set; }
        public string? cashRegisterId { get; set; }
        public int? amount { get; set; }
        public string? currencyCode { get; set; }
        public string? merchantReference { get; set; }
        public string? customerTrns { get; set; }
        public int? tipAmount { get; set; }
        public string? aid { get; set; }
        public bool? showTransactionResult { get; set; }
        public bool? showReceipt { get; set; }
        public bool? success { get; set; }
        public int? eventId { get; set; }
        public string? authorizationId { get; set; }
        public string? transactionId { get; set; }
        public int? transactionTypeId { get; set; }
        public long? retrievalReferenceNumber { get; set; }
        public string? panEntryMode { get; set; }
        public string? applicationLabel { get; set; }
        public string? primaryAccountNumberMasked { get; set; }
        public DateTime? transactionDateTime { get; set; }
        public bool? abortOperation { get; set; }
        public string? abortAckTime { get; set; }
        public bool? abortSuccess { get; set; }
        public object? loyaltyInfo { get; set; } // TBD define
        public string? verificationMethod { get; set; }
        public string? tid { get; set; }
        public string? shortOrderCode { get; set; }
        public int? installments { get; set; }
        public string? message { get; set; }
        public bool? preauth { get; set; }
        public int? referenceNumber { get; set; }
        public string? orderCode { get; set; }
        public string? aadeTransactionId { get; set; }
        public object? dccDetails { get; set; } // Todo
        public object? surchargeAmount { get; set; }
    }

# pragma warning disable
    public class VivaWalletPayment
    {
        public string sessionId { get; set; }
        public string terminalId { get; set; }
        public string cashRegisterId { get; set; }
        public int amount { get; set; }
        public string currencyCode { get; set; }
        public string merchantReference { get; set; }
        public string aadeProviderId { get; set; }
        public string aadeProviderSignatureData { get; set; }
        public string aadeProviderSignature { get; set; }
        public int tipAmount { get; set; }
    }

    public class PayItemCaseProviderVivaWalletApp2APp : PayItemCaseProviderData
    {
        [JsonPropertyName("ProtocolRequest")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string ProtocolRequest { get; set; }

        [JsonPropertyName("ProtocolResponse")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string ProtocolResponse { get; set; }
    }

    public class PayItemCaseProviderVivaWallet : PayItemCaseProviderData
    {
        [JsonPropertyName("ProtocolRequest")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public VivaWalletPayment? ProtocolRequest { get; set; }

        [JsonPropertyName("ProtocolResponse")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public VivaPaymentSession? ProtocolResponse { get; set; }
    }

    [JsonDerivedType(typeof(PayItemCaseProviderVivaWallet))]
    public class PayItemCaseProviderData
    {
        [JsonPropertyName("Protocol")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [DataMember(EmitDefaultValue = true, IsRequired = true)]
        public required string Protocol { get; set; }

        [JsonPropertyName("ProtocolVersion")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string? ProtocolVersion { get; set; }

        [JsonPropertyName("Action")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [DataMember(EmitDefaultValue = true, IsRequired = true)]
        public required string Action { get; set; }

        [JsonPropertyName("ProtocolRequest")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public object ProtocolRequest { get; set; }

        [JsonPropertyName("ProtocolResponse")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public object ProtocolResponse { get; set; }
    }

    public class PayItemCaseDataApp2App
    {
        [JsonPropertyName("Provider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public PayItemCaseProviderVivaWalletApp2APp? Provider { get; set; }

        [JsonPropertyName("Receipt")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public List<string>? Receipt { get; set; }
    }

    public class PayItemCaseDataCloudApi
    {
        [JsonPropertyName("Provider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public PayItemCaseProviderVivaWallet? Provider { get; set; }

        [JsonPropertyName("Receipt")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public List<string>? Receipt { get; set; }
    }

    public class GenericPaymentPayload
    {
        [JsonPropertyName("Provider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public PayItemCaseProviderData? Provider { get; set; }

        [JsonPropertyName("Receipt")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public List<string>? Receipt { get; set; }
    }
}
