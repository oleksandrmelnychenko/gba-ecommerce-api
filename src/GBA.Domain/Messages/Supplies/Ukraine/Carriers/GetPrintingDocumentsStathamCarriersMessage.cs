using System.Collections.Generic;
using GBA.Common.Helpers.PrintingDocuments;

namespace GBA.Domain.Messages.Supplies.Ukraine.Carriers;

public sealed class GetPrintingDocumentsStathamCarriersMessage {
    public GetPrintingDocumentsStathamCarriersMessage(
        string pathToFolder,
        List<ColumnsDataForPrinting> dataForPrintingDocument) {
        PathToFolder = pathToFolder;
        DataForPrintingDocument = dataForPrintingDocument;
    }

    public string PathToFolder { get; }
    public List<ColumnsDataForPrinting> DataForPrintingDocument { get; }
}