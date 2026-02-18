using System;

namespace GBA.Domain.Entities.Sales;

public class SetFastClientRequest {
    public Guid SaleNetId { get; set; }
    public string Url { get; set; }
}