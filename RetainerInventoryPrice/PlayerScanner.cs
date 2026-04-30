using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace RetainerInventoryPrice;

public unsafe class PlayerScanner
{
    private DateTime _lastScan = DateTime.MinValue;
    private readonly InventoryType[] _bagPages =
    [
        InventoryType.Inventory1, InventoryType.Inventory2,
        InventoryType.Inventory3, InventoryType.Inventory4
    ];

    public PlayerScanner()
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

            var firstBag = invManager->GetInventoryContainer(InventoryType.Inventory1);
            if (firstBag == null || !firstBag->IsLoaded) return;

            _lastScan = DateTime.Now;
            Scan();
        }
        catch
        {
            // Ignore
        }
    } 

    public void ScanNow()
    {
        _lastScan = DateTime.MinValue;
    }

    private void Scan()
    {
        var itemSheet = Svc.Data.GetExcelSheet<Item>();
        var manager = InventoryManager.Instance();
        var config = Plugin.Instance.Configuration;

        // Scan bags (Inventory1–4)
        var bagsFound = new List<SavedItem>();
        foreach (var page in _bagPages)
        {
            var container = manager->GetInventoryContainer(page);
            if (container == null || !container->IsLoaded) continue;

            for (var i = 0; i < container->Size; i++)
            {
                var item = container->Items[i];
                if (item.ItemId == 0) continue;

                bagsFound.Add(new SavedItem
                {
                    ItemId = item.ItemId,
                    Quantity = (int)item.Quantity,
                    IsHq = item.Flags.HasFlag(InventoryItem.ItemFlags.HighQuality),
                    Name = itemSheet.GetRowOrDefault(item.ItemId)?.Name.ToString() ?? "Unknown"
                });
            }
        }

        // Scan crystals
        var crystalsFound = new List<SavedItem>();
        var crystalContainer = manager->GetInventoryContainer(InventoryType.Crystals);
        if (crystalContainer != null && crystalContainer->IsLoaded)
        {
            for (var i = 0; i < crystalContainer->Size; i++)
            {
                var item = crystalContainer->Items[i];
                if (item.ItemId == 0) continue;

                crystalsFound.Add(new SavedItem
                {
                    ItemId = item.ItemId,
                    Quantity = (int)item.Quantity,
                    IsHq = false,
                    Name = itemSheet.GetRowOrDefault(item.ItemId)?.Name.ToString() ?? "Unknown"
                });
            }
        }

        bool updated = false;

        if (config.PlayerBags.Count != bagsFound.Count ||
            !config.PlayerBags.Select(x => x.ItemId).SequenceEqual(bagsFound.Select(x => x.ItemId)))
        {
            config.PlayerBags = bagsFound;
            updated = true;
        }

        if (config.PlayerCrystals.Count != crystalsFound.Count ||
            !config.PlayerCrystals.Select(x => x.ItemId).SequenceEqual(crystalsFound.Select(x => x.ItemId)))
        {
            config.PlayerCrystals = crystalsFound;
            updated = true;
        }

        if (updated)
            config.Save();

        Plugin.Instance.PriceFetcher.FetchPrices(
            bagsFound.Select(x => x.ItemId).Concat(crystalsFound.Select(x => x.ItemId)));
    }

    public void Dispose()
    {
        Svc.Framework.Update -= OnUpdate;
    }
}
