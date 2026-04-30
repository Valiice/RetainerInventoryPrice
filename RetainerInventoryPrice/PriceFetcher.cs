using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using Newtonsoft.Json.Linq;

namespace RetainerInventoryPrice;

public class PriceFetcher
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);

    private readonly HttpClient _http = new(new SocketsHttpHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(30) };
    private bool _isFetching = false;
    private DateTime _nextFetchAllowed = DateTime.MinValue;

    public void FetchPrices(IEnumerable<uint> itemIds)
    {
        if (_isFetching || DateTime.UtcNow < _nextFetchAllowed) return;

        List<uint> toFetch;
        lock (Plugin.Instance.Configuration.Lock)
        {
            toFetch = [.. itemIds.Where(NeedsRefetch).Distinct()];
        }

        if (toFetch.Count > 0)
            _ = FetchAsync(toFetch);
    }

    private static bool NeedsRefetch(uint id)
    {
        var config = Plugin.Instance.Configuration;
        if (!config.PriceCache.ContainsKey(id) || !config.DcPriceCache.ContainsKey(id))
            return true;
        if (!config.PriceCacheTimestamps.TryGetValue(id, out var ts))
            return true;
        return DateTime.UtcNow - ts > CacheExpiry;
    }

    private async Task FetchAsync(List<uint> itemIds)
    {
        _isFetching = true;
        try
        {
            var worldRowId = Svc.PlayerState?.CurrentWorld.RowId ?? 74;
            var worldRow = Svc.Data.GetExcelSheet<World>()?.GetRowOrDefault(worldRowId);
            var worldIdentifier = worldRow?.Name.ToString() is { Length: > 0 } wn ? wn : worldRowId.ToString();
            var dcIdentifier = worldRow?.DataCenter.Value.Name.ToString() is { Length: > 0 } dc ? dc : worldIdentifier;

            foreach (var chunk in itemIds.Chunk(50))
            {
                var ids = string.Join(",", chunk);
                await Task.WhenAll(
                    FetchChunkIntoCache($"https://universalis.app/api/v2/{worldIdentifier}/{ids}", Plugin.Instance.Configuration.PriceCache),
                    FetchChunkIntoCache($"https://universalis.app/api/v2/{dcIdentifier}/{ids}", Plugin.Instance.Configuration.DcPriceCache)
                );

                var now = DateTime.UtcNow;
                lock (Plugin.Instance.Configuration.Lock)
                {
                    foreach (var id in chunk)
                        Plugin.Instance.Configuration.PriceCacheTimestamps[id] = now;
                }

                await Task.Delay(100);
            }

            Plugin.Instance.Configuration.Save();
            Svc.Log.Debug($"Fetch complete: {Plugin.Instance.Configuration.PriceCache.Count} world, {Plugin.Instance.Configuration.DcPriceCache.Count} DC items cached.");
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"Universalis fetch failed: {ex.Message}");
            _nextFetchAllowed = DateTime.UtcNow.AddMinutes(5);
        }
        finally
        {
            _isFetching = false;
        }
    }

    private async Task FetchChunkIntoCache(string url, Dictionary<uint, long> cache)
    {
        Svc.Log.Debug($"Fetching: {url}");
        string response;
        try
        {
            response = await _http.GetStringAsync(url);
        }
        catch (TaskCanceledException)
        {
            Svc.Log.Warning($"Timeout on first attempt, retrying: {url}");
            response = await _http.GetStringAsync(url);
        }

        var json = JObject.Parse(response);

        lock (Plugin.Instance.Configuration.Lock)
        {
            if (json["items"] is JObject items)
            {
                foreach (var prop in items.Properties())
                {
                    if (uint.TryParse(prop.Name, out var id))
                        cache[id] = prop.Value["minPrice"]?.Value<long>() ?? 0;
                }

                if (json["unresolvedItems"] is JArray unresolved)
                {
                    foreach (var token in unresolved)
                    {
                        if (token.Type == JTokenType.Integer)
                            cache[(uint)token.Value<long>()] = 0;
                    }
                }
            }
            else if (json["itemID"]?.Value<uint>() is uint singleId)
            {
                cache[singleId] = json["minPrice"]?.Value<long>() ?? 0;
            }
        }
    }
}
