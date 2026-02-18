using System;
using GBA.Common.Helpers;

namespace GBA.Domain.EntityHelpers.SalesModels.Models;

public sealed class ManagerWithTotalValueSoldModel {
    public Guid NetId { get; set; }

    public string ManagerName { get; set; }

    public decimal TotalManagerSold { get; set; }

    public OrderSource TypeOrder { get; set; }
}