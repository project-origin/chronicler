using System.Text.Json.Serialization;

namespace ProjectOrigin.ServiceCommon;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LogFormat
{
    Text,
    Json
}
