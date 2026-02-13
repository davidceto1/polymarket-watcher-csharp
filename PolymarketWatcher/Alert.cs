namespace PolymarketWatcher;

public class Alert
{
    public string Direction { get; }
    public double Threshold { get; }
    public string Message { get; }
    public bool Triggered { get; private set; }

    public Alert(string direction, double threshold, string message)
    {
        Direction = direction;
        Threshold = threshold;
        Message = message;
        Triggered = false;
    }

    /// <summary>
    /// Check if this alert should fire for the given price.
    /// Returns the message if triggered, null otherwise.
    /// Fires once on crossing, resets when price crosses back.
    /// </summary>
    public string? Check(double price)
    {
        bool isCrossed = Direction == "above"
            ? price >= Threshold
            : price <= Threshold;

        if (isCrossed && !Triggered)
        {
            Triggered = true;
            return Message;
        }

        if (!isCrossed && Triggered)
        {
            // Price crossed back, reset so it can fire again
            Triggered = false;
        }

        return null;
    }

    public static string Format(string marketName, double price, Alert alert, string message)
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var arrow = alert.Direction == "above" ? "\u2191" : "\u2193";
        return $"[{now}] {arrow} ALERT: {marketName} | Price: {price:F4} ({price * 100:F1}%) | Threshold: {alert.Threshold:F4} | {message}";
    }
}
