using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.SaleReturns;

namespace GBA.Domain.Repositories.SaleReturns.Contracts;

public interface ISaleReturnRepository {
    long Add(SaleReturn saleReturn);

    void Update(SaleReturn saleReturn);

    void SetCanceled(SaleReturn saleReturn);

    SaleReturn GetById(long id);

    SaleReturn GetByNetId(Guid netId);

    SaleReturn GetByNetIdForPrinting(Guid netId);

    SaleReturn GetLastReturnByCulture();

    SaleReturn GetLastReturnByPrefix(string prefix);

    List<SaleReturn> GetAll();

    List<SaleReturn> GetAllFiltered(DateTime from, DateTime to, long limit, long offset, string value);

    List<SaleReturn> GetAllFiltered(DateTime from, DateTime to);

    List<Client> GetFilteredDetailReportByClient(DateTime from, DateTime to, bool forMyClient, long userId, Guid? clientNetId, List<SaleReturnItemStatusName> reasons);

    List<SaleReturn> GetFilteredGroupedByReasonReport(DateTime from, DateTime to, bool forMyClient, long userId, Guid? clientNetId);
}