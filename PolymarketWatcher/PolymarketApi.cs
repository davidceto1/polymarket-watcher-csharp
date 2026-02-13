using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolymarketWatcher;

public class MarketInfo
{
    public string? ConditionId { get; set; }
    public string? Question { get; set; }
    public string? Slug { get; set; }
    public List<TokenInfo> Tokens { get; set; } = new();
}

public class TokenInfo
{
    public string TokenId { get; set; } = "";
    public string Outcome { get; set; } = "";
}

public static class PolymarketApi
{
    private const string ClobBaseUrl = "https://clob.polymarket.com";
    private const string GammaBaseUrl = "https://gamma-api.polymarket.com";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Resolve a market slug to token IDs and market info via the Gamma API.
    /// </summary>
    public static async Task<MarketInfo> ResolveMarketAsync(string slug)
    {
        var url = $"{GammaBaseUrl}/markets?slug={Uri.EscapeDataString(slug)}";
        var resp = await Http.GetStringAsync(url);
        var markets = JsonSerializer.Deserialize<List<GammaMarket>>(resp, JsonOptions);

        if (markets == null || markets.Count == 0)
            throw new Exception($"No market found for slug: {slug}");

        var market = markets[0];
        var info = new MarketInfo
        {
            ConditionId = market.ConditionId,
            Question = market.Question ?? slug,
            Slug = slug,
        };

        var tokenIds = ParseJsonStringArray(market.ClobTokenIds);
        var outcomes = ParseJsonStringArray(market.Outcomes);

        for (int i = 0; i < tokenIds.Count && i < outcomes.Count; i++)
        {
            info.Tokens.Add(new TokenInfo
            {
                TokenId = tokenIds[i],
                Outcome = outcomes[i],
            });
        }

        return info;
    }

    /// <summary>
    /// Resolve a market by condition_id via the CLOB API.
    /// </summary>
    public static async Task<MarketInfo> ResolveMarketByConditionIdAsync(string conditionId)
    {
        var url = $"{ClobBaseUrl}/markets/{conditionId}";
        var resp = await Http.GetStringAsync(url);
        var market = JsonSerializer.Deserialize<ClobMarket>(resp, JsonOptions);

        if (market == null)
            throw new Exception($"No market found for condition_id: {conditionId}");

        var info = new MarketInfo
        {
            ConditionId = conditionId,
            Question = market.Question ?? conditionId,
        };

        if (market.Tokens != null)
        {
            foreach (var token in market.Tokens)
            {
                info.Tokens.Add(new TokenInfo
                {
                    TokenId = token.TokenId ?? "",
                    Outcome = token.Outcome ?? "Unknown",
                });
            }
        }

        return info;
    }

    /// <summary>
    /// Fetch the mid-price for a token from the CLOB order book.
    /// Returns null if no book data is available.
    /// </summary>
    public static async Task<double?> GetMidPriceAsync(string tokenId)
    {
        var url = $"{ClobBaseUrl}/book?token_id={Uri.EscapeDataString(tokenId)}";
        var resp = await Http.GetStringAsync(url);
        var book = JsonSerializer.Deserialize<OrderBook>(resp, JsonOptions);

        if (book == null)
            return null;

        double? bestBid = book.Bids?.Count > 0
            ? book.Bids.Max(b => double.Parse(b.Price ?? "0"))
            : null;
        double? bestAsk = book.Asks?.Count > 0
            ? book.Asks.Min(a => double.Parse(a.Price ?? "0"))
            : null;

        if (bestBid.HasValue && bestAsk.HasValue)
            return (bestBid.Value + bestAsk.Value) / 2;
        if (bestBid.HasValue)
            return bestBid.Value;
        if (bestAsk.HasValue)
            return bestAsk.Value;

        return null;
    }

    /// <summary>
    /// Parse a field that might be a JSON array string or an actual array.
    /// </summary>
    private static List<string> ParseJsonStringArray(object? value)
    {
        if (value == null)
            return new List<string>();

        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                return element.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .ToList();
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                var str = element.GetString();
                if (str != null)
                {
                    var parsed = JsonSerializer.Deserialize<List<string>>(str);
                    return parsed ?? new List<string>();
                }
            }
        }

        return new List<string>();
    }

    // --- JSON models for deserialization ---

    private class GammaMarket
    {
        [JsonPropertyName("conditionId")]
        public string? ConditionId { get; set; }

        [JsonPropertyName("condition_id")]
        public string? ConditionIdAlt { get; set; }

        [JsonPropertyName("question")]
        public string? Question { get; set; }

        [JsonPropertyName("clobTokenIds")]
        public object? ClobTokenIds { get; set; }

        [JsonPropertyName("outcomes")]
        public object? Outcomes { get; set; }
    }

    private class ClobMarket
    {
        [JsonPropertyName("question")]
        public string? Question { get; set; }

        [JsonPropertyName("tokens")]
        public List<ClobToken>? Tokens { get; set; }
    }

    private class ClobToken
    {
        [JsonPropertyName("token_id")]
        public string? TokenId { get; set; }

        [JsonPropertyName("outcome")]
        public string? Outcome { get; set; }
    }

    private class OrderBook
    {
        [JsonPropertyName("bids")]
        public List<BookEntry>? Bids { get; set; }

        [JsonPropertyName("asks")]
        public List<BookEntry>? Asks { get; set; }
    }

    private class BookEntry
    {
        [JsonPropertyName("price")]
        public string? Price { get; set; }
    }
}
