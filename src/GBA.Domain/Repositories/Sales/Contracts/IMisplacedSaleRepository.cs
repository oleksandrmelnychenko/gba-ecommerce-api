using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IMisplacedSaleRepository {
    long Add(MisplacedSale misplacedSale);

    List<MisplacedSale> GetByRetailClientNetId(Guid netId);

    List<MisplacedSale> GetByRetailClientId(long id);

    List<MisplacedSale> GetAll();

    List<MisplacedSale> GetAllFiltered(
        string number,
        DateTime from,
        DateTime to,
        bool isAccepted,
        Guid netId);

    MisplacedSale GetByRetailClientAndSaleIds(long retailClientId, long saleId);

    MisplacedSale GetById(long id);

    MisplacedSale GetBySaleNetId(Guid netId);

    void Update(MisplacedSale misplacedSale);

    void Remove(Guid netId);
}