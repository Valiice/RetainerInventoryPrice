using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace RetainerInventoryPrice.Windows;

public class MainWindow : Window
{
    public MainWindow() : base("Retainer Inventory Price")
    {
        Size = new Vector2(600, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        ImGui.TextWrapped("Open your retainers one by one to scan their inventories.");
        ImGui.Separator();

        DrawHeader();
        ImGui.Separator();

        long grandTotal = 0;
        var config = Plugin.Instance.Configuration;

        foreach (var (id, items) in config.RetainerInventories)
        {
            var name = config.RetainerNames.TryGetValue(id, out var n) ? n : $"{id:X}";
            var retainerTotal = Plugin.Instance.GetRetainerValue(id);
            grandTotal += retainerTotal;

            DrawRetainerRow(id, name, items.Count, retainerTotal, items);
            ImGui.Separator();
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(1, 0.8f, 0, 1), $"Grand Total: {grandTotal:N0} gil");
    }

    private static void DrawHeader()
    {
        ImGui.Columns(3, "RetainerList", false);
        ImGui.SetColumnWidth(0, 250);
        ImGui.Text("Retainer Name");
        ImGui.NextColumn();
        ImGui.Text("Item Count");
        ImGui.NextColumn();
        ImGui.Text("Est. Total Value");
        ImGui.NextColumn();
        ImGui.Columns(1);
    }

    private static void DrawRetainerRow(ulong id, string name, int itemCount, long total, List<SavedItem> items)
    {
        ImGui.Columns(3, $"RetainerCols_{id}", false);
        ImGui.SetColumnWidth(0, 250);
        ImGui.AlignTextToFramePadding();

        bool expanded = ImGui.TreeNode($"##{id}");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), name);
        ImGui.NextColumn();
        ImGui.Text($"{itemCount} items");
        ImGui.NextColumn();
        ImGui.Text($"{total:N0} gil");
        ImGui.Columns(1);

        if (expanded)
        {
            DrawItemsTable(id, items);
            ImGui.TreePop();
        }
    }

    private static void DrawItemsTable(ulong id, List<SavedItem> items)
    {
        var sortedItems = items.Select(item =>
        {
            lock (Plugin.Instance.Configuration.Lock)
            {
                var price = Plugin.Instance.Configuration.PriceCache.TryGetValue(item.ItemId, out var p) ? p : 0;
                return new { Item = item, UnitPrice = price, TotalValue = price * item.Quantity };
            }
        }).OrderByDescending(x => x.TotalValue).ToList();

        ImGui.Indent(20f);
        if (ImGui.BeginTable($"Items_{id}", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Item Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Qty");
            ImGui.TableSetupColumn("Unit Price");
            ImGui.TableSetupColumn("Total Value");
            ImGui.TableHeadersRow();

            foreach (var entry in sortedItems)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(entry.Item.IsHq ? $"{entry.Item.Name} (HQ)" : entry.Item.Name);
                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Item.Quantity}");
                ImGui.TableNextColumn();
                ImGui.Text($"{entry.UnitPrice:N0}");
                ImGui.TableNextColumn();
                ImGui.Text($"{entry.TotalValue:N0}");
            }
            ImGui.EndTable();
        }
        ImGui.Unindent(20f);
    }
}