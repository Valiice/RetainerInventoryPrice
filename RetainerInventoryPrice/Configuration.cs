using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace RetainerInventoryPrice;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public Dictionary<ulong, List<SavedItem>> RetainerInventories { get; set; } = [];
    public Dictionary<uint, long> PriceCache { get; set; } = [];
    public Dictionary<uint, long> DcPriceCache { get; set; } = [];
    public Dictionary<uint, DateTime> PriceCacheTimestamps { get; set; } = [];
    public Dictionary<ulong, string> RetainerNames { get; set; } = [];
    public List<SavedItem> PlayerBags { get; set; } = [];
    public List<SavedItem> PlayerCrystals { get; set; } = [];
    public List<NetWorthSnapshot> NetWorthHistory { get; set; } = [];

    [NonSerialized]
    private IDalamudPluginInterface? _pluginInterface;

    [JsonIgnore]
    public readonly object Lock = new();

    public static Configuration Get(IDalamudPluginInterface pluginInterface)
    {
        var config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        config._pluginInterface = pluginInterface;
        return config;
    }

    public void Save()
    {
        _pluginInterface?.SavePluginConfig(this);
    }
}

[Serializable]
public class SavedItem
{
    public uint ItemId { get; set; }
    public int Quantity { get; set; }
    public bool IsHq { get; set; }
    public string Name { get; set; } = "";
}

[Serializable]
public class NetWorthSnapshot
{
    public DateTime Timestamp { get; set; }
    public long WorldTotal { get; set; }
    public long DcTotal { get; set; }
}
