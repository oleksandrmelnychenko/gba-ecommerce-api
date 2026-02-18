using System;
using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface IProductPlacementStorageManager {
    (string xlsxFile, string pdfFile) ExportProductPlacementStorageToXlsx(string path, IEnumerable<ProductPlacementStorage> productPlacementStorages,
        IEnumerable<DocumentMonth> months);

    (string xlsxFile, string pdfFile) ExportStockStateStorageToXlsx(string path, IEnumerable<StockStateStorage> productPlacementStorages, IEnumerable<DocumentMonth> months);

    (string xlsxFile, string pdfFile) ExportVerificationStockStatesStorageToXlsx(string path, DateTime from, DateTime to, IEnumerable<StockStateStorage> stockStorageList,
        IEnumerable<DocumentMonth> months);

    (string xlsxFile, string pdfFile) ExportVerificationStockStatesStorageToXlsxTest(string path, DateTime from, DateTime to,
        IEnumerable<ProductPlacementDataHistory> productPlacementDataHistoryList, IEnumerable<DocumentMonth> months);
}