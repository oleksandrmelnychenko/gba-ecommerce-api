using System.Collections.Generic;
using GBA.Common.Helpers.PrintingDocuments;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface IPrintDocumentsManager {
    (string, string) GetPrintDocument(
        string path,
        List<ColumnsDataForPrinting> columns,
        List<Dictionary<string, string>> rows);
}