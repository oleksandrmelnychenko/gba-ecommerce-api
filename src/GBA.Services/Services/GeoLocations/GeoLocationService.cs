using System.Net.Http;
using System.Threading.Tasks;
using GBA.Services.Services.GeoLocations.Contracts;
using Microsoft.Extensions.Http;

namespace GBA.Services.Services.GeoLocations;

public sealed class GeoLocationService : IGeoLocationService {
    private readonly IHttpClientFactory _httpClientFactory;

    public GeoLocationService(IHttpClientFactory httpClientFactory) {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetGeoLocationDataByIpAddress(string ipAddress) {
        using HttpClient client = _httpClientFactory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync($"https://tools.keycdn.com/geo.json?host={ipAddress}");
        return await response.Content.ReadAsStringAsync();
    }
}
