using System.Text.Json;

namespace SsePulse;

internal static class Serialization
{
    public static JsonSerializerOptions EventDataJsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
}