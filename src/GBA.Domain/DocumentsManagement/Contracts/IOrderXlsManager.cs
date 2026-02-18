using GBA.Domain.Entities.DepreciatedOrders;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface IOrderXlsManager {
    (string xlsxFile, string pdfFile) ExportDepreciatedOrderDocumentToXlsx(string path, DepreciatedOrder depreciatedOrder);
}