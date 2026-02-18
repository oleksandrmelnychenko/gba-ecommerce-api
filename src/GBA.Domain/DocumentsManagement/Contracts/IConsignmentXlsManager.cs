using System.Collections.Generic;
using GBA.Domain.EntityHelpers.Consignments;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface IConsignmentXlsManager {
    (string xlsxFile, string pdfFile) ExportGetRemainingProductsByStorageDocumentToXlsx(
        string pathToFolder,
        List<RemainingConsignment> remainingConsignments,
        decimal totalEuro,
        decimal accountingTotalEuro,
        decimal totalLocal,
        decimal accountingTotalLocal,
        decimal totalEuroFiltered,
        decimal accountingTotalEuroFiltered,
        decimal totalLocalFiltered,
        decimal accountingTotalLocalFiltered,
        double totalQty,
        double totalQtyFiltered);

    (string xlsxFile, string pdfFile) ExportGetGroupedConsignmentByStorageDocumentToXlsx(
        string pathToFolder,
        List<GroupedConsignment> groupedConsignments,
        decimal totalEuro,
        decimal accountingTotalEuro,
        decimal totalLocal,
        decimal accountingTotalLocal,
        decimal totalEuroFiltered,
        decimal accountingTotalEuroFiltered,
        decimal totalLocalFiltered,
        decimal accountingTotalLocalFiltered,
        double totalQty,
        double totalQtyFiltered);

    (string xlsxFile, string pdfFile) ExportClientMovementInfoFilteredDocumentToXlsx(
        string pathToFolder,
        IEnumerable<ClientMovementConsignmentInfo> clientMovementConsignmentInfos);

    (string xlsxFile, string pdfFile) ExportIncomeMovementConsignmentDocumentToXlsx(
        string path,
        IEnumerable<IncomeConsignmentInfo> incomeConsignmentInfos);

    (string xlsxFile, string pdfFile) ExportOutcomeMovementConsignmentDocumentToXlsx(
        string path,
        IEnumerable<OutcomeConsignmentInfo> outcomeConsignmentInfos);

    (string xlsxFile, string pdfFile) ExportMovementInfoDocumentToXlsx(
        string path,
        IEnumerable<MovementConsignmentInfo> movementConsignmentInfos);
}