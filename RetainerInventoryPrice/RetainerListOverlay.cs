using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace RetainerInventoryPrice;

public unsafe class RetainerListOverlay : IDisposable
{
    private const float StartYOffset = 80f;
    private const float RowHeight = 25f;
    private const float ColumnXOffset = 175f;

    public RetainerListOverlay()
    {
        Svc.PluginInterface.UiBuilder.Draw += Draw;
    }

    public void Dispose()
    {
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
    }

    private void Draw()
    {
        var addonRaw = Svc.GameGui.GetAddonByName("RetainerList");
        var addonPtr = (nint)addonRaw;

        if (addonPtr == nint.Zero) return;

        var addon = (AtkUnitBase*)addonPtr;

        if (addon == null || !addon->IsVisible || addon->Scale <= 0) return;

        var manager = RetainerManager.Instance();
        if (manager == null) return;

        int activeRow = 0;
        for (int i = 0; i < 10; i++)
        {
            var retainer = manager->Retainers[i];
            if (retainer.RetainerId == 0) continue;

            long totalValue = 0;
            if (Plugin.Instance.Configuration.RetainerInventories.TryGetValue(retainer.RetainerId, out var items))
            {
                foreach (var item in items)
                {
                    if (Plugin.Instance.Configuration.PriceCache.TryGetValue(item.ItemId, out var price))
                    {
                        totalValue += price * item.Quantity;
                    }
                }
            }

            var x = addon->X + (ColumnXOffset * addon->Scale);
            var y = addon->Y + ((StartYOffset + (activeRow * RowHeight)) * addon->Scale);

            if (totalValue > 0)
            {
                var text = $"{totalValue:N0} G";

                ImGui.SetNextWindowPos(new Vector2(x, y));

                ImGui.SetNextWindowSize(new Vector2(150, 0));

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

                ImGui.Begin($"RetOverlay_{retainer.RetainerId}",
                    ImGuiWindowFlags.NoDecoration |
                    ImGuiWindowFlags.NoBackground |
                    ImGuiWindowFlags.NoInputs |
                    ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.NoNav |
                    ImGuiWindowFlags.NoSavedSettings |
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoBringToFrontOnFocus);

                ImGui.SetCursorPos(new Vector2(1, 1));
                ImGui.TextColored(new Vector4(0, 0, 0, 1), text);

                ImGui.SetCursorPos(new Vector2(0, 0));
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), text);

                ImGui.End();

                ImGui.PopStyleVar();
            }

            activeRow++;
        }
    }
}