# Polymarket Watcher (C#)

Monitor Polymarket prices and get terminal alerts when thresholds are crossed.

## Setup

```bash
# Requires .NET 8 SDK
cd PolymarketWatcher
dotnet restore
cp ../watches.example.yaml watches.yaml
# Edit watches.yaml with the markets you want to watch
```

## Usage

```bash
cd PolymarketWatcher
dotnet run                           # Start watching (polls every 30s)
dotnet run -- -c ../my-config.yaml   # Use a custom config file
dotnet run -- --once                 # Poll once and exit
```

## Configuration

Edit `watches.yaml` to define which markets to watch and when to alert.

```yaml
poll_interval: 30  # seconds

watches:
  - slug: "will-ethereum-hit-5000"    # from the Polymarket URL
    name: "ETH $5000"                  # friendly label
    alerts:
      - direction: above               # "above" or "below"
        threshold: 0.25                 # YES token price (0.25 = 25%)
        message: "ETH heating up!"      # printed when triggered
```

### Finding market slugs

The slug comes from the Polymarket URL. For example:
- `https://polymarket.com/event/will-ethereum-hit-5000` -> slug: `will-ethereum-hit-5000`

### Alert behavior

- Alerts fire **once** when the threshold is first crossed
- They reset when the price crosses back, allowing them to fire again
- Prices are the mid-price (average of best bid and best ask) from the CLOB order book

## Project Structure

```
PolymarketWatcher/
├── Program.cs          # Entry point - polling loop
├── WatchConfig.cs      # YAML config models + loader
├── PolymarketApi.cs    # REST API client (Gamma + CLOB)
├── Alert.cs            # Alert logic (threshold crossing detection)
└── WatchState.cs       # Runtime state per watched market
```
