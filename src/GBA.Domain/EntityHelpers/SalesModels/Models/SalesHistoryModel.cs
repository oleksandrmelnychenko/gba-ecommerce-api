using System;

namespace GBA.Domain.EntityHelpers.SalesModels.Models;

public sealed class SalesHistoryModel {
    public DateTime Created { get; set; }
    public double TotalRowsQty { get; set; }
    public string Number { get; set; }
    public string FullName { get; set; }
    public string OriginalRegionCode { get; set; }
    public bool IsDevelopment { get; set; }
}