using System;
using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels.SaleReturns;

public sealed class SaleReturnDto {
    public Guid NetUid { get; set; }

    public DateTime DocumentDate { get; set; }

    public decimal Amount { get; set; }
    public decimal VatAmount { get; set; }

    public string Pricing { get; set; }

    public string DocumentNumber { get; set; }

    public bool IsCanceled { get; set; }

    // public decimal TotalAmountLocal { get; set; }

    public ExtendedClientDto Client { get; set; }

    public ExtendedAgreementDto Agreement { get; set; }

    // public User CreatedBy { get; set; }
    //
    // public User UpdatedBy { get; set; }
    //
    // public User CanceledBy { get; set; }

    public List<SaleReturnItemDto> OrderItems { get; set; }

    public bool IncludesVat { get; set; }

    public decimal ExchangeRate { get; set; }

    public double VatRate { get; set; }
    public string OrganizationName { get; set; }
    public string OrganizationUSREOU { get; set; }

    // public SaleDto Sale { get; set; }

    public string StorageName { get; set; }

    public string Comment { get; set; }

    public CurrencyDto AgreementCurrency { get; set; }
}