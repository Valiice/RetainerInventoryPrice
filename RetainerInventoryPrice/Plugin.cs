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
    public PriceFetcher PriceFetcher { get; private set; }

    public MainWindow MainWindow { get; private set; }
    public RetainerListOverlay Overlay { get; private set; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        Instance = this;
        ECommonsMain.Init(pluginInterface, this);

        Configuration = Configuration.Get(pluginInterface);

        Scanner = new RetainerScanner();
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
    }

    public long GetRetainerValue(ulong retainerId)
    {
        if (!Configuration.RetainerInventories.TryGetValue(retainerId, out var items)) return 0;
        lock (Configuration.Lock)
        {
            return items.Sum(item => Configuration.PriceCache.TryGetValue(item.ItemId, out var price) ? price * item.Quantity : 0);
        }
    }

    public void Dispose()
    {
        Svc.Commands.RemoveHandler("/retainerprice");

        WindowSystem.RemoveAllWindows();
        Overlay?.Dispose();

        ECommonsMain.Dispose();

        GC.SuppressFinalize(this);
    }
}