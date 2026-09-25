using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SsePulse.Client.Internal;

internal static class JsonTypeInfoProvider
{
    public static JsonTypeInfo<T> Get<T>(JsonSerializerOptions options)
    {
        if (!options.IsReadOnly && options.TypeInfoResolver is null && JsonSerializer.IsReflectionEnabledByDefault)
        {
            PopulateDefaultResolver(options);
        }

        return (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Only called when reflection-based serialization is enabled.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Only called when reflection-based serialization is enabled.")]
    private static void PopulateDefaultResolver(JsonSerializerOptions options)
    {
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
    }
}
