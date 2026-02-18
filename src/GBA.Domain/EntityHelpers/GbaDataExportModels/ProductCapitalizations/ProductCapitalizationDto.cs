using System;
using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels.ProductCapitalizations;

public sealed class ProductCapitalizationDto {
    public long Id { get; set; }

    public Guid NetUid { get; set; }

    public DateTime Created { get; set; }

    public DateTime Updated { get; set; }

    public bool Deleted { get; set; }

    public string DocumentNumber { get; set; }

    public string Comment { get; set; }

    public DateTime DocumentDate { get; set; }

    public long StorageId { get; set; }

    public decimal Amount { get; set; }

    public string OrganizationName { get; set; }

    public string OrganizationUSREOU { get; set; }

    public double VatRate { get; set; }

    public decimal ExchangeRate { get; set; }

    public CurrencyDto Currency { get; set; }

    public string StorageName { get; set; }

    public List<ProductCapitalizationItemDto> OrderItems { get; set; }
}