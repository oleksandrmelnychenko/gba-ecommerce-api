using System;
using GBA.Domain.Entities.Transporters;

namespace GBA.Domain.Entities.Sales.Shipments;

public sealed class WarehousesShipment : EntityBase {
    public bool IsDevelopment { get; set; }
    public long? SaleId { get; set; }
    public Sale Sale { get; set; }
    public long? TransporterId { get; set; }
    public long? UserId { get; set; }
    public bool IsCashOnDelivery { get; set; }
    public decimal CashOnDeliveryAmount { get; set; }
    public bool HasDocument { get; set; }
    public DateTime? ShipmentDate { get; set; }
    public string TTN { get; set; }
    public string Comment { get; set; }
    public string Number { get; set; }
    public string MobilePhone { get; set; }
    public string FullName { get; set; }
    public Transporter Transporter { get; set; }
    public string City { get; set; }
    public string Department { get; set; }
    public User User { get; set; }
    public string TtnPDFPath { get; set; }
    public bool ApproveUpdate { get; set; }
}