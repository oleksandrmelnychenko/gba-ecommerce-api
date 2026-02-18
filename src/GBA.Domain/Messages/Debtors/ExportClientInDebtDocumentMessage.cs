using System;
using GBA.Domain.EntityHelpers.DebtorModels;

namespace GBA.Domain.Messages.Debtors;

public sealed class ExportClientInDebtDocumentMessage {
    public ExportClientInDebtDocumentMessage(
        string pathToFolder,
        Guid? userNetId,
        Guid? organizationNetId,
        TypeOfClientAgreement typeAgreement,
        TypeOfCurrencyOfAgreement typeCurrency) {
        PathToFolder = pathToFolder;
        UserNetId = userNetId;
        OrganizationNetId = organizationNetId;
        TypeAgreement = typeAgreement;
        TypeCurrency = typeCurrency;
    }

    public string PathToFolder { get; }

    public Guid? UserNetId { get; }

    public Guid? OrganizationNetId { get; }

    public TypeOfClientAgreement TypeAgreement { get; }

    public TypeOfCurrencyOfAgreement TypeCurrency { get; }
}