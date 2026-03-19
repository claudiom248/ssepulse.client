using System.Text.Json;

namespace SsePulse.Serialization;

internal static class SerializationOptions
{
    public static readonly JsonSerializerOptions EventDataJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}