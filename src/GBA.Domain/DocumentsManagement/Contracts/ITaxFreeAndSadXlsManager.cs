using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface ITaxFreeAndSadXlsManager {
    (string xlsxFile, string pdfFile) ExportTaxFreeToXlsx(string path, TaxFree taxFree, bool isFromSale = false);

    (string xlsxFile, string pdfFile) ExportTaxFreesToXlsx(string path, List<TaxFree> taxFrees);

    (string xlsxFile, string pdfFile) ExportSadInvoiceToXlsx(string path, Sad sadPl, Sad sadUk, string userFullName, bool isFromSale = false);

    (string xlsxFile, string pdfFile) ExportOldSadInvoiceToXlsx(string path, Sad sadPl, Sad sadUk, string userFullName, bool isFromSale = false);
}