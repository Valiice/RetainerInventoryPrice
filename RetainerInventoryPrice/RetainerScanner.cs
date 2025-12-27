using System.Text;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace RetainerInventoryPrice;

public unsafe class RetainerScanner
{
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
        try
        {
            var invManager = InventoryManager.Instance();
            if (invManager == null) return;

            var retainerInv = invManager->GetInventoryContainer(InventoryType.RetainerPage1);
            if (retainerInv == null || !retainerInv->IsLoaded) return;

            var retManager = FFXIVClientStructs.FFXIV.Client.Game.RetainerManager.Instance();
            if (retManager == null) return;

            var activeRetainer = retManager->GetActiveRetainer();
            if (activeRetainer == null) return;

            var retainerId = activeRetainer->RetainerId;
            if (retainerId == 0) return;

            var name = Encoding.UTF8.GetString(activeRetainer->Name).TrimEnd('\0');

            if (DateTime.Now.Second % 3 != 0) return;

            ScanRetainer(retainerId, name);
        }
        catch (Exception)
        {
        }
    }

    private void ScanRetainer(ulong retainerId, string? name)
    {
        var manager = InventoryManager.Instance();
        var itemsFound = new List<SavedItem>();
        var itemSheet = Svc.Data.GetExcelSheet<Item>();

        foreach (var page in _retainerPages)
        {
            var container = manager->GetInventoryContainer(page);
            if (container == null || !container->IsLoaded) continue;

            for (var i = 0; i < container->Size; i++)
            {
                var item = container->Items[i];
                if (item.ItemId == 0) continue;

                var row = itemSheet.GetRowOrDefault(item.ItemId);
                bool isHq = item.Flags.HasFlag(InventoryItem.ItemFlags.HighQuality);

                itemsFound.Add(new SavedItem
                {
                    ItemId = item.ItemId,
                    Quantity = (int)item.Quantity,
                    IsHq = isHq,
                    Name = row?.Name.ToString() ?? "Unknown"
                });
            }
        }

        Plugin.Instance.Configuration.RetainerInventories[retainerId] = itemsFound;

        if (!string.IsNullOrEmpty(name))
        {
            Plugin.Instance.Configuration.RetainerNames[retainerId] = name;
        }

        Plugin.Instance.Configuration.Save();
        Plugin.Instance.PriceFetcher.FetchPrices(itemsFound.Select(x => x.ItemId).Distinct());
    }
}