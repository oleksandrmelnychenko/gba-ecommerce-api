using System;
using System.Collections.Generic;
using System.Linq;

namespace GBA.Domain.EntityHelpers.SalesModels.Models;

public sealed class ProductsSalesByManagersModel {
    public ProductsSalesByManagersModel() {
        ManagersSoldProduct = new Dictionary<Guid, decimal>();
    }

    public Guid ProductNetId { get; set; }

    public string VendorCode { get; set; }

    public Dictionary<Guid, decimal> ManagersSoldProduct { get; }

    public decimal TotalValueSoldProduct => ManagersSoldProduct.Sum(x => x.Value);
}