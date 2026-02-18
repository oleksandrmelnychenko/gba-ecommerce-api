using System;
using System.Collections.Generic;
using GBA.Common.Helpers.PrintingDocuments;

namespace GBA.Domain.Messages.Supplies.DeliveryProductProtocols;

public sealed class GetAllFilteredDeliveryProductProtocolForPrintingMessage {
    public GetAllFilteredDeliveryProductProtocolForPrintingMessage(
        string pathToFolder,
        List<ColumnsDataForPrinting> dataForPrintings,
        DateTime from,
        DateTime to) {
        PathToFolder = pathToFolder;
        DataForPrintings = dataForPrintings;
        From = from.Date;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public string PathToFolder { get; }
    public List<ColumnsDataForPrinting> DataForPrintings { get; }
    public DateTime From { get; }
    public DateTime To { get; }
}