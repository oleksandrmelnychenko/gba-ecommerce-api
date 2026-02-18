using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Entities.Sales;

public sealed class PreOrder : EntityBase {
    public string Comment { get; set; }

    public string MobileNumber { get; set; }

    public string Culture { get; set; }

    public double Qty { get; set; }

    public long ProductId { get; set; }

    public long? ClientId { get; set; }

    public Product Product { get; set; }

    public Client Client { get; set; }
}