using System;
using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels.Sales;

public class SaleDto {
    public Guid NetUid { get; set; }
    public DateTime DocumentDate { get; set; }
    public string DocumentNumber { get; set; }
    public decimal Amount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public string Pricing { get; set; }
    public bool IncludesVat { get; set; }
    public string OrganizationName { get; set; }
    public string OrganizationUSREOU { get; set; }
    public double VatRate { get; set; }
    public ExtendedClientDto Client { get; set; }
    public ExtendedAgreementDto Agreement { get; set; }
    public CurrencyDto AgreementCurrency { get; set; }

    public string Comment { get; set; }

    public DateTime PaymentDate { get; set; }
    public DateTime ShipmentDate { get; set; }

    public string Transporter { get; set; }

    public string Responsible { get; set; }
    public List<OrderItemDto> OrderItems { get; set; }
}

public sealed class InvoiceDto : SaleDto {
    public DateTime OrderDate { get; set; }
}