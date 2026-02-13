namespace PolymarketWatcher.Tests;

public class AlertTests
{
    [Fact]
    public void AboveAlert_Fires_WhenPriceCrossesThreshold()
    {
        var alert = new Alert("above", 0.50, "crossed 50%");

        var result = alert.Check(0.55);

        Assert.Equal("crossed 50%", result);
        Assert.True(alert.Triggered);
    }

    [Fact]
    public void BelowAlert_Fires_WhenPriceCrossesThreshold()
    {
        var alert = new Alert("below", 0.30, "dropped below 30%");

        var result = alert.Check(0.25);

        Assert.Equal("dropped below 30%", result);
        Assert.True(alert.Triggered);
    }

    [Fact]
    public void AboveAlert_DoesNotFire_WhenPriceBelowThreshold()
    {
        var alert = new Alert("above", 0.50, "crossed 50%");

        var result = alert.Check(0.40);

        Assert.Null(result);
        Assert.False(alert.Triggered);
    }

    [Fact]
    public void BelowAlert_DoesNotFire_WhenPriceAboveThreshold()
    {
        var alert = new Alert("below", 0.30, "dropped below 30%");

        var result = alert.Check(0.40);

        Assert.Null(result);
        Assert.False(alert.Triggered);
    }

    [Fact]
    public void Alert_FiresOnce_ThenSuppresses()
    {
        var alert = new Alert("above", 0.50, "crossed 50%");

        var first = alert.Check(0.55);
        var second = alert.Check(0.60);

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public void Alert_ResetsAndFiresAgain_AfterPriceCrossesBack()
    {
        var alert = new Alert("above", 0.50, "crossed 50%");

        var first = alert.Check(0.55);  // fires
        alert.Check(0.40);              // crosses back, resets
        var third = alert.Check(0.55);  // fires again

        Assert.NotNull(first);
        Assert.NotNull(third);
    }

    [Fact]
    public void Alert_FiresAtExactThreshold()
    {
        var alert = new Alert("above", 0.50, "hit 50%");

        var result = alert.Check(0.50);

        Assert.Equal("hit 50%", result);
    }

    [Fact]
    public void BelowAlert_FiresAtExactThreshold()
    {
        var alert = new Alert("below", 0.30, "hit 30%");

        var result = alert.Check(0.30);

        Assert.Equal("hit 30%", result);
    }

    [Fact]
    public void Format_ContainsExpectedParts()
    {
        var alert = new Alert("above", 0.50, "crossed 50%");
        alert.Check(0.55); // trigger it

        var formatted = Alert.Format("TestMarket", 0.55, alert, "crossed 50%");

        Assert.Contains("TestMarket", formatted);
        Assert.Contains("0.5500", formatted);
        Assert.Contains("55.0%", formatted);
        Assert.Contains("0.5000", formatted);
        Assert.Contains("crossed 50%", formatted);
        Assert.Contains("\u2191", formatted); // up arrow for "above"
    }

    [Fact]
    public void Format_UsesDownArrow_ForBelowAlert()
    {
        var alert = new Alert("below", 0.30, "dropped");

        var formatted = Alert.Format("TestMarket", 0.25, alert, "dropped");

        Assert.Contains("\u2193", formatted); // down arrow
    }
}
