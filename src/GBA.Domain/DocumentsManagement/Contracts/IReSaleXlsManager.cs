using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface IReSaleXlsManager {
    (string xlsxFile, string pdfFile) ExportReSaleInvoicePaymentDocumentToXlsx(string pathToFolder, UpdatedReSaleModel reSale);

    (string xlsxFile, string pdfFile) ExportReSaleSalesInvoiceDocumentToXlsx(string path, UpdatedReSaleModel reSale, IEnumerable<DocumentMonth> months);

    (string excelFilePath, string pdfFilePath) ExportReSalePaymentDocumentToXlsx(string pathToFolder, ReSale reSale);
}