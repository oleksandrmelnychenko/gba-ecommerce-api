using System.Collections.Generic;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface IInvoiceXlsManager {
    (string xlsxFile, string pdfFile) ExportSupplyInvoicePzDocument(string path, SupplyInvoice invoice);

    (string xlsxFile, string pdfFile) ExportUkInvoiceProductSpecification(string path, SupplyInvoice invoice);

    (string xlsxFile, string pdfFile) ExportSpecificationToXlsx(
        string path,
        List<PackingListForSpecification> specifications,
        List<GroupedSpecificationByPackingList> grouped);
}