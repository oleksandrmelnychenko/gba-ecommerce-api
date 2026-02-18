using System;
using System.Collections.Generic;
using GBA.Common.Helpers.PrintingDocuments;

namespace GBA.Domain.Messages.Supplies.Ukraine.TaxFreePackLists;

public sealed class TaxFreePackingListPrintDocumentsMessage {
    public TaxFreePackingListPrintDocumentsMessage(
        string pathToFolder,
        List<ColumnsDataForPrinting> columnDataForPrint,
        DateTime from,
        DateTime to) {
        PathToFolder = pathToFolder;
        ColumnDataForPrint = columnDataForPrint;
        From = from.Date;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public string PathToFolder { get; }
    public List<ColumnsDataForPrinting> ColumnDataForPrint { get; }
    public DateTime From { get; }
    public DateTime To { get; }
}