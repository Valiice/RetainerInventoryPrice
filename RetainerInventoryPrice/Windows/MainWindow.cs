using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System.Linq;
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

        // --- HEADER ---
        ImGui.Columns(3, "RetainerList", false);
        ImGui.SetColumnWidth(0, 250);
        ImGui.Text("Retainer Name");
        ImGui.NextColumn();
        ImGui.Text("Item Count");
        ImGui.NextColumn();
        ImGui.Text("Est. Total Value");
        ImGui.NextColumn();
        ImGui.Columns(1); // Reset columns to draw the separator correctly
        ImGui.Separator();

        long grandTotal = 0;

        foreach (var retainer in Plugin.Instance.Configuration.RetainerInventories)
        {
            var id = retainer.Key;
            var items = retainer.Value;
            var name = Plugin.Instance.Configuration.RetainerNames.TryGetValue(id, out var n) ? n : $"{id:X}";

            // 1. Calculate Total Value
            long retainerTotal = 0;
            foreach (var item in items)
            {
                if (Plugin.Instance.Configuration.PriceCache.TryGetValue(item.ItemId, out var price))
                {
                    retainerTotal += price * item.Quantity;
                }
            }
            grandTotal += retainerTotal;

            // --- THE FIX IS HERE ---
            // 2. Start Columns FIRST, then draw the Tree Node inside Column 0
            ImGui.Columns(3, $"RetainerCols_{id}", false);
            ImGui.SetColumnWidth(0, 250);

            // Align text vertically so it looks nice with the arrow
            ImGui.AlignTextToFramePadding();

            // Draw the Arrow
            bool expanded = ImGui.TreeNode($"##{id}");

            // Draw the Name on the same line, just after the arrow
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), name);

            // Move to next columns
            ImGui.NextColumn();
            ImGui.Text($"{items.Count} items");

            ImGui.NextColumn();
            ImGui.Text($"{retainerTotal:N0} gil");

            // Reset columns before drawing the inner table
            ImGui.Columns(1);

            // 3. Draw the Items Table (if expanded)
            if (expanded)
            {
                var sortedItems = items
                    .Select(item => {
                        long price = Plugin.Instance.Configuration.PriceCache.TryGetValue(item.ItemId, out var p) ? p : 0;
                        return new { Item = item, UnitPrice = price, TotalValue = price * item.Quantity };
                    })
                    .OrderByDescending(x => x.TotalValue)
                    .ToList();

                // Indent the table slightly so it looks "inside" the retainer
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