using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace RetainerInventoryPrice.Windows;

public class MainWindow : Window
{
    public MainWindow() : base("Retainer Inventory Price")
    {
        Size = new Vector2(800, 500);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var config = Plugin.Instance.Configuration;

        long retainersTotal = config.RetainerInventories.Keys.Sum(id => Plugin.Instance.GetRetainerValue(id));
        long retainersTotalDc = config.RetainerInventories.Keys.Sum(id => Plugin.Instance.GetRetainerValueDc(id));
        long bagsTotal = Plugin.Instance.GetPlayerBagsValue();
        long bagsTotalDc = Plugin.Instance.GetPlayerBagsValueDc();
        long crystalsTotal = Plugin.Instance.GetPlayerCrystalsValue();
        long crystalsTotalDc = Plugin.Instance.GetPlayerCrystalsValueDc();
        long grandTotal = retainersTotal + bagsTotal + crystalsTotal;
        long grandTotalDc = retainersTotalDc + bagsTotalDc + crystalsTotalDc;

        if (ImGui.BeginTabBar("MainTabs"))
        {
            if (ImGui.BeginTabItem("Retainers"))
            {
                DrawRetainersTab(config, retainersTotal, retainersTotalDc);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Character"))
            {
                DrawCharacterTab(config, bagsTotal, bagsTotalDc, crystalsTotal, crystalsTotalDc);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(1, 0.8f, 0, 1), $"Grand Total — World: {grandTotal:N0} gil");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.6f, 1f, 0.6f, 1), $"| DC: {grandTotalDc:N0} gil");
        ImGui.SameLine();
        ImGui.TextDisabled($"({config.PriceCache.Count} cached)");
        if (ImGui.Button("Clear Price Cache"))
        {
            lock (config.Lock)
            {
                config.PriceCache.Clear();
                config.DcPriceCache.Clear();
            }
            config.Save();
            Plugin.Instance.PlayerScanner.ScanNow();
        }
    }

