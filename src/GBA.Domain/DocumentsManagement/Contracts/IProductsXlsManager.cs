using System.Collections.Generic;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Consignments;
using GBA.Domain.EntityHelpers.Supplies;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface IProductsXlsManager {
    string ExportMissingVendorCodes(string path, List<string> missingVendorCodes);

    (string xlsxFile, string pdfFile) ExportSadProductSpecification(
        string path,
        Sad sadPl,
        Sad sadUk,
        List<GroupedProductSpecification> plSpecifications,
        List<GroupedProductSpecification> ukSpecifications,
        bool isFromSale = false
    );

    (string xlsxFile, string pdfFile) ExportOldSadProductSpecification(
        string path,
        Sad sadPl,
        Sad sadUk,
        List<GroupedProductSpecification> plSpecifications,
        List<GroupedProductSpecification> ukSpecifications,
        bool isFromSale = false
    );

    (string xlsxFile, string pdfFile) ExportProductCapitalizationToXlsx(string path, ProductCapitalization productCapitalization);

    (string xlsxFile, string pdfFile) ExportProductTransferToXlsx(string path, ProductTransfer productTransfer);

    (string xlsxFile, string pdfFile) ExportProductIncomeDocumentToXlsx(string path, ProductIncome productIncome);

    (string xlsxFile, string pdfFile) ExportAllProductsByStorageToXlsx(string path, List<ProductAvailability> productAvailabilities);

    (string, string) ExportAllConsignmentAvailabilityFilteredToXlsx(
        string path,
        IEnumerable<ConsignmentAvailabilityItem> availabilities);
}