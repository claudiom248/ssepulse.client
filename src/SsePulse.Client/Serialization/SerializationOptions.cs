using System.Text.Json;

namespace SsePulse.Client.Serialization;

internal static class SerializationOptions
{
    public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}