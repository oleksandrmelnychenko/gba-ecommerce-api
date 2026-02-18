using System;
using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels.ProductIncomeModels;

public class ProductIncomeDto {
    public Guid NetUid { get; set; }
    public DateTime DocumentDate { get; set; }
    public string DocumentNumber { get; set; }

    public string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public decimal Amount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public string Pricing { get; set; }
    public string OrganizationName { get; set; }
    public string OrganizationUSREOU { get; set; }

    public double VatRate { get; set; }

    public bool IncludesVat { get; set; }
    public string StorageName { get; set; }
    public ExtendedClientDto Client { get; set; }
    public ExtendedAgreementDto Agreement { get; set; }
    public CurrencyDto AgreementCurrency { get; set; }

    public double TotalNetWeight { get; set; }
    public double TotalGrossWeight { get; set; }

    public string Comment { get; set; }

    public string Responsible { get; set; }
    public List<ProductIncomeItemDto> OrderItems { get; set; }
}