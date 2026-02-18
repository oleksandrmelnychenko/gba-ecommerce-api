using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Sales.Shipments;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface ISalesShipmentListManager {
    (string xlsxFile, string pdfFile) ExportAllSalesShipmentsToXlsx(string path, IEnumerable<ShipmentList> shipmentList, IEnumerable<DocumentMonth> months);
    (string xlsxFile, string pdfFile) ExportSalesShipmentsToXlsx(string path, ShipmentList shipmentList, IEnumerable<DocumentMonth> months);
}