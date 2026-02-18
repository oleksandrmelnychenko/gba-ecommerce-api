using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IGbaDataExportRepository {
    List<PackingList> GetPackingListForSpecification(DateTime from, DateTime to);

    SupplyInvoice GetSupplyInvoiceByPackingListNetId(Guid netId);
}