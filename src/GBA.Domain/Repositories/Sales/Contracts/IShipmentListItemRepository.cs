using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales.Shipments;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IShipmentListItemRepository {
    void Add(ShipmentListItem item);

    void Add(IEnumerable<ShipmentListItem> items);

    void Update(ShipmentListItem item);
    void UpdateIsChangeTransporter(Guid netId);

    void Update(IEnumerable<ShipmentListItem> items);

    void RemoveAllByListIdExceptProvided(long id, IEnumerable<long> ids);

    void RemoveAllByIdExceptProvided(long id, IEnumerable<long> ids);
}