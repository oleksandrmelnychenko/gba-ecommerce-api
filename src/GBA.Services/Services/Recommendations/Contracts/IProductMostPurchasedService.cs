using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.EntityHelpers;

namespace GBA.Services.Services.Recommendations.Contracts;

public interface IProductMostPurchasedService {
    Task<List<FromSearchProduct>> GetMostPurchasedProductsByClientNetId(Guid clientNetId);
}