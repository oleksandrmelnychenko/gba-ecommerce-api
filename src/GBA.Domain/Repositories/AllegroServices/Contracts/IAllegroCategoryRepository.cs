using System.Collections.Generic;
using GBA.Domain.Entities.AllegroServices;

namespace GBA.Domain.Repositories.AllegroServices.Contracts;

public interface IAllegroCategoryRepository {
    List<AllegroCategory> GetAll();

    List<AllegroCategory> GetAllRootCategoriesWithSubCategories();

    List<AllegroCategory> GetAllSubCategoriesByParentCategoryId(long id);

    List<AllegroCategory> GetAllFromSearch(string value, int limit, int offset);

    List<AllegroCategory> GetTreeByCategoryId(int categoryId);

    void Add(IEnumerable<AllegroCategory> categories);

    void Update(IEnumerable<AllegroCategory> categories);

    void RemoveByIds(IEnumerable<long> ids);

    void DeleteRemovedCategories();
}