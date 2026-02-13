using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PolymarketWatcher;

public class WatchConfig
{
    [YamlMember(Alias = "poll_interval")]
    public int PollInterval { get; set; } = 30;

    [YamlMember(Alias = "watches")]
    public List<WatchEntry> Watches { get; set; } = new();
}

public class WatchEntry
{
    [YamlMember(Alias = "slug")]
    public string? Slug { get; set; }

    [YamlMember(Alias = "condition_id")]
    public string? ConditionId { get; set; }

    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "alerts")]
    public List<AlertConfig> Alerts { get; set; } = new();
}

public class AlertConfig
{
    [YamlMember(Alias = "direction")]
    public string Direction { get; set; } = "above";

    [YamlMember(Alias = "threshold")]
    public double Threshold { get; set; }

    [YamlMember(Alias = "message")]
    public string? Message { get; set; }
}

public static class ConfigLoader
{
    public static WatchConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Config file not found: {path}");
            Console.Error.WriteLine("Copy watches.example.yaml to watches.yaml and edit it.");
            Environment.Exit(1);
        }

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var config = deserializer.Deserialize<WatchConfig>(yaml);

        if (config.Watches.Count == 0)
        {
            Console.Error.WriteLine("Config must contain at least one watch entry");
            Environment.Exit(1);
        }

        foreach (var watch in config.Watches)
        {
            if (string.IsNullOrEmpty(watch.Slug) && string.IsNullOrEmpty(watch.ConditionId))
            {
                Console.Error.WriteLine($"Watch entry '{watch.Name ?? "(unnamed)"}' missing 'slug' or 'condition_id'");
                Environment.Exit(1);
            }

            watch.Name ??= watch.Slug ?? watch.ConditionId;

            if (watch.Alerts.Count == 0)
            {
                Console.Error.WriteLine($"Watch '{watch.Name}' has no alerts defined");
                Environment.Exit(1);
            }

            foreach (var alert in watch.Alerts)
            {
                if (alert.Direction is not ("above" or "below"))
                {
                    Console.Error.WriteLine($"Alert in '{watch.Name}': direction must be 'above' or 'below'");
                    Environment.Exit(1);
                }

                alert.Message ??= $"{watch.Name} crossed {alert.Threshold}";
            }
        }

        if (config.PollInterval < 1)
            config.PollInterval = 30;

        return config;
    }
}
