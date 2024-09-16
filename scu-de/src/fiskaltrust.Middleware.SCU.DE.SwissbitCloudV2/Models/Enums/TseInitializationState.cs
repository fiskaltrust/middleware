using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

[JsonConverter(typeof(StringEnumConverter))]
public enum TseInitializationState
{
    [EnumMember(Value = "uninitialized")]
    Uninitialized,

    [EnumMember(Value = "initialized")]
    Initialized,

    [EnumMember(Value = "disabled")]
    Disabled
}