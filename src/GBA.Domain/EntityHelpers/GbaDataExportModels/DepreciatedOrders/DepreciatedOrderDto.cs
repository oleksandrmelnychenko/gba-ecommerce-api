using System;
using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels.DepreciatedOrders;

public sealed class DepreciatedOrderDto {
    public Guid NetUid { get; set; }

    public string DocumentNumber { get; set; }

    public string Comment { get; set; }

    public DateTime DocumentDate { get; set; }

    public string StorageName { get; set; }

    public string OrganizationName { get; set; }

    public string OrganizationUSREOU { get; set; }

    public string Responsible { get; set; }

    public CurrencyDto Currency { get; set; }

    public decimal Amount { get; set; }

    public decimal ExchangeRate { get; set; }

    public List<DepreciatedOrderItemDto> OrderItems { get; set; }
}