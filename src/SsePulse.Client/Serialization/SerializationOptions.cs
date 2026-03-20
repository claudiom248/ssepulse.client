using System.Text.Json;

namespace SsePulse.Client.Serialization;

internal static class SerializationOptions
{
    public static readonly JsonSerializerOptions EventDataJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}