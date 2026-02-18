using System.Collections.Generic;
using System.Linq;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.SignalRMessages;

namespace GBA.Domain.EntityHelpers;

public sealed class AddSupplyOrderFromFileResponse {
    public AddSupplyOrderFromFileResponse() {
        MissingVendorCodes = new List<string>();
    }

    public bool HasError => MissingVendorCodes.Any();

    public SupplyOrder SupplyOrder { get; set; }

    public InformationMessage InformationMessage { get; set; }

    public List<string> MissingVendorCodes { get; set; }

    public string MissingVendorCodesFileUrl { get; set; }
}