using System.Text.Json;
using System.Text.Json.Serialization;
using TickerLoader.TinkoffClient.Models;

namespace TickerLoader.TinkoffClient.Serialization;

internal static class TinkoffJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, Options);

    public static MarketDataResponse? DeserializeResponse(string json) =>
        JsonSerializer.Deserialize<MarketDataResponse>(json, Options);
}
