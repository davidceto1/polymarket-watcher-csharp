using PolymarketWatcher;

var configPath = "watches.yaml";
bool once = false;

// Simple arg parsing
for (int i = 0; i < args.Length; i++)
{
    if (args[i] is "-c" or "--config" && i + 1 < args.Length)
    {
        configPath = args[++i];
    }
    else if (args[i] == "--once")
    {
        once = true;
    }
}

// Graceful shutdown
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down...");
    cts.Cancel();
};

// Load config
var config = ConfigLoader.Load(configPath);

// Startup banner
Console.WriteLine(new string('=', 60));
Console.WriteLine("  Polymarket Watcher");
Console.WriteLine($"  Poll interval: {config.PollInterval}s");
Console.WriteLine($"  Watches: {config.Watches.Count}");
Console.WriteLine(new string('=', 60));
Console.WriteLine();

// Resolve markets
Console.WriteLine("Resolving markets...");
var watchStates = new List<WatchState>();

foreach (var watch in config.Watches)
{
    var slug = watch.Slug ?? watch.ConditionId ?? "";
    var name = watch.Name ?? slug;

    Console.Write($"  Resolving '{name}' ({slug})... ");

    try
    {
        MarketInfo marketInfo;
        if (!string.IsNullOrEmpty(watch.ConditionId))
            marketInfo = await PolymarketApi.ResolveMarketByConditionIdAsync(watch.ConditionId);
        else
            marketInfo = await PolymarketApi.ResolveMarketAsync(slug);

        // Find the YES token (or fall back to first token)
        var yesToken = marketInfo.Tokens.FirstOrDefault(t =>
            t.Outcome.Equals("Yes", StringComparison.OrdinalIgnoreCase));
        yesToken ??= marketInfo.Tokens.FirstOrDefault();

        if (yesToken == null)
        {
            Console.WriteLine("FAILED: No tokens found");
            continue;
        }

        var alerts = watch.Alerts.Select(a =>
            new Alert(a.Direction, a.Threshold, a.Message ?? $"{name} crossed {a.Threshold}")
        ).ToList();

        watchStates.Add(new WatchState
        {
            Name = name,
            Slug = slug,
            TokenId = yesToken.TokenId,
            Question = marketInfo.Question ?? name,
            Alerts = alerts,
        });

        Console.WriteLine($"OK (token: {yesToken.TokenId[..Math.Min(12, yesToken.TokenId.Length)]}...)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAILED: {ex.Message}");
    }
}

if (watchStates.Count == 0)
{
    Console.Error.WriteLine("No markets could be resolved. Check your config.");
    Environment.Exit(1);
}

Console.WriteLine($"\nWatching {watchStates.Count} market(s). Press Ctrl+C to stop.\n");

// Print initial prices
await PrintStatus(watchStates);

if (once)
{
    await PollOnce(watchStates);
    return;
}

// Main polling loop
try
{
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(config.PollInterval * 1000, cts.Token);
        await PollOnce(watchStates);
    }
}
catch (OperationCanceledException)
{
    // Expected on Ctrl+C
}

// --- Helper methods ---

async Task PollOnce(List<WatchState> states)
{
    foreach (var state in states)
    {
        try
        {
            var price = await PolymarketApi.GetMidPriceAsync(state.TokenId);
            if (price == null)
                continue;

            foreach (var alert in state.Alerts)
            {
                var msg = alert.Check(price.Value);
                if (msg != null)
                {
                    Console.WriteLine(Alert.Format(state.Name, price.Value, alert, msg));
                }
            }
        }
        catch (Exception ex)
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine($"[{now}] Error fetching {state.Name}: {ex.Message}");
        }
    }
}

async Task PrintStatus(List<WatchState> states)
{
    var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    Console.WriteLine($"[{now}] Prices:");
    foreach (var state in states)
    {
        try
        {
            var price = await PolymarketApi.GetMidPriceAsync(state.TokenId);
            if (price.HasValue)
                Console.WriteLine($"  {state.Name}: {price.Value:F4} ({price.Value * 100:F1}%)");
            else
                Console.WriteLine($"  {state.Name}: no data");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  {state.Name}: error ({ex.Message})");
        }
    }
}
