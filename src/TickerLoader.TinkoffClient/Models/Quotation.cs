using System.Text.Json.Serialization;

namespace TickerLoader.TinkoffClient.Models;

public sealed class Quotation
{
    [JsonPropertyName("units")]
    public string Units { get; init; } = "0";

    [JsonPropertyName("nano")]
    public int Nano { get; init; }

    public decimal ToDecimal()
    {
        var units = long.Parse(Units);
        return units + Nano / 1_000_000_000m;
    }
}
