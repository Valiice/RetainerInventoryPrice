using System.Text;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace RetainerInventoryPrice;

public unsafe class RetainerScanner
{
    private DateTime _lastScan = DateTime.MinValue;
    private readonly InventoryType[] _retainerPages =
    [
        InventoryType.RetainerPage1, InventoryType.RetainerPage2, InventoryType.RetainerPage3,
        InventoryType.RetainerPage4, InventoryType.RetainerPage5, InventoryType.RetainerPage6,
        InventoryType.RetainerPage7
    ];

    public RetainerScanner()
    {
        Svc.Framework.Update += OnUpdate;
    }

    private void OnUpdate(object framework)
    {
        if ((DateTime.Now - _lastScan).TotalSeconds < 3) return;

        try
        {
            var invManager = InventoryManager.Instance();
            if (invManager == null) return;

            var container = invManager->GetInventoryContainer(InventoryType.RetainerPage1);
            if (container == null || !container->IsLoaded) return;

            var retManager = RetainerManager.Instance();
            if (retManager == null) return;

            var activeRetainer = retManager->GetActiveRetainer();
            if (activeRetainer == null || activeRetainer->RetainerId == 0) return;

            _lastScan = DateTime.Now;
            ScanRetainer(activeRetainer->RetainerId, Encoding.UTF8.GetString(activeRetainer->Name).TrimEnd('\0'));
        }
        catch
        {
            // Ignore
        }
    }

    private void ScanRetainer(ulong retainerId, string name)
    {
        var itemsFound = new List<SavedItem>();
        var itemSheet = Svc.Data.GetExcelSheet<Item>();
        var manager = InventoryManager.Instance();

        foreach (var page in _retainerPages)
        {
            var container = manager->GetInventoryContainer(page);
            if (container == null || !container->IsLoaded) continue;

            for (var i = 0; i < container->Size; i++)
            {
                var item = container->Items[i];
                if (item.ItemId == 0) continue;

                itemsFound.Add(new SavedItem
                {
                    ItemId = item.ItemId,
                    Quantity = (int)item.Quantity,
                    IsHq = item.Flags.HasFlag(InventoryItem.ItemFlags.HighQuality),
                    Name = itemSheet.GetRowOrDefault(item.ItemId)?.Name.ToString() ?? "Unknown"
                });
            }
        }

        var config = Plugin.Instance.Configuration;
        bool updated = false;

        if (!config.RetainerInventories.TryGetValue(retainerId, out var existing) ||
            existing.Count != itemsFound.Count ||
            !existing.Select(x => x.ItemId).SequenceEqual(itemsFound.Select(x => x.ItemId)))
        {
            config.RetainerInventories[retainerId] = itemsFound;
            updated = true;
        }

        if (config.RetainerNames.TryAdd(retainerId, name) || config.RetainerNames[retainerId] != name)
        {
            config.RetainerNames[retainerId] = name;
            updated = true;
        }

        if (updated)
        {
            config.Save();
            Plugin.Instance.PriceFetcher.FetchPrices(itemsFound.Select(x => x.ItemId));
        }
    }
}