using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

[JsonConverter(typeof(StringEnumConverter))]
public enum TseHealthState
{
    [EnumMember(Value = "starting")]
    Starting,

    [EnumMember(Value = "started")]
    Started,

    [EnumMember(Value = "stopped")]
    Stopped,

    [EnumMember(Value = "defect")]
    Defect
}