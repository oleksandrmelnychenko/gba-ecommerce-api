using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.Repositories.ReSales.Contracts;

public interface IReSaleRepository {
    long Add(ReSale reSale);

    ReSale GetById(long id);

    ReSale GetByNetId(Guid netId);

    IEnumerable<ReSale> GetAll(
        DateTime from, DateTime to, int limit, int offset, FilterReSaleStatusOption status);


    UpdatedReSaleModel GetProductLocations(UpdatedReSaleModel reSale);

    void Update(ReSale reSale);

    void Delete(long id);

    UpdatedReSaleModel GetUpdatedByNetId(Guid netId);

    ReSale GetByNetIdWithItemsInfo(Guid netId);

    void UpdateChangeToInvoice(ReSale reSale);

    void Remove(long id);

    ReSale GetForDocumentExportByNetId(Guid netId);

    IEnumerable<ReSale> GetLastPaidReSalesByClientAgreementId(long clientAgreementId, DateTime created);

    ReSale GetByNetIdWithoutInfo(
        Guid netId);

    void ChangeIsCompleted(
        Guid netId,
        bool isCompleted);

    ConsignmentItem GetConsignmentItemByProductId(long productId);

    ConsignmentItem GetConsignmentItemByProductAndStorageId(
        long productId,
        long storageId);
}