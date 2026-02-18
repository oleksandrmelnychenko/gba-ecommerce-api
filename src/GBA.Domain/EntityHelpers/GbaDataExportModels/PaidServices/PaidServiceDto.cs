using System;
using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels.PaidServices;

public sealed class PaidServiceDto {
    public Guid NetUid { get; set; }
    public DateTime DocumentDate { get; set; }
    public string DocumentNumber { get; set; }
    public string InvoiceNumber { get; set; }
    public ExtendedClientDto Client { get; set; }
    public ExtendedAgreementDto Agreement { get; set; }
    public decimal Amount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal VatRate { get; set; }

    public List<PaidServiceItemDto> OrderItems { get; set; }
}

// <LabelValueRow labelId="GrossPrice">{format(service.GrossPrice, currencyCode)}</LabelValueRow>
//     <LabelValueRow labelId="NetPrice">{format(service.NetPrice, currencyCode)}</LabelValueRow>
//     <LabelValueRow labelId="VATAccountingPercent">{service.VatPercent}</LabelValueRow>
//     <LabelValueRow labelId="VATAccounting">{format(service.Vat, currencyCode)}</LabelValueRow>
//
//     <LabelValueRow labelId="AccountingGrossPrice">{format(service.AccountingGrossPrice, currencyCode)}</LabelValueRow>
//     <LabelValueRow label={`${t('NetPrice')} (${t('AccountingShort')})`}>
// {format(service.AccountingNetPrice, currencyCode)}
// </LabelValueRow>
//     <LabelValueRow label={`${t('VATAccountingPercent')} (${t('AccountingShort')})`}>{service.AccountingVatPercent}</LabelValueRow>
//     <LabelValueRow label={`${t('VATAccounting')} (${t('AccountingShort')})`}>
// {format(service.AccountingVat, currencyCode)}
// </LabelValueRow>
// {service.IsIncludeAccountingValue ? <Translate id={'IncludedAccountingValue'} /> : null}
// <LabelValueRow labelId="FromDate">
//     <FormattedDate format={DateTimeFormat.DATE} date={service.FromDate} />