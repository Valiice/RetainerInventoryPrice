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

        ImGui.Columns(3, "RetainerList", false);
        ImGui.SetColumnWidth(0, 250);
        ImGui.Text("Retainer Name");
        ImGui.NextColumn();
        ImGui.Text("Item Count");
        ImGui.NextColumn();
        ImGui.Text("Est. Total Value");
        ImGui.NextColumn();
        ImGui.Columns(1);
        ImGui.Separator();

        long grandTotal = 0;

        foreach (var retainer in Plugin.Instance.Configuration.RetainerInventories)
        {
            var id = retainer.Key;
            var items = retainer.Value;
            var name = Plugin.Instance.Configuration.RetainerNames.TryGetValue(id, out var n) ? n : $"{id:X}";

            long retainerTotal = 0;
            foreach (var item in items)
            {
                if (Plugin.Instance.Configuration.PriceCache.TryGetValue(item.ItemId, out var price))
                {
                    retainerTotal += price * item.Quantity;
                }
            }
            grandTotal += retainerTotal;

            ImGui.Columns(3, $"RetainerCols_{id}", false);
            ImGui.SetColumnWidth(0, 250);

            ImGui.AlignTextToFramePadding();

            bool expanded = ImGui.TreeNode($"##{id}");

            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), name);

            ImGui.NextColumn();
            ImGui.Text($"{items.Count} items");

            ImGui.NextColumn();
            ImGui.Text($"{retainerTotal:N0} gil");

            ImGui.Columns(1);

            if (expanded)
            {
                var sortedItems = items
                    .Select(item => {
                        long price = Plugin.Instance.Configuration.PriceCache.TryGetValue(item.ItemId, out var p) ? p : 0;
                        return new { Item = item, UnitPrice = price, TotalValue = price * item.Quantity };
                    })
                    .OrderByDescending(x => x.TotalValue)
                    .ToList();

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
                        var nameText = entry.Item.IsHq ? $"{entry.Item.Name} (HQ)" : entry.Item.Name;
                        ImGui.Text(nameText);
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
                ImGui.TreePop();
            }
            ImGui.Separator();
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(1, 0.8f, 0, 1), $"Grand Total: {grandTotal:N0} gil");
    }
}