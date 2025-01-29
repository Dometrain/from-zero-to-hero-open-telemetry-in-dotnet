using StackExchange.Redis;

namespace Clients.Api.Extensions;

public static class RedisConfigurationExtensions
{
    public static WebApplicationBuilder AddRedis(this WebApplicationBuilder builder)
    {
        IConnectionMultiplexer? connectionMultiplexer = ConnectionMultiplexer.Connect(
            builder.Configuration.GetConnectionString("ClientsCache")!);
        builder.Services.AddSingleton(connectionMultiplexer);
        builder.Services.AddStackExchangeRedisCache(options =>
            options.ConnectionMultiplexerFactory =()=> Task.FromResult(connectionMultiplexer));
        return builder;
    }
}