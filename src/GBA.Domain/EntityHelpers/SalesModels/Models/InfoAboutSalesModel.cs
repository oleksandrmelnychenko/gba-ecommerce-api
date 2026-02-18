using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.SalesModels.Models;

public sealed class InfoAboutSalesModel {
    public InfoAboutSalesModel() {
        TotalByColumn = new Dictionary<string, decimal>();
    }

    public List<SaleByManagerAndProductTopModel> SalesByManagerAndProductTop { get; set; }

    public Dictionary<string, decimal> TotalByColumn { get; }
}