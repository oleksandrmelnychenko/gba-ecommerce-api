using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.AllegroSellFormEntities;

public sealed class NewAllegroSellingItemRequest {
    public NewAllegroSellingItemRequest() {
        CategoryFields = new List<AllegroSellFromField>();
    }

    public string Title { get; set; }

    public int CategoryId { get; set; }

    public string Description { get; set; }

    public IEnumerable<AllegroSellFromField> CategoryFields { get; set; }

    public bool IsAuction { get; set; }

    public bool IsBuyNow { get; set; }

    public float Price { get; set; }

    public float StartingPrice { get; set; }

    public float MinimalPrice { get; set; }

    public int ItemCount { get; set; }

    public AllegroItemCountType ItemCountType { get; set; }

    public bool UntilLastItem { get; set; }

    public AllegroSellingLifeTime SellingLifeTime { get; set; }

    public bool IsOfferResuming { get; set; }

    public AllegroSellingInvoiceOption InvoiceOption { get; set; }

    public bool IsDeferredSelling { get; set; }

    public DateTime SellingStartDate { get; set; }

    public string[] Images { get; set; }

    public float PackageFirstItemPrice { get; set; }

    public float PackageEachItemPrice { get; set; }

    public int QtyInPackage { get; set; }

    public long ProductId { get; set; }

    public Product Product { get; set; }
}