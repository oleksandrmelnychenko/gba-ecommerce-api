using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;

namespace GBA.Domain.Entities.Products.Transfers;

public sealed class ProductTransfer : EntityBase {
    public ProductTransfer() {
        Consignments = new HashSet<Consignment>();

        ProductTransferItems = new HashSet<ProductTransferItem>();
    }

    public string Number { get; set; }

    public string Comment { get; set; }

    public DateTime FromDate { get; set; }

    public long ResponsibleId { get; set; }

    public long FromStorageId { get; set; }

    public long ToStorageId { get; set; }

    public long OrganizationId { get; set; }

    public bool IsManagement { get; set; }

    public User Responsible { get; set; }

    public Storage FromStorage { get; set; }

    public Storage ToStorage { get; set; }

    public Organization Organization { get; set; }

    public ICollection<Consignment> Consignments { get; set; }

    public ICollection<ProductTransferItem> ProductTransferItems { get; set; }

    //Ignored

    public Currency Currency { get; set; }

    public decimal ExchangeRate { get; set; }
}