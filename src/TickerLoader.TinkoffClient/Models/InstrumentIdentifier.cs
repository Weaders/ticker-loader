using System.Text.Json.Serialization;

namespace TickerLoader.TinkoffClient.Models;

public sealed class InstrumentIdentifier
{
    [JsonPropertyName("instrumentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InstrumentId { get; init; }

    public static InstrumentIdentifier FromInstrumentId(string instrumentId) =>
        new() { InstrumentId = instrumentId };
}
