using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales.Shipments;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IShipmentListRepository {
    long Add(ShipmentList shipmentList);

    void Update(ShipmentList shipmentList);

    ShipmentList GetLastRecord();

    ShipmentList GetById(long id);

    ShipmentList GetByNetId(Guid netId);
    ShipmentList GetByFillteredNetId(DateTime from, DateTime to, Guid netId);
    ShipmentList GetByTransporterFilteredNetId(DateTime from, DateTime to, Guid netId);

    ShipmentList GetByTransporterNetId(Guid netId);

    IEnumerable<ShipmentList> GetAllFiltered(DateTime from, DateTime to, Guid netId, long limit, long offset);

    IEnumerable<ShipmentList> GetDocumentFiltered();
}