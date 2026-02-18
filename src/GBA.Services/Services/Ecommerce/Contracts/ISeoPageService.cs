using System.Threading.Tasks;
using GBA.Domain.EntityHelpers;

namespace GBA.Services.Services.Ecommerce.Contracts;

public interface ISeoPageService {
    Task FillDbIfNoData();

    Task<SeoPageModel> GetAll();

    Task<FullSeoPageModel> GetAll(string locale);
}