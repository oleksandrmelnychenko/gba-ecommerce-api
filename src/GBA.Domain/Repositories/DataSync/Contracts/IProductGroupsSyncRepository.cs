using System.Collections.Generic;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers.DataSync;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IProductGroupsSyncRepository {
    IEnumerable<SyncProductGroup> GetAllSyncProductGroup();

    IEnumerable<SyncProductGroup> GetAmgAllSyncProductGroup();

    IEnumerable<ProductGroup> GetAllProductGroups();

    ProductSubGroup GetProductSubGroupByIdsIfExists(long rootProductGroupId, long subProductGroupId);

    ProductGroup GetProductGroupBySourceId(byte[] sourceId);

    long Add(ProductGroup productGroup);

    void Add(ProductSubGroup productSubGroup);

    void Update(ProductGroup productGroup);

    void Update(ProductSubGroup productSubGroup);

    void RemoveAssignmentsForRootProductGroupById(long productGroupId);
}