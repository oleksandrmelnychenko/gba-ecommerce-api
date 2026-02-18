using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Common.Helpers.DepreciatedOrders;
using GBA.Common.Helpers.ProductCapitalizations;
using GBA.Common.Helpers.Products;
using GBA.Common.Helpers.SupplyOrders;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.DepreciatedOrderModels;
using GBA.Domain.EntityHelpers.ProductModels;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface IParseConfigurationXlsManager {
    List<ParsedProduct> GetProductsFromSupplyDocumentsByConfiguration(string pathToFile, DocumentParseConfiguration configuration);

    List<ParsedProduct> GetProductsFromCartItemsDocumentByConfiguration(string pathToFile, CartItemsParseConfiguration configuration);

    List<ParsedProductForUkraine> GetProductsFromUkraineSupplyDocumentsByConfiguration(string pathToFile, UkraineOrderParseConfiguration configuration);

    List<ParsedProductForUkraine> GetProductsForUkraineOrderFromSupplierByConfiguration(string pathToFile, UkraineOrderFromSupplierParseConfiguration configuration);

    List<ProductForUpload> GetProductsForUploadByConfiguration(string pathToFile, ProductUploadParseConfiguration configuration);

    List<AnalogueForUpload> GetAnaloguesForUploadByConfiguration(string pathToFile, AnaloguesUploadParseConfiguration configuration);

    List<ComponentForUpload> GetComponentsForUploadByConfiguration(string pathToFile, ComponentsUploadParseConfiguration configuration);

    List<OriginalNumberForUpload> GetOriginalNumbersForUploadByConfiguration(string pathToFile, OriginalNumbersUploadParseConfiguration configuration);

    List<ParsedProduct> GetProductsFromUploadForCapitalizationByConfiguration(string pathToFile, ProductCapitalizationParseConfiguration configuration);

    List<ParsedProductSpecification> GetProductSpecificationsFromUploadByConfiguration(string pathToFile, ProductSpecificationParseConfiguration configuration);

    List<ProductSpecificationWithVendorCode> GetProductSpecificationWithVendorCodesFromXlsx(string pathToFile);

    List<ProductPlacementMovementVendorCode> GetProductPlacementMovementFromXlsx(string pathToFile, PlacementMovementsStorageParseConfiguration configuration);

    List<PackingListItemWithVendorCode> GetPackingListItemsWithVendorCodesFromXlsx(string pathToFile);

    List<ProductMovementItemFromFile> GetDepreciatedItemsFromXlsx(string pathToFile, DepreciatedAndTransferParseConfiguration parseConfig);
}