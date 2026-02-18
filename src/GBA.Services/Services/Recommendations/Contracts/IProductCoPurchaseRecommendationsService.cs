using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.EntityHelpers;

namespace GBA.Services.Services.Recommendations.Contracts;

public interface IProductCoPurchaseRecommendationsService {
    Task<List<FromSearchProduct>> GetCoPurchaseProductsByProductClientNetIds(Guid productNetId, Guid clientNetId);
}