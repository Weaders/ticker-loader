namespace TickerLoader.TinkoffClient.Models.Extensions;

public static class SubscriptionActionExtensions
{
    public static string ToApiValue(this SubscriptionAction action) => action switch
    {
        SubscriptionAction.Subscribe => "SUBSCRIPTION_ACTION_SUBSCRIBE",
        SubscriptionAction.Unsubscribe => "SUBSCRIPTION_ACTION_UNSUBSCRIBE",
        _ => "SUBSCRIPTION_ACTION_UNSPECIFIED"
    };
}
