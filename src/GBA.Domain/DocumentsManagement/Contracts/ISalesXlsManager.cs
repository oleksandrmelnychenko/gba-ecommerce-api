using System;
using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface ISalesXlsManager {
    (string xlsxFile, string pdfFile) ExportPlInvoiceToXlsx(string path, Sale sale);

    (string xlsxFile, string pdfFile) ExportPlInvoicePzToXlsx(string path, Sale sale);

    (string xlsxFile, string pdfFile) ExportUkInvoiceToXlsx(string path, Sale sale, IEnumerable<DocumentMonth> months);

    (string xlsxFile, string pdfFile) ExportUkSaleByNetIdWithShiftedItemsToXlsx(string path, Sale sale, IEnumerable<DocumentMonth> months);

    (string xlsxFile, string pdfFile) ExportUkInvoiceFromStorageToXlsx(string path, Sale sale, IEnumerable<DocumentMonth> months);
    (string xlsxFile, string pdfFile) ExportUkInvoiceFromStorageToXlsxFromSale(string path, Sale sale, IEnumerable<DocumentMonth> months);

    (string xlsxFile, string pdfFile) ExportUkInvoiceIsVatSaleToXlsx(string path, Sale sale, IEnumerable<DocumentMonth> months);
    (string xlsxFile, string pdfFile) ExportUkRegisterInvoiceSaleToXlsx(string path, List<Sale> sales, DateTime to, IEnumerable<DocumentMonth> months);

    (string xlsxFile, string pdfFile) ExportInvoiceForPaymentForSale(string path, Sale sale);

    (string xlsxFile, string pdfFile) ExportShipmentListForSale(string path, Sale sale, IEnumerable<DocumentMonth> months);

    (string xlsxFile, string pdfFile) ExportUkDocumentPackageIsVatSaleToXlsx(string path, Sale sale, IEnumerable<DocumentMonth> months);
}