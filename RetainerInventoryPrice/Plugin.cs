using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;
using RetainerInventoryPrice.Windows;

namespace RetainerInventoryPrice;

public class Plugin : IDalamudPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    public Configuration Configuration { get; private set; }
    public WindowSystem WindowSystem = new("RetainerInventoryPrice");

    public RetainerScanner Scanner { get; private set; }
    public PlayerScanner PlayerScanner { get; private set; }
    public PriceFetcher PriceFetcher { get; private set; }

    public MainWindow MainWindow { get; private set; }
    public RetainerListOverlay Overlay { get; private set; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        Instance = this;
        ECommonsMain.Init(pluginInterface, this);

        Configuration = Configuration.Get(pluginInterface);

        Scanner = new RetainerScanner();
        PlayerScanner = new PlayerScanner();
        PriceFetcher = new PriceFetcher();

        MainWindow = new MainWindow();
        WindowSystem.AddWindow(MainWindow);

        Overlay = new RetainerListOverlay();

        Svc.Commands.AddHandler("/retainerprice", new CommandInfo((_, _) => MainWindow.IsOpen = true)
        {
            HelpMessage = "Opens the Retainer Inventory Price window"
        });

        Svc.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += () => MainWindow.IsOpen = true;
        Svc.PluginInterface.UiBuilder.OpenMainUi += () => MainWindow.IsOpen = true;
    }

    public long GetRetainerValue(ulong retainerId) => GetRetainerValue(retainerId, Configuration.PriceCache);
    public long GetRetainerValueDc(ulong retainerId) => GetRetainerValue(retainerId, Configuration.DcPriceCache);
    private long GetRetainerValue(ulong retainerId, Dictionary<uint, long> cache)
    {
        if (!Configuration.RetainerInventories.TryGetValue(retainerId, out var items)) return 0;
        lock (Configuration.Lock)
            return items.Sum(item => cache.TryGetValue(item.ItemId, out var price) ? price * item.Quantity : 0);
    }

    public long GetPlayerBagsValue() => GetPlayerValue(Configuration.PlayerBags, Configuration.PriceCache);
    public long GetPlayerBagsValueDc() => GetPlayerValue(Configuration.PlayerBags, Configuration.DcPriceCache);
    public long GetPlayerCrystalsValue() => GetPlayerValue(Configuration.PlayerCrystals, Configuration.PriceCache);
    public long GetPlayerCrystalsValueDc() => GetPlayerValue(Configuration.PlayerCrystals, Configuration.DcPriceCache);
    private long GetPlayerValue(List<SavedItem> items, Dictionary<uint, long> cache)
    {
        lock (Configuration.Lock)
            return items.Sum(item => cache.TryGetValue(item.ItemId, out var price) ? price * item.Quantity : 0);
    }

    public void Dispose()
    {
        Svc.Commands.RemoveHandler("/retainerprice");

        WindowSystem.RemoveAllWindows();
        Overlay?.Dispose();
        PlayerScanner?.Dispose();

        ECommonsMain.Dispose();

        GC.SuppressFinalize(this);
    }
}