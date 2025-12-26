using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace RetainerInventoryPrice;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // RetainerID -> List of Items
    public Dictionary<ulong, List<SavedItem>> RetainerInventories { get; set; } = new();

    // ItemID -> Price
    public Dictionary<uint, long> PriceCache { get; set; } = new();

    // NEW: RetainerID -> Retainer Name
    public Dictionary<ulong, string> RetainerNames { get; set; } = new();

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public static Configuration Get(IDalamudPluginInterface pluginInterface)
    {
        var config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        config.pluginInterface = pluginInterface;
        return config;
    }

    public void Save()
    {
        pluginInterface?.SavePluginConfig(this);
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