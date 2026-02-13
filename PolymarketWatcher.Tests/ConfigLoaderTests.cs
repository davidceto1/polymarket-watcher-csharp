namespace PolymarketWatcher.Tests;

public class ConfigLoaderTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    private string WriteTempYaml(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var f in _tempFiles)
            if (File.Exists(f)) File.Delete(f);
    }

    [Fact]
    public void Load_ParsesValidConfig()
    {
        var path = WriteTempYaml("""
            poll_interval: 15
            watches:
              - slug: "test-market"
                name: "Test"
                alerts:
                  - direction: above
                    threshold: 0.5
                    message: "above 50%"
            """);

        var config = ConfigLoader.Load(path);

        Assert.Equal(15, config.PollInterval);
        Assert.Single(config.Watches);
        Assert.Equal("test-market", config.Watches[0].Slug);
        Assert.Equal("Test", config.Watches[0].Name);
        Assert.Single(config.Watches[0].Alerts);
        Assert.Equal("above", config.Watches[0].Alerts[0].Direction);
        Assert.Equal(0.5, config.Watches[0].Alerts[0].Threshold);
        Assert.Equal("above 50%", config.Watches[0].Alerts[0].Message);
    }

    [Fact]
    public void Load_DefaultsMessageWhenOmitted()
    {
        var path = WriteTempYaml("""
            watches:
              - slug: "test-market"
                name: "Test"
                alerts:
                  - direction: above
                    threshold: 0.75
            """);

        var config = ConfigLoader.Load(path);

        Assert.Contains("0.75", config.Watches[0].Alerts[0].Message);
    }

    [Fact]
    public void Load_DefaultsPollInterval_WhenInvalid()
    {
        var path = WriteTempYaml("""
            poll_interval: -5
            watches:
              - slug: "test-market"
                name: "Test"
                alerts:
                  - direction: below
                    threshold: 0.1
                    message: "low"
            """);

        var config = ConfigLoader.Load(path);

        Assert.Equal(30, config.PollInterval);
    }

    [Fact]
    public void Load_SupportsConditionId()
    {
        var path = WriteTempYaml("""
            watches:
              - condition_id: "0xabc123"
                name: "By Condition"
                alerts:
                  - direction: above
                    threshold: 0.5
                    message: "test"
            """);

        var config = ConfigLoader.Load(path);

        Assert.Equal("0xabc123", config.Watches[0].ConditionId);
    }

    [Fact]
    public void Load_SupportsMultipleAlerts()
    {
        var path = WriteTempYaml("""
            watches:
              - slug: "test-market"
                name: "Test"
                alerts:
                  - direction: above
                    threshold: 0.25
                    message: "low trigger"
                  - direction: above
                    threshold: 0.75
                    message: "high trigger"
                  - direction: below
                    threshold: 0.10
                    message: "very low"
            """);

        var config = ConfigLoader.Load(path);

        Assert.Equal(3, config.Watches[0].Alerts.Count);
    }

    [Fact]
    public void Load_DefaultsNameToSlug_WhenNameOmitted()
    {
        var path = WriteTempYaml("""
            watches:
              - slug: "my-market-slug"
                alerts:
                  - direction: above
                    threshold: 0.5
                    message: "test"
            """);

        var config = ConfigLoader.Load(path);

        Assert.Equal("my-market-slug", config.Watches[0].Name);
    }
}
