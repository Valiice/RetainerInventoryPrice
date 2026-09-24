using Dalamud.Configuration;
using Dalamud.Plugin;
using ECommons.DalamudServices;
using Newtonsoft.Json;

namespace RetainerInventoryPrice;

[Serializable]
public class Configuration : IPluginConfiguration, IDisposable
{
    private static readonly TimeSpan SaveDelay = TimeSpan.FromSeconds(2);

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

    [NonSerialized]
    private readonly object _saveGate = new();

    [NonSerialized]
    private Timer? _saveTimer;

    [NonSerialized]
    private int _dirty;

    [JsonIgnore]
    public readonly object Lock = new();

    public static Configuration Get(IDalamudPluginInterface pluginInterface)
    {
        var config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        config._pluginInterface = pluginInterface;
        return config;
    }

    /// <summary>
    /// Marks the configuration as changed and writes it to disk two seconds later on a
    /// thread-pool thread. A burst of changes writes once, and the game thread never waits
    /// on file I/O. Call <see cref="Flush"/> before unloading so nothing is lost.
    /// </summary>
    public void Save()
    {
        Interlocked.Exchange(ref _dirty, 1);
        lock (_saveGate)
        {
            _saveTimer ??= new Timer(_ => Flush(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _saveTimer.Change(SaveDelay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>Writes to disk now if there are unsaved changes. Safe to call from any thread.</summary>
    public void Flush()
    {
        if (Interlocked.Exchange(ref _dirty, 0) == 0) return;

        try
        {
            lock (Lock)
                _pluginInterface?.SavePluginConfig(this);
        }
        catch (Exception ex)
        {
            Svc.Log.Warning($"Configuration save failed, retrying: {ex.Message}");
            Save();
        }
    }

    public void Dispose()
    {
        lock (_saveGate)
        {
            _saveTimer?.Dispose();
            _saveTimer = null;
        }

        Flush();
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
