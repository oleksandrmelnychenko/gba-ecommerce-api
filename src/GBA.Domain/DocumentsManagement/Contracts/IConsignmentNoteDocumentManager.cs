using GBA.Domain.Entities.ConsignmentNoteSettings;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface IConsignmentNoteDocumentManager {
    (string, string) GetPrintSaleConsignmentNoteDocument(
        string path,
        Sale sale,
        ConsignmentNoteSetting setting);

    (string, string) GetPrintReSaleConsignmentNoteDocument(
        string path,
        UpdatedReSaleModel reSale,
        ConsignmentNoteSetting messageSetting);
}