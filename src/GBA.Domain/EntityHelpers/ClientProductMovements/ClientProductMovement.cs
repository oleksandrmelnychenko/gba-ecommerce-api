using System;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.EntityHelpers.ClientProductMovements;

public sealed class ClientProductMovement {
    public ClientProductMovementType MovementType { get; set; }

    public DateTime FromDate { get; set; }

    public Product Product { get; set; }

    public OrderItem OrderItem { get; set; }

    public SaleReturnItem SaleReturnItem { get; set; }
}