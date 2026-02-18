using System;
using GBA.Domain.EntityHelpers.ReportTypes;

namespace GBA.Domain.Messages.SaleReturns;

public sealed class ExportSaleReturnDocumentMessage {
    public ExportSaleReturnDocumentMessage(
        string pathToFolder,
        DateTime from,
        DateTime to,
        SaleReturnReportType reportType,
        bool forMyClients,
        Guid? clientNetId,
        Guid userNetId) {
        PathToFolder = pathToFolder;
        From = from;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
        ReportType = reportType;
        ForMyClients = forMyClients;
        ClientNetId = clientNetId;
        UserNetId = userNetId;
    }

    public string PathToFolder { get; }

    public DateTime From { get; }

    public DateTime To { get; }

    public SaleReturnReportType ReportType { get; }

    public bool ForMyClients { get; }

    public Guid? ClientNetId { get; }

    public Guid UserNetId { get; }
}