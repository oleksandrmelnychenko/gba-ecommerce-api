using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales.Shipments;

namespace GBA.Domain.Repositories.UpdateDataCarriers.Contracts;

public interface IUpdateDataCarrierRepository {
    List<UpdateDataCarrier> Get(long saleId);
    List<UpdateDataCarrier> GetIsEditTransporter(long saleId);
    UpdateDataCarrier GetByNetId(Guid netId);
    UpdateDataCarrier GetId(long saleId);
    void UpdateIsDevelopment(Guid netId);
    long Add(UpdateDataCarrier updateDataCarrier);
}