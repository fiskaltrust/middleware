using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.ProForma;

public class CostCenterChangeInput : BaseInputData
{
    [JsonPropertyName("transfer")]
    public required TransferInput Transfer { get; set; }
}

public class ProFormaMutations
{
    public static string SignOrderQuery = @"mutation SignOrder($data:OrderInput! $isTraining:Boolean!) {signOrder(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}";
}