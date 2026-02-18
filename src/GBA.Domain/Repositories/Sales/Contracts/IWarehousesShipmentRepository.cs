using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales.Shipments;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IWarehousesShipmentRepository {
    List<WarehousesShipment> GetAll(long saleId);
    WarehousesShipment Get(long saleId);

    void UpdateApprove(Guid netId);
    long Add(WarehousesShipment warehousesShipment);
    void Update(WarehousesShipment updateDataCarrier);
    void Update(UpdateDataCarrier updateDataCarrier);
}