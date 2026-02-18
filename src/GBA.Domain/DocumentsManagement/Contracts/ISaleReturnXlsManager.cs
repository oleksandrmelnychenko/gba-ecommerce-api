using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Supplies.Returns;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface ISaleReturnXlsManager {
    (string xlsxFile, string pdfFile) ExportPlInvoicePzToXlsx(string path, SaleReturn saleReturn);

    (string xlsxFile, string pdfFile) ExportPlSaleReturnToXlsx(string path, SaleReturn saleReturn);

    (string xlsFile, string pdfFile) ExportUkSaleReturnToXlsx(string path, SaleReturn saleReturn, IEnumerable<DocumentMonth> months);

    (string xlsFile, string pdfFile) ExportUkSaleReturnToXlsxFromVatSales(string path, SaleReturn saleReturn, IEnumerable<DocumentMonth> months);

    (string xlsxFile, string pdfFile) ExportSupplyReturnToXlsx(string path, SupplyReturn supplyReturn);

    (string xlsxFile, string pdfFile) ExportSaleReturnDetailReportToXlsx(string path, List<Client> clients);

    (string xlsxFile, string pdfFile) ExportSaleReturnGroupedByReasonReportToXlsx(
        string path,
        List<SaleReturn> saleReturns,
        List<SaleReturnItemStatusName> reasons,
        Dictionary<SaleReturnItemStatus, double> totalQuantitySaleReturnByReasons);
}