using System.Collections.Generic;
using GBA.Domain.Entities.Sales.Shipments;

namespace GBA.Domain.EntityHelpers.SalesModels.Models;

public sealed class TransportersEditModel {
    public TransportersEditModel() {
        UpdateDataCarriers = new HashSet<UpdateDataCarrier>();
    }

    public ICollection<UpdateDataCarrier> UpdateDataCarriers { get; set; }
}