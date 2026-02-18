using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products.Transfers;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductTransferRepository {
    long Add(ProductTransfer productTransfer);

    void Update(ProductTransfer productTransfer);

    ProductTransfer GetLastRecord(string culture);

    ProductTransfer GetLastRecord(long organizationId);

    ProductTransfer GetById(long id);

    ProductTransfer GetByIdForConsignmentCreation(long id);

    ProductTransfer GetByNetId(Guid netId);

    List<ProductTransfer> GetAll();

    List<ProductTransfer> GetAllFiltered(DateTime from, DateTime to, long limit, long offset);

    List<ProductTransfer> GetAllFiltered(DateTime from, DateTime to);
}