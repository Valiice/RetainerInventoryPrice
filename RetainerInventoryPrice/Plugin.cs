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

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        Instance = this;
        ECommonsMain.Init(pluginInterface, this);

        Configuration = Configuration.Get(pluginInterface);

        Scanner = new RetainerScanner();
        PriceFetcher = new PriceFetcher();

        MainWindow = new MainWindow();
        WindowSystem.AddWindow(MainWindow);

        Svc.Commands.AddHandler("/retainerprice", new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Retainer Inventory Price window"
        });

        Svc.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += () => MainWindow.IsOpen = true;
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.IsOpen = true;
    }

    public void Dispose()
    {
        Svc.Commands.RemoveHandler("/retainerprice");
        WindowSystem.RemoveAllWindows();
        ECommonsMain.Dispose();
    }
}