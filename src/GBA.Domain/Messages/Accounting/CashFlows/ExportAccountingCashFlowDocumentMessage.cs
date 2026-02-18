using System;
using GBA.Domain.EntityHelpers.Supplies;

namespace GBA.Domain.Messages.Accounting.CashFlows;

public sealed class ExportAccountingCashFlowDocumentMessage {
    public ExportAccountingCashFlowDocumentMessage(
        string pathToFolder,
        Guid netId,
        DateTime from,
        DateTime to,
        Guid userNetId,
        TypePaymentTask typePaymentTask) {
        PathToFolder = pathToFolder;
        NetId = netId;
        From = from.Date;
        UserNetId = userNetId;
        TypePaymentTask = typePaymentTask;
        To = to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public string PathToFolder { get; }
    public Guid NetId { get; }
    public DateTime From { get; }
    public Guid UserNetId { get; }
    public TypePaymentTask TypePaymentTask { get; }
    public DateTime To { get; }
}