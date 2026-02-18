using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Products;

public sealed class ProductSpecification : EntityBase {
    public ProductSpecification() {
        OrderProductSpecifications = new HashSet<OrderProductSpecification>();

        ConsignmentItems = new HashSet<ConsignmentItem>();

        OrderItems = new HashSet<OrderItem>();

        SupplyOrderUkraineItems = new HashSet<SupplyOrderUkraineItem>();
    }

    public string Name { get; set; }

    public string SpecificationCode { get; set; }

    public string Locale { get; set; }

    public decimal DutyPercent { get; set; }

    public bool IsActive { get; set; }

    public long AddedById { get; set; }

    public long ProductId { get; set; }

    public decimal CustomsValue { get; set; }

    public decimal Duty { get; set; }

    public decimal VATValue { get; set; }

    public decimal VATPercent { get; set; }

    public User AddedBy { get; set; }

    public Product Product { get; set; }

    public ICollection<OrderProductSpecification> OrderProductSpecifications { get; set; }

    public ICollection<ConsignmentItem> ConsignmentItems { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; }

    public OrderProductSpecification OrderProductSpecification { get; set; }

    public ICollection<SupplyOrderUkraineItem> SupplyOrderUkraineItems { get; set; }

    // Ignored

    public decimal Price { get; set; }

    public double Qty { get; set; }
}