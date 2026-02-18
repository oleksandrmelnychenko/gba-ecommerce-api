using System.Collections.Generic;
using System.Linq;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.EntityHelpers;

public sealed class AddSupplyOrderUkraineFromFileResponse {
    public AddSupplyOrderUkraineFromFileResponse() {
        MissingVendorCodes = new List<string>();
    }

    public bool HasError => MissingVendorCodes.Any();

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }

    public List<string> MissingVendorCodes { get; set; }

    public string MissingVendorCodesFileUrl { get; set; }
}