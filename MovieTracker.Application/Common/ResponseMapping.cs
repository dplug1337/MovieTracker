using Mapster;

namespace MovieTracker.Application.Common;

internal static class ResponseMapping
{
    internal static TypeAdapterConfig Config { get; } = CreateConfig();

    private static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(ResponseMapping).Assembly);
        return config;
    }
}
