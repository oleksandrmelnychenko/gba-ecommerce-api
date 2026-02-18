using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyOrderDeliveryDocumentRepository {
    List<SupplyOrderDeliveryDocument> GetAllFromSearch(string documentType, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);

    void Add(IEnumerable<SupplyOrderDeliveryDocument> supplyOrderDeliveryDocuments);

    void Update(IEnumerable<SupplyOrderDeliveryDocument> supplyOrderDeliveryDocuments);

    void UpdateDocumentData(SupplyOrderDeliveryDocument supplyOrderDeliveryDocument);

    void RemoveAllBySupplyOrderId(long id);

    void RemoveAllBySupplyOrderIdExceptProvided(long id, IEnumerable<long> notRemoveIds);
}