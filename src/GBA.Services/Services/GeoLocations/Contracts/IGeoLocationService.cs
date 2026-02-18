using System.Threading.Tasks;

namespace GBA.Services.Services.GeoLocations.Contracts;

public interface IGeoLocationService {
    Task<string> GetGeoLocationDataByIpAddress(string ipAddress);
}