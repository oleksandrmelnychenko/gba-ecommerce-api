using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Entities.Clients;

public sealed class ClientShoppingCart : EntityBase {
    public ClientShoppingCart() {
        Orders = new HashSet<Order>();

        OrderItems = new HashSet<OrderItem>();
    }

    public string Number { get; set; }

    public string Comment { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalLocalAmount { get; set; }

    public DateTime ValidUntil { get; set; }

    public bool IsOfferProcessed { get; set; }

    public bool IsOffer { get; set; }

    public bool IsVatCart { get; set; }

    public OfferProcessingStatus OfferProcessingStatus { get; set; }

    public long? OfferProcessingStatusChangedById { get; set; }

    public long? CreatedById { get; set; }

    public long ClientAgreementId { get; set; }

    public long? WorkplaceId { get; set; }

    public User OfferProcessingStatusChangedBy { get; set; }

    public User CreatedBy { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public Workplace Workplace { get; set; }

    public ICollection<Order> Orders { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; }
}