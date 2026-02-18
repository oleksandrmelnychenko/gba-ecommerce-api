using System;
using GBA.Domain.EntityHelpers.DebtorModels;

namespace GBA.Domain.Messages.Debtors;

public sealed class GetAllFilteredDebtorsByClientMessage {
    public GetAllFilteredDebtorsByClientMessage(
        Guid? userNetId,
        Guid? organizationNetId,
        TypeOfClientAgreement typeAgreement,
        TypeOfCurrencyOfAgreement typeCurrency,
        int days,
        long limit,
        long offset) {
        UserNetId = userNetId;
        OrganizationNetId = organizationNetId;
        TypeAgreement = typeAgreement;
        TypeCurrency = typeCurrency;
        Days = days;
        Limit = limit;
        Offset = offset;
    }

    public Guid? UserNetId { get; }

    public Guid? OrganizationNetId { get; }

    public TypeOfClientAgreement TypeAgreement { get; }

    public TypeOfCurrencyOfAgreement TypeCurrency { get; }

    public long Limit { get; }

    public long Offset { get; }

    public int Days { get; }
}