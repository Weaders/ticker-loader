using System.Text.Json.Serialization;

namespace TickerLoader.TinkoffClient.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubscriptionAction
{
    Unspecified,
    Subscribe,
    Unsubscribe
}
