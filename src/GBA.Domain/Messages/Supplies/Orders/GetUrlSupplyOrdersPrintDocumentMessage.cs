using System;
using System.Collections.Generic;
using GBA.Common.Helpers.PrintingDocuments;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetUrlSupplyOrdersPrintDocumentMessage {
    public GetUrlSupplyOrdersPrintDocumentMessage(
        string pathToFolder,
        List<ColumnsDataForPrinting> dataForPrint,
        DateTime from,
        DateTime to) {
        PathToFolder = pathToFolder;
        DataForPrint = dataForPrint;
        From = from.Date;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public string PathToFolder { get; }
    public List<ColumnsDataForPrinting> DataForPrint { get; }
    public DateTime From { get; }
    public DateTime To { get; }
}