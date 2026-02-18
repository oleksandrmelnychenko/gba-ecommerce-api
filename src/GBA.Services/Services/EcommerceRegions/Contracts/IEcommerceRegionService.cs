using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.Ecommerce;

namespace GBA.Services.Services.EcommerceRegions.Contracts;

public interface IEcommerceRegionService {
    Task<IEnumerable<EcommerceRegion>> GetAllLocale();

    Task<IEnumerable<EcommerceRegion>> Update(EcommerceRegion ecommerceRegion);
}