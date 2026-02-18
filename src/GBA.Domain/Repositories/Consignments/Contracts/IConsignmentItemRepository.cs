using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Consignments.Contracts;

public interface IConsignmentItemRepository {
    long Add(ConsignmentItem consignmentItem);

    void Add(IEnumerable<ConsignmentItem> consignmentItems);

    void UpdateRemainingQty(ConsignmentItem item);

    ConsignmentItem GetId(long consignmentItemId);

    IEnumerable<ConsignmentItem> GetAllAvailable(long organizationId, long storageId, long productId, ProductWriteOffRuleType ruleType);

    IEnumerable<ConsignmentItem> GetAllAvailable(long organizationId, long productId, ProductWriteOffRuleType ruleType, string storageLocale);

    IEnumerable<ConsignmentItem> GetAllAvailable(long organizationId, long productId, ProductWriteOffRuleType ruleType, bool vatStorage, bool forDefective = false,
        long? storageId = null);

    IEnumerable<ConsignmentItem> GetAvailableItemsCreatedFromSpecificRootItemOnSpecificStorage(long rootItemId, long storageId);

    void UpdateGrossPriceAfterIncomes(ConsignmentItem item);

    IEnumerable<ConsignmentItem> GetAllAvailableWithIncludes(long organizationId, long storageId, long productId, ProductWriteOffRuleType ruleType);

    decimal GetPriceForReSaleByConsignmentItemId(long id);
}