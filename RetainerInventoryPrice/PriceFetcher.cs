using ECommons.DalamudServices;
using Newtonsoft.Json.Linq;


namespace RetainerInventoryPrice;

public class PriceFetcher
{
    private readonly HttpClient _http = new();
    private bool _isFetching = false;

    public void FetchPrices(IEnumerable<uint> itemIds)
    {
        if (_isFetching) return;

        var toFetch = itemIds.Where(id => !Plugin.Instance.Configuration.PriceCache.ContainsKey(id)).ToList();

        if (toFetch.Count == 0) return;

        _ = FetchAsync(toFetch);
    }

    private async Task FetchAsync(List<uint> itemIds)
    {
        _isFetching = true;
        try
        {
            var worldId = Svc.PlayerState?.CurrentWorld.RowId ?? 74;

            var chunks = itemIds.Chunk(50);

            foreach (var chunk in chunks)
            {
                var idString = string.Join(",", chunk);
                var url = $"https://universalis.app/api/v2/{worldId}/{idString}";

                var response = await _http.GetStringAsync(url);
                var json = JObject.Parse(response);

                if (json["items"] is JObject items)
                {
                    foreach (var prop in items.Properties())
                    {
                        if (uint.TryParse(prop.Name, out var id))
                        {
                            var price = prop.Value["minPrice"]?.Value<long>() ?? 0;
                            if (price > 0)
                            {
                                Plugin.Instance.Configuration.PriceCache[id] = price;
                            }
                        }
                    }
                }
                await Task.Delay(100);
            }

            Plugin.Instance.Configuration.Save();
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"Universalis fetch failed: {ex.Message}");
        }
        finally
        {
            _isFetching = false;
        }
    }
}