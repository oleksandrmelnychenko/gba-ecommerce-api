using System;
using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels.ProductTransfers;

public sealed class ProductTransferDto {
    public Guid NetUid { get; set; }

    public string DocumentNumber { get; set; }

    public string Comment { get; set; }

    public DateTime DocumentDate { get; set; }

    public string Responsible { get; set; }

    public string FromStorage { get; set; }

    public string ToStorage { get; set; }

    public string OrganizationName { get; set; }

    public string OrganizationUSREOU { get; set; }

    public CurrencyDto Currency { get; set; }

    public decimal ExchangeRate { get; set; }

    public List<ProductTransferItemDto> OrderItems { get; set; }
}