    private static void DrawRetainersTab(Configuration config, long retainersTotal, long retainersTotalDc)
    {
        ImGui.TextWrapped("Open your retainers one by one to scan their inventories.");
        ImGui.Separator();

        DrawRetainerListHeader();
        ImGui.Separator();

        foreach (var (id, items) in config.RetainerInventories)
        {
            var name = config.RetainerNames.TryGetValue(id, out var n) ? n : $"{id:X}";
            DrawRetainerRow(id, name, items.Count,
                Plugin.Instance.GetRetainerValue(id),
                Plugin.Instance.GetRetainerValueDc(id),
                items);
            ImGui.Separator();
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.6f, 0.9f, 1f, 1f), $"Retainers Subtotal — World: {retainersTotal:N0} gil");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.6f, 1f, 0.6f, 1f), $"| DC: {retainersTotalDc:N0} gil");
    }

    private static void DrawCharacterTab(Configuration config, long bagsTotal, long bagsTotalDc, long crystalsTotal, long crystalsTotalDc)
    {
        if (ImGui.Button("Scan Now"))
            Plugin.Instance.PlayerScanner.ScanNow();
        ImGui.SameLine();
        ImGui.TextDisabled("Scans automatically every 3 seconds");

        ImGui.Spacing();

        if (ImGui.BeginTabBar("CharacterTabs"))
        {
            if (ImGui.BeginTabItem("Bags"))
            {
                ImGui.Spacing();
                ImGui.Text($"{config.PlayerBags.Count} items");
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.6f, 0.9f, 1f, 1f), $"World: {bagsTotal:N0} gil");
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.6f, 1f, 0.6f, 1f), $"| DC: {bagsTotalDc:N0} gil");
                ImGui.Spacing();
                DrawItemsTable("PlayerBags", config.PlayerBags);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Crystals"))
            {
                ImGui.Spacing();
                ImGui.Text($"{config.PlayerCrystals.Count} items");
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.6f, 0.9f, 1f, 1f), $"World: {crystalsTotal:N0} gil");
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.6f, 1f, 0.6f, 1f), $"| DC: {crystalsTotalDc:N0} gil");
                ImGui.Spacing();
                DrawItemsTable("PlayerCrystals", config.PlayerCrystals);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private static void DrawRetainerListHeader()
    {
        ImGui.Columns(4, "RetainerList", false);
        ImGui.SetColumnWidth(0, 200);
        ImGui.SetColumnWidth(1, 80);
        ImGui.SetColumnWidth(2, 150);
        ImGui.SetColumnWidth(3, 150);
        ImGui.Text("Retainer Name");
        ImGui.NextColumn();
        ImGui.Text("Items");
        ImGui.NextColumn();
        ImGui.Text("World Value");
        ImGui.NextColumn();
        ImGui.Text("DC Value");
        ImGui.NextColumn();
        ImGui.Columns(1);
    }

    private static void DrawRetainerRow(ulong id, string name, int itemCount, long total, long totalDc, List<SavedItem> items)
    {
        ImGui.Columns(4, $"RetainerCols_{id}", false);
        ImGui.SetColumnWidth(0, 200);
        ImGui.SetColumnWidth(1, 80);
        ImGui.SetColumnWidth(2, 150);
        ImGui.SetColumnWidth(3, 150);
        ImGui.AlignTextToFramePadding();

        bool expanded = ImGui.TreeNode($"##{id}");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), name);
        ImGui.NextColumn();
        ImGui.Text($"{itemCount}");
        ImGui.NextColumn();
        ImGui.Text($"{total:N0} gil");
        ImGui.NextColumn();
        ImGui.Text($"{totalDc:N0} gil");
        ImGui.Columns(1);

        if (expanded)
        {
            DrawItemsTable(id, items);
            ImGui.TreePop();
        }
    }

    private static void DrawItemsTable(ulong id, List<SavedItem> items) =>
        DrawItemsTable($"{id}", items);

    private static void DrawItemsTable(string idPrefix, List<SavedItem> items)
    {
        var sortedItems = items.Select(item =>
        {
            lock (Plugin.Instance.Configuration.Lock)
            {
                var worldPrice = Plugin.Instance.Configuration.PriceCache.TryGetValue(item.ItemId, out var wp) ? wp : 0;
                var dcPrice = Plugin.Instance.Configuration.DcPriceCache.TryGetValue(item.ItemId, out var dp) ? dp : 0;
                return new
                {
                    Item = item,
                    WorldPrice = worldPrice,
                    WorldTotal = worldPrice * item.Quantity,
                    DcPrice = dcPrice,
                    DcTotal = dcPrice * item.Quantity
                };
            }
        }).OrderByDescending(x => x.WorldTotal).ToList();

        ImGui.Indent(20f);
        if (ImGui.BeginTable($"Items_{idPrefix}", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Item Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Qty");
            ImGui.TableSetupColumn("World Price");
            ImGui.TableSetupColumn("World Total");
            ImGui.TableSetupColumn("DC Price");
            ImGui.TableSetupColumn("DC Total");
            ImGui.TableHeadersRow();

            foreach (var entry in sortedItems)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(entry.Item.IsHq ? $"{entry.Item.Name} (HQ)" : entry.Item.Name);
                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Item.Quantity}");
                ImGui.TableNextColumn();
                ImGui.Text($"{entry.WorldPrice:N0}");
                ImGui.TableNextColumn();
                ImGui.Text($"{entry.WorldTotal:N0}");
                ImGui.TableNextColumn();
                ImGui.Text($"{entry.DcPrice:N0}");
                ImGui.TableNextColumn();
                ImGui.Text($"{entry.DcTotal:N0}");
            }
            ImGui.EndTable();
        }
        ImGui.Unindent(20f);
    }
}
