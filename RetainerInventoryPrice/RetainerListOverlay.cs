using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Numerics;

namespace RetainerInventoryPrice;

public unsafe class RetainerListOverlay : IDisposable
{
    private const float _startYOffset = 80f;
    private const float _rowHeight = 22f;
    private const float _columnXOffset = 160f;

    public RetainerListOverlay()
    {
        Svc.PluginInterface.UiBuilder.Draw += Draw;
    }

    public void Dispose()
    {
        Svc.PluginInterface.UiBuilder.Draw -= Draw;

        GC.SuppressFinalize(this);
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

            long totalValue = Plugin.Instance.GetRetainerValue(retainer.RetainerId);

            if (totalValue > 0)
            {
                var pos = new Vector2(
                    addon->X + (_columnXOffset * addon->Scale),
                    addon->Y + ((_startYOffset + (activeRow * _rowHeight)) * addon->Scale)
                );

                DrawOverlayText(retainer.RetainerId, pos, addon->Scale, $"{totalValue:N0} G");
            }
            activeRow++;
        }
    }

    private static void DrawOverlayText(ulong id, Vector2 pos, float scale, string text)
    {
        ImGui.SetNextWindowPos(pos);
        ImGui.SetNextWindowSize(new Vector2(220 * scale, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        if (ImGui.Begin($"RetOverlay_{id}", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBringToFrontOnFocus))
        {
            ImGui.SetWindowFontScale(scale);

            ImGui.SetCursorPos(new Vector2(1 * scale, 1 * scale));
            ImGui.TextColored(new Vector4(0, 0, 0, 1), text);

            ImGui.SetCursorPos(Vector2.Zero);
            ImGui.TextColored(new Vector4(0.95f, 0.95f, 0.95f, 1f), text);

            ImGui.End();
        }
        ImGui.PopStyleVar();
    }
}