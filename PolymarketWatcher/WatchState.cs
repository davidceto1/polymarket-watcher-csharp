namespace PolymarketWatcher;

/// <summary>
/// Runtime state for a single watched market.
/// </summary>
public class WatchState
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string TokenId { get; set; } = "";
    public string Question { get; set; } = "";
    public List<Alert> Alerts { get; set; } = new();
}
