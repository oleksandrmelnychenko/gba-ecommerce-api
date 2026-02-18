using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.Helpers.DepreciatedOrders;
using GBA.Common.Helpers.ProductCapitalizations;
using GBA.Common.Helpers.Products;
using GBA.Common.Helpers.SupplyOrders;
using GBA.Common.ResourceNames;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.DepreciatedOrderModels;
using GBA.Domain.EntityHelpers.ProductModels;
using OfficeOpenXml;

namespace GBA.Domain.DocumentsManagement;

public sealed class ParseConfigurationXlsManager : IParseConfigurationXlsManager {
    public List<ParsedProduct> GetProductsFromSupplyDocumentsByConfiguration(string pathToFile, DocumentParseConfiguration configuration) {
        List<ParsedProduct> parsedProducts = new();

        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx"))
            if (inputtedFile.Extension.ToLower().Equals(".xls"))
                inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);
        if (!package.Workbook.Worksheets.Any()) throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.NoWorksheets, 0, 0, string.Empty);

        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        try {
            for (int i = configuration.StartRow; i <= configuration.EndRow; i++) {
                ParsedProduct parsedProduct = new() {
                    RowNumber = i
                };

                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.VendorCodeColumnNumber].Text))
                    parsedProduct.VendorCode = worksheet.Cells[i, configuration.VendorCodeColumnNumber].Text;
                else
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.VendorCodeColumnNumber, string.Empty);

                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.QtyColumnNumber].Text)) {
                    if (double.TryParse(worksheet.Cells[i, configuration.QtyColumnNumber].Text, out double qty))
                        parsedProduct.Qty = qty;
                    else
                        throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.QtyColumnNumber, string.Empty);
                } else {
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.QtyColumnNumber, string.Empty);
                }

                if (configuration.WithTotalAmount) {
                    if (configuration.TotalAmountColumnNumber > 0) {
                        if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.TotalAmountColumnNumber].Text)) {
                            if (decimal.TryParse(worksheet.Cells[i, configuration.TotalAmountColumnNumber].Value.ToString(), out decimal price))
                                parsedProduct.UnitPrice = price / Convert.ToDecimal(parsedProduct.Qty);
                            else
                                throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.TotalAmountColumnNumber,
                                    string.Empty);
                        } else {
                            throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.TotalAmountColumnNumber, string.Empty);
                        }
                    }
                } else {
                    if (configuration.UnitPriceColumnNumber > 0) {
                        if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.UnitPriceColumnNumber].Text)) {
                            if (decimal.TryParse(worksheet.Cells[i, configuration.UnitPriceColumnNumber].Value.ToString(), out decimal price))
                                parsedProduct.UnitPrice = price;
                            else
                                throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.UnitPriceColumnNumber,
                                    string.Empty);
                        } else {
                            throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.UnitPriceColumnNumber, string.Empty);
                        }
                    }
                }

                if (configuration.WithNetWeight) {
                    if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.NetWeightColumnNumber].Text)) {
                        if (double.TryParse(Convert.ToString(worksheet.Cells[i, configuration.NetWeightColumnNumber].Value), out double netWeight))
                            parsedProduct.NetWeight =
                                configuration.IsWeightPerUnit
                                    ? netWeight
                                    : netWeight / parsedProduct.Qty;
                        else
                            throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.NetWeightColumnNumber,
                                string.Empty);
                    } else {
                        throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.NetWeightColumnNumber, string.Empty);
                    }
                }

                if (configuration.WithGrossWeight) {
                    if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.GrossWeightColumnNumber].Text)) {
                        if (double.TryParse(Convert.ToString(worksheet.Cells[i, configuration.GrossWeightColumnNumber].Value), out double grossWeight))
                            parsedProduct.GrossWeight =
                                configuration.IsWeightPerUnit
                                    ? grossWeight
                                    : grossWeight / parsedProduct.Qty;
                        else
                            throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.GrossWeightColumnNumber,
                                string.Empty);
                    } else {
                        throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.GrossWeightColumnNumber, string.Empty);
                    }
                }

                parsedProducts.Add(parsedProduct);
            }
        } catch (IndexOutOfRangeException) {
            throw new Exception(SupplyInvoiceResourceNames.CONFIGURATION_CONTAINS_ERRORS);
        } catch (Exception exc) {
            if (exc.Message.Equals("Column out of range")) throw new Exception(SupplyInvoiceResourceNames.CONFIGURATION_CONTAINS_ERRORS);

            throw;
        }

        return parsedProducts;
    }

    public List<ParsedProduct> GetProductsFromCartItemsDocumentByConfiguration(string pathToFile, CartItemsParseConfiguration configuration) {
        List<ParsedProduct> parsedProducts = new();

        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx"))
            if (inputtedFile.Extension.ToLower().Equals(".xls"))
                inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);
        if (!package.Workbook.Worksheets.Any()) throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.NoWorksheets, 0, 0, string.Empty);

        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        for (int i = configuration.StartRow; i <= configuration.EndRow; i++) {
            ParsedProduct parsedProduct = new();

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.VendorCodeColumnNumber].Text))
                parsedProduct.VendorCode = worksheet.Cells[i, configuration.VendorCodeColumnNumber].Text;
            else
                throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.VendorCodeColumnNumber, string.Empty);

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.QtyColumnNumber].Text)) {
                if (double.TryParse(worksheet.Cells[i, configuration.QtyColumnNumber].Text, out double qty))
                    parsedProduct.Qty = qty;
                else
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.QtyColumnNumber, string.Empty);
            } else {
                throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.QtyColumnNumber, string.Empty);
            }

            if (configuration.FromDateColumnNumber > 0) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.FromDateColumnNumber].Text)) {
                    if (DateTime.TryParse(Convert.ToString(worksheet.Cells[i, configuration.FromDateColumnNumber].Value), out DateTime fromDate))
                        parsedProduct.FromDate = fromDate;
                    else
                        throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.FromDateColumnNumber, string.Empty);
                } else {
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.FromDateColumnNumber, string.Empty);
                }
            }

            if (configuration.PriorityColumnNumber > 0) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.PriorityColumnNumber].Text)) {
                    if (int.TryParse(worksheet.Cells[i, configuration.PriorityColumnNumber].Text, out int priority))
                        parsedProduct.Priority = priority;
                    else
                        throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.PriorityColumnNumber, string.Empty);
                } else {
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.PriorityColumnNumber, string.Empty);
                }
            }

            parsedProducts.Add(parsedProduct);
        }

        return parsedProducts;
    }

    public List<ParsedProductForUkraine> GetProductsFromUkraineSupplyDocumentsByConfiguration(string pathToFile, UkraineOrderParseConfiguration configuration) {
        List<ParsedProductForUkraine> parsedProducts = new();

        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx"))
            if (inputtedFile.Extension.ToLower().Equals(".xls"))
                inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);
        if (!package.Workbook.Worksheets.Any()) throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.NoWorksheets, 0, 0, string.Empty);

        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        for (int i = configuration.StartRow; i <= configuration.EndRow; i++) {
            ParsedProductForUkraine parsedProduct = new();

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.VendorCodeColumnNumber].Text))
                parsedProduct.VendorCode = worksheet.Cells[i, configuration.VendorCodeColumnNumber].Text;
            else
                throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.VendorCodeColumnNumber, string.Empty);

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.SupplierColumnNumber].Text))
                parsedProduct.SupplierName = worksheet.Cells[i, configuration.SupplierColumnNumber].Text;
            else
                throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.SupplierColumnNumber, string.Empty);

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.QtyColumnNumber].Text)) {
                if (double.TryParse(worksheet.Cells[i, configuration.QtyColumnNumber].Text, out double qty))
                    parsedProduct.Qty = qty;
                else
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.QtyColumnNumber, string.Empty);
            } else {
                throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.QtyColumnNumber, string.Empty);
            }

            if (configuration.WithTotalAmount) {
                if (configuration.TotalAmountColumnNumber > 0) {
                    if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.TotalAmountColumnNumber].Text)) {
                        if (decimal.TryParse(worksheet.Cells[i, configuration.TotalAmountColumnNumber].Value.ToString(), out decimal price))
                            parsedProduct.TotalNetPrice = price / Convert.ToDecimal(parsedProduct.Qty);
                        else
                            throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.TotalAmountColumnNumber,
                                string.Empty);
                    } else {
                        throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.TotalAmountColumnNumber, string.Empty);
                    }
                }
            } else {
                if (configuration.UnitPriceColumnNumber > 0) {
                    if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.UnitPriceColumnNumber].Text)) {
                        if (decimal.TryParse(worksheet.Cells[i, configuration.UnitPriceColumnNumber].Value.ToString(), out decimal price))
                            parsedProduct.TotalNetPrice = price;
                        else
                            throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.UnitPriceColumnNumber,
                                string.Empty);
                    } else {
                        throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.UnitPriceColumnNumber, string.Empty);
                    }
                }
            }

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.NetWeightColumnNumber].Text)) {
                if (double.TryParse(worksheet.Cells[i, configuration.NetWeightColumnNumber].Text, out double netWeight))
                    parsedProduct.TotalNetWeight = netWeight / parsedProduct.Qty;
                else
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.NetWeightColumnNumber, string.Empty);
            } else {
                throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.NetWeightColumnNumber, string.Empty);
            }

            parsedProducts.Add(parsedProduct);
        }

        return parsedProducts;
    }

    public List<ParsedProductForUkraine> GetProductsForUkraineOrderFromSupplierByConfiguration(string pathToFile, UkraineOrderFromSupplierParseConfiguration configuration) {
        List<ParsedProductForUkraine> parsedProducts = new();

        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx"))
            if (inputtedFile.Extension.ToLower().Equals(".xls"))
                inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);
        if (!package.Workbook.Worksheets.Any()) throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.NoWorksheets, 0, 0, string.Empty);

        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        for (int i = configuration.StartRow; i <= configuration.EndRow; i++) {
            ParsedProductForUkraine parsedProduct = new();

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.VendorCodeColumnNumber].Text))
                parsedProduct.VendorCode = worksheet.Cells[i, configuration.VendorCodeColumnNumber].Text;
            else
                throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.VendorCodeColumnNumber, string.Empty);

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.QtyColumnNumber].Text)) {
                if (double.TryParse(worksheet.Cells[i, configuration.QtyColumnNumber].Text, out double qty))
                    parsedProduct.Qty = qty;
                else
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.QtyColumnNumber, string.Empty);
            } else {
                throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.QtyColumnNumber, string.Empty);
            }

            if (!configuration.IsPricePerItem) {
                if (configuration.TotalAmountColumnNumber > 0) {
                    if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.TotalAmountColumnNumber].Text)) {
                        if (decimal.TryParse(worksheet.Cells[i, configuration.TotalAmountColumnNumber].Value.ToString(), out decimal price))
                            parsedProduct.UnitPrice = price / Convert.ToDecimal(parsedProduct.Qty);
                        //                                        decimal.Round(
                        //                                            price / Convert.ToDecimal(parsedProduct.Qty),
                        //                                            2,
                        //                                            MidpointRounding.AwayFromZero
                        //                                        );
                        else
                            throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.TotalAmountColumnNumber,
                                string.Empty);
                    } else {
                        throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.TotalAmountColumnNumber, string.Empty);
                    }
                }
            } else {
                if (configuration.UnitPriceColumnNumber > 0) {
                    if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.UnitPriceColumnNumber].Text)) {
                        if (decimal.TryParse(worksheet.Cells[i, configuration.UnitPriceColumnNumber].Value.ToString(), out decimal price))
                            parsedProduct.UnitPrice = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
                        else
                            throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.UnitPriceColumnNumber,
                                string.Empty);
                    } else {
                        throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.UnitPriceColumnNumber, string.Empty);
                    }
                }
            }

            if (configuration.WithWeight) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.WeightColumnNumber].Text)) {
                    if (double.TryParse(Convert.ToString(worksheet.Cells[i, configuration.WeightColumnNumber].Value), out double netWeight))
                        parsedProduct.TotalNetWeight =
                            configuration.IsWeightPerItem
                                ? netWeight
                                : netWeight / parsedProduct.Qty;
                    else
                        throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.WeightColumnNumber, string.Empty);
                } else {
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.WeightColumnNumber, string.Empty);
                }
            }

            if (configuration.WithGrossWeight) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.GrossWeightColumnNumber].Text)) {
                    if (double.TryParse(Convert.ToString(worksheet.Cells[i, configuration.GrossWeightColumnNumber].Value), out double grossWeight))
                        parsedProduct.TotalGrossWeight =
                            configuration.IsWeightPerItem
                                ? grossWeight
                                : grossWeight / parsedProduct.Qty;
                    else
                        throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.WeightColumnNumber, string.Empty);
                } else {
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.GrossWeightColumnNumber, string.Empty);
                }
            }

            if (configuration.WithSpecificationCode) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.SpecificationCodeColumnNumber].Text))
                    parsedProduct.SpecificationCode = worksheet.Cells[i, configuration.SpecificationCodeColumnNumber].Text;
                else
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.SpecificationCodeColumnNumber, string.Empty);
            }

            if (configuration.WithIsImportedProduct) {
                string value = worksheet.Cells[i, configuration.IsImportedProduct].Text;

                parsedProduct.ProductIsImported = !string.IsNullOrEmpty(value);
            }

            parsedProducts.Add(parsedProduct);
        }

        return parsedProducts;
    }

    public List<ProductForUpload> GetProductsForUploadByConfiguration(string pathToFile, ProductUploadParseConfiguration configuration) {
        List<ProductForUpload> products = new();

        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx"))
            if (inputtedFile.Extension.ToLower().Equals(".xls"))
                inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);
        if (!package.Workbook.Worksheets.Any()) throw new ProductUploadParseException(ProductUploadParseExceptionType.NoWorksheets, 0, 0, string.Empty);

        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        for (int i = configuration.StartRow; i <= configuration.EndRow; i++) {
            ProductForUpload parsedProduct = new();

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.VendorCode].Text))
                parsedProduct.VendorCode = worksheet.Cells[i, configuration.VendorCode].Text;
            else
                throw new ProductUploadParseException(ProductUploadParseExceptionType.EmptyValue, configuration.VendorCode, i, string.Empty);

            if (configuration.WithNewVendorCode) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.NewVendorCode].Text))
                    parsedProduct.NewVendorCode = worksheet.Cells[i, configuration.NewVendorCode].Text;
                else
                    throw new ProductUploadParseException(ProductUploadParseExceptionType.EmptyValue, configuration.NewVendorCode, i, string.Empty);
            }

            if (configuration.WithNameRU) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.NameRU].Text))
                    parsedProduct.Name = worksheet.Cells[i, configuration.NameRU].Text;
                else
                    throw new ProductUploadParseException(ProductUploadParseExceptionType.EmptyValue, configuration.NameRU, i, string.Empty);
            }

            if (configuration.WithNameUA) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.NameUA].Text))
                    parsedProduct.NameUA = worksheet.Cells[i, configuration.NameUA].Text;
                else
                    throw new ProductUploadParseException(ProductUploadParseExceptionType.EmptyValue, configuration.NameUA, i, string.Empty);
            }

            if (configuration.WithNamePL) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.NamePL].Text))
                    parsedProduct.NamePL = worksheet.Cells[i, configuration.NamePL].Text;
                else
                    throw new ProductUploadParseException(ProductUploadParseExceptionType.EmptyValue, configuration.NamePL, i, string.Empty);
            }

            if (configuration.WithDescriptionRU)
                parsedProduct.Description = !string.IsNullOrEmpty(worksheet.Cells[i, configuration.DescriptionRU].Text)
                    ? worksheet.Cells[i, configuration.DescriptionRU].Text
                    : string.Empty;

            if (configuration.WithDescriptionUA)
                parsedProduct.DescriptionUA = !string.IsNullOrEmpty(worksheet.Cells[i, configuration.DescriptionUA].Text)
                    ? worksheet.Cells[i, configuration.DescriptionUA].Text
                    : string.Empty;

            if (configuration.WithDescriptionPL)
                parsedProduct.DescriptionPL = !string.IsNullOrEmpty(worksheet.Cells[i, configuration.DescriptionPL].Text)
                    ? worksheet.Cells[i, configuration.DescriptionPL].Text
                    : string.Empty;

            if (configuration.WithProductGroup)
                parsedProduct.ProductGroup = !string.IsNullOrEmpty(worksheet.Cells[i, configuration.ProductGroup].Text)
                    ? worksheet.Cells[i, configuration.ProductGroup].Text
                    : string.Empty;

            if (configuration.WithMeasureUnit)
                parsedProduct.MeasureUnit = !string.IsNullOrEmpty(worksheet.Cells[i, configuration.MeasureUnit].Text)
                    ? worksheet.Cells[i, configuration.MeasureUnit].Text
                    : string.Empty;

            if (configuration.WithWeight) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.Weight].Text)) {
                    if (double.TryParse(worksheet.Cells[i, configuration.Weight].Text, out double weight))
                        parsedProduct.Weight = weight;
                    else
                        throw new ProductUploadParseException(ProductUploadParseExceptionType.IncorrectDataType, configuration.Weight, i, string.Empty);
                } else {
                    parsedProduct.Weight = 0d;
                }
            }

            if (configuration.WithMainOriginalNumber)
                parsedProduct.MainOriginalNumber = !string.IsNullOrEmpty(worksheet.Cells[i, configuration.MainOriginalNumber].Text)
                    ? worksheet.Cells[i, configuration.MainOriginalNumber].Text
                    : string.Empty;

            if (configuration.WithTop)
                parsedProduct.Top = !string.IsNullOrEmpty(worksheet.Cells[i, configuration.Top].Text) ? worksheet.Cells[i, configuration.Top].Text : string.Empty;

            if (configuration.WithOrderStandard)
                parsedProduct.OrderStandard = !string.IsNullOrEmpty(worksheet.Cells[i, configuration.OrderStandard].Text)
                    ? worksheet.Cells[i, configuration.OrderStandard].Text
                    : string.Empty;

            if (configuration.WithPackingStandard)
                parsedProduct.PackingStandard = !string.IsNullOrEmpty(worksheet.Cells[i, configuration.PackingStandard].Text)
                    ? worksheet.Cells[i, configuration.PackingStandard].Text
                    : string.Empty;

            if (configuration.WithSize)
                parsedProduct.Size = !string.IsNullOrEmpty(worksheet.Cells[i, configuration.Size].Text) ? worksheet.Cells[i, configuration.Size].Text : string.Empty;

            if (configuration.WithVolume)
                parsedProduct.Volume = !string.IsNullOrEmpty(worksheet.Cells[i, configuration.Volume].Text) ? worksheet.Cells[i, configuration.Volume].Text : string.Empty;

            if (configuration.WithUCGFEA)
                parsedProduct.UCGFEA = !string.IsNullOrEmpty(worksheet.Cells[i, configuration.UCGFEA].Text) ? worksheet.Cells[i, configuration.UCGFEA].Text : string.Empty;

            if (configuration.WithIsForWeb) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.IsForWeb].Text)) {
                    if (int.TryParse(worksheet.Cells[i, configuration.IsForWeb].Text, out int val))
                        parsedProduct.IsForWeb = val.Equals(1);
                    else
                        throw new ProductUploadParseException(ProductUploadParseExceptionType.IncorrectDataType, configuration.IsForWeb, i, string.Empty);
                } else {
                    parsedProduct.IsForWeb = false;
                }
            }

            if (configuration.WithIsForSale) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.IsForSale].Text)) {
                    if (int.TryParse(worksheet.Cells[i, configuration.IsForSale].Text, out int val))
                        parsedProduct.IsForSale = val.Equals(1);
                    else
                        throw new ProductUploadParseException(ProductUploadParseExceptionType.IncorrectDataType, configuration.IsForSale, i, string.Empty);
                } else {
                    parsedProduct.IsForSale = false;
                }
            }

            if (configuration.WithPrices)
                foreach (ProductUploadPriceConfiguration priceConfiguration in configuration.PriceConfigurations) {
                    ProductForUploadPricing pricing = new() {
                        PricingId = priceConfiguration.PricingId,
                        Name = priceConfiguration.Name
                    };

                    if (!string.IsNullOrEmpty(worksheet.Cells[i, priceConfiguration.ColumnNumber].Text)) {
                        if (decimal.TryParse(worksheet.Cells[i, priceConfiguration.ColumnNumber].Text, out decimal price))
                            pricing.Price = price;
                        else
                            throw new ProductUploadParseException(ProductUploadParseExceptionType.IncorrectDataType, priceConfiguration.ColumnNumber, i, string.Empty);
                    } else {
                        continue;
                    }

                    parsedProduct.Pricings.Add(pricing);
                }

            products.Add(parsedProduct);
        }

        return products;
    }

    public List<AnalogueForUpload> GetAnaloguesForUploadByConfiguration(string pathToFile, AnaloguesUploadParseConfiguration configuration) {
        List<AnalogueForUpload> products = new();

        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx"))
            if (inputtedFile.Extension.ToLower().Equals(".xls"))
                inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);
        if (!package.Workbook.Worksheets.Any()) throw new ProductUploadParseException(ProductUploadParseExceptionType.NoWorksheets, 0, 0, string.Empty);

        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        for (int i = configuration.From; i <= configuration.To; i++) {
            AnalogueForUpload parsedAnalogue = new();

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.VendorCode].Text))
                parsedAnalogue.VendorCode = worksheet.Cells[i, configuration.VendorCode].Text;
            else
                throw new ProductUploadParseException(ProductUploadParseExceptionType.EmptyValue, configuration.VendorCode, i, string.Empty);

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.AnalogueVendorCode].Text))
                parsedAnalogue.AnalogueVendorCode = worksheet.Cells[i, configuration.AnalogueVendorCode].Text;
            else
                throw new ProductUploadParseException(ProductUploadParseExceptionType.EmptyValue, configuration.AnalogueVendorCode, i, string.Empty);

            parsedAnalogue.ProductColumn = configuration.VendorCode;
            parsedAnalogue.AnalogueColumn = configuration.AnalogueVendorCode;
            parsedAnalogue.Row = i;

            products.Add(parsedAnalogue);
        }

        return products;
    }

    public List<ComponentForUpload> GetComponentsForUploadByConfiguration(string pathToFile, ComponentsUploadParseConfiguration configuration) {
        List<ComponentForUpload> products = new();

        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx"))
            if (inputtedFile.Extension.ToLower().Equals(".xls"))
                inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);
        if (!package.Workbook.Worksheets.Any()) throw new ProductUploadParseException(ProductUploadParseExceptionType.NoWorksheets, 0, 0, string.Empty);

        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        for (int i = configuration.From; i <= configuration.To; i++) {
            ComponentForUpload parsedComponent = new();

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.VendorCode].Text))
                parsedComponent.VendorCode = worksheet.Cells[i, configuration.VendorCode].Text;
            else
                throw new ProductUploadParseException(ProductUploadParseExceptionType.EmptyValue, configuration.VendorCode, i, string.Empty);

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.ComponentVendorCode].Text))
                parsedComponent.ComponentVendorCode = worksheet.Cells[i, configuration.ComponentVendorCode].Text;
            else
                throw new ProductUploadParseException(ProductUploadParseExceptionType.EmptyValue, configuration.ComponentVendorCode, i, string.Empty);

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.Qty].Text)) {
                if (int.TryParse(worksheet.Cells[i, configuration.Qty].Text, out int value)) {
                    if (value <= 0)
                        throw new ProductUploadParseException(ProductUploadParseExceptionType.IncorrectDataType, configuration.Qty, i, string.Empty);

                    parsedComponent.SetComponentsQty = value;
                } else {
                    throw new ProductUploadParseException(ProductUploadParseExceptionType.IncorrectDataType, configuration.Qty, i, string.Empty);
                }
            } else {
                throw new ProductUploadParseException(ProductUploadParseExceptionType.EmptyValue, configuration.ComponentVendorCode, i, string.Empty);
            }

            products.Add(parsedComponent);
        }

        return products;
    }

    public List<OriginalNumberForUpload> GetOriginalNumbersForUploadByConfiguration(string pathToFile, OriginalNumbersUploadParseConfiguration configuration) {
        List<OriginalNumberForUpload> originalNumbers = new();

        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx"))
            if (inputtedFile.Extension.ToLower().Equals(".xls"))
                inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);
        if (!package.Workbook.Worksheets.Any()) throw new ProductUploadParseException(ProductUploadParseExceptionType.NoWorksheets, 0, 0, string.Empty);

        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        for (int i = configuration.From; i <= configuration.To; i++) {
            OriginalNumberForUpload parsedOriginalNumber = new();

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.VendorCode].Text))
                parsedOriginalNumber.VendorCode = worksheet.Cells[i, configuration.VendorCode].Text;
            else
                throw new ProductUploadParseException(ProductUploadParseExceptionType.EmptyValue, configuration.VendorCode, i, string.Empty);

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.OriginalNumber].Text))
                parsedOriginalNumber.OriginalNumber = worksheet.Cells[i, configuration.OriginalNumber].Text;
            else
                throw new ProductUploadParseException(ProductUploadParseExceptionType.EmptyValue, configuration.OriginalNumber, i, string.Empty);

            originalNumbers.Add(parsedOriginalNumber);
        }

        return originalNumbers;
    }

    public List<ParsedProduct> GetProductsFromUploadForCapitalizationByConfiguration(string pathToFile, ProductCapitalizationParseConfiguration configuration) {
        List<ParsedProduct> parsedProducts = new();

        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx"))
            if (inputtedFile.Extension.ToLower().Equals(".xls"))
                inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);
        if (!package.Workbook.Worksheets.Any()) throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.NoWorksheets, 0, 0, string.Empty);

        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        for (int i = configuration.StartRow; i <= configuration.EndRow; i++) {
            ParsedProduct parsedProduct = new();

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.VendorCodeColumnNumber].Text))
                parsedProduct.VendorCode = worksheet.Cells[i, configuration.VendorCodeColumnNumber].Text;
            else
                throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.VendorCodeColumnNumber, string.Empty);

            if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.QtyColumnNumber].Text)) {
                if (double.TryParse(worksheet.Cells[i, configuration.QtyColumnNumber].Text, out double qty))
                    parsedProduct.Qty = qty;
                else
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.QtyColumnNumber, string.Empty);
            } else {
                throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.QtyColumnNumber, string.Empty);
            }

            if (configuration.WithWeight) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.WeightColumnNumber].Text)) {
                    if (double.TryParse(worksheet.Cells[i, configuration.WeightColumnNumber].Text, out double weight))
                        parsedProduct.GrossWeight =
                            configuration.WeightPerItem
                                ? weight
                                : weight / parsedProduct.Qty;
                    else
                        throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.WeightColumnNumber, string.Empty);
                } else {
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.WeightColumnNumber, string.Empty);
                }
            }

            if (configuration.WithPrice) {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.PriceColumnNumber].Text)) {
                    if (decimal.TryParse(worksheet.Cells[i, configuration.PriceColumnNumber].Text, out decimal price))
                        parsedProduct.UnitPrice =
                            configuration.PricePerItem
                                ? price
                                : price / Convert.ToDecimal(parsedProduct.Qty);
                    else
                        throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.IncorrectDataType, i, configuration.PriceColumnNumber, string.Empty);
                } else {
                    throw new SupplyDocumentParseException(SupplyDocumentParseExceptionType.EmptyValue, i, configuration.PriceColumnNumber, string.Empty);
                }
            }

            parsedProducts.Add(parsedProduct);
        }

        return parsedProducts;
    }

    public List<ParsedProductSpecification>
        GetProductSpecificationsFromUploadByConfiguration(string pathToFile, ProductSpecificationParseConfiguration configuration) {
        List<ParsedProductSpecification> parsedSpecifications = new();

        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx"))
            if (inputtedFile.Extension.ToLower().Equals(".xls"))
                inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);
        if (!package.Workbook.Worksheets.Any()) return parsedSpecifications;

        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        for (int i = configuration.StartRow; i <= configuration.EndRow; i++) {
            ParsedProductSpecification parsedSpecification = new();

            try {
                if (!string.IsNullOrEmpty(worksheet.Cells[i, configuration.VendorCode].Text)) {
                    parsedSpecification.VendorCode = worksheet.Cells[i, configuration.VendorCode].Text;
                } else {
                    parsedSpecification.VendorCode = string.Empty;
                    parsedSpecification.HasError = true;
                    parsedSpecification.RowNumber = i;
                    parsedSpecification.ColumnNumber = configuration.VendorCode;
                }
            } catch (Exception) {
                parsedSpecification.VendorCode = string.Empty;
                parsedSpecification.HasError = true;
                parsedSpecification.RowNumber = i;
                parsedSpecification.ColumnNumber = configuration.VendorCode;
            }

            try {
                parsedSpecification.SpecificationCode =
                    !string.IsNullOrEmpty(worksheet.Cells[i, configuration.SpecificationCode].Text)
                        ? worksheet.Cells[i, configuration.SpecificationCode].Text
                        : string.Empty;
            } catch (Exception) {
                parsedSpecification.SpecificationCode = string.Empty;
            }

            try {
                parsedSpecification.CustomsValue =
                    !string.IsNullOrEmpty(worksheet.Cells[i, configuration.CustomsValue].Text)
                        ? decimal.TryParse(worksheet.Cells[i, configuration.CustomsValue].Text, out decimal parsedPercent) ? parsedPercent : 0m
                        : 0m;
            } catch (Exception) {
                parsedSpecification.CustomsValue = 0m;
            }

            try {
                parsedSpecification.Price =
                    !string.IsNullOrEmpty(worksheet.Cells[i, configuration.Price].Text)
                        ? decimal.TryParse(worksheet.Cells[i, configuration.Price].Text, out decimal parsedPercent) ? parsedPercent : 0m
                        : 0m;
            } catch (Exception) {
                parsedSpecification.Price = 0m;
            }

            try {
                parsedSpecification.Qty =
                    !string.IsNullOrEmpty(worksheet.Cells[i, configuration.Qty].Text)
                        ? double.TryParse(worksheet.Cells[i, configuration.Qty].Text, out double parsedQty) ? parsedQty : 0d
                        : 0d;
            } catch (Exception) {
                parsedSpecification.Qty = 0d;
            }

            try {
                parsedSpecification.Duty =
                    !string.IsNullOrEmpty(worksheet.Cells[i, configuration.Duty].Text)
                        ? decimal.TryParse(worksheet.Cells[i, configuration.Duty].Text, out decimal parsedPercent) ? parsedPercent : 0m
                        : 0m;
            } catch (Exception) {
                parsedSpecification.Duty = 0m;
            }

            try {
                parsedSpecification.VATValue =
                    !string.IsNullOrEmpty(worksheet.Cells[i, configuration.VATValue].Text)
                        ? decimal.TryParse(worksheet.Cells[i, configuration.VATValue].Text, out decimal parsedPercent) ? parsedPercent : 0m
                        : 0m;
            } catch (Exception) {
                parsedSpecification.VATValue = 0m;
            }

            parsedSpecifications.Add(parsedSpecification);
        }

        return parsedSpecifications;
    }

    public List<ProductSpecificationWithVendorCode> GetProductSpecificationWithVendorCodesFromXlsx(string pathToFile) {
        List<ProductSpecificationWithVendorCode> toReturn = new();

        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx"))
            if (inputtedFile.Extension.ToLower().Equals(".xls"))
                inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);
        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        int column = worksheet.Dimension.Start.Column;

        for (int i = worksheet.Dimension.Start.Row; i <= worksheet.Dimension.End.Row; i++) {
            ProductSpecificationWithVendorCode specification = new() {
                VendorCode = worksheet.Cells[i, column].Text,
                Name = worksheet.Cells[i, column + 1].Text,
                Code = worksheet.Cells[i, column + 2].Text
            };

            if (ProductSpecificationIsValid(specification)) toReturn.Add(specification);
        }

        return toReturn;
    }

    public List<PackingListItemWithVendorCode> GetPackingListItemsWithVendorCodesFromXlsx(string pathToFile) {
        List<PackingListItemWithVendorCode> itemsToReturn = new();

        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx"))
            if (inputtedFile.Extension.ToLower().Equals(".xls"))
                inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);
        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        int column = worksheet.Dimension.Start.Column;

        for (int i = worksheet.Dimension.Start.Row + 1; i <= worksheet.Dimension.End.Row; i++) {
            PackingListItemWithVendorCode item = new() {
                VendorCode = worksheet.Cells[i, column].Text,
                NetWeight = Convert.ToDouble(worksheet.Cells[i, column + 1].Value),
                GrossWeight = Convert.ToDouble(worksheet.Cells[i, column + 2].Value),
                Qty = Convert.ToDouble(worksheet.Cells[i, column + 3].Value),
                UnitPrice = Convert.ToDecimal(worksheet.Cells[i, column + 4].Value)
            };

            if (PackingListItemIsValid(item)) itemsToReturn.Add(item);
        }

        return itemsToReturn;
    }

    public List<ProductMovementItemFromFile> GetDepreciatedItemsFromXlsx(
        string pathToFile,
        DepreciatedAndTransferParseConfiguration parseConfig) {
        List<ProductMovementItemFromFile> itemsToReturn = new();

        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx") && inputtedFile.Extension.ToLower().Equals(".xls"))
            inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);
        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        for (int i = parseConfig.StartRow; i <= parseConfig.EndRow; i++) {
            string vendorCode = worksheet.Cells[i, parseConfig.VendorCodeColumnNumber].Text;

            if (worksheet.Cells[i, parseConfig.QtyColumnNumber].Value == null)
                throw new LocalizedException(
                    DepreciatedOrderResourceNames.ROW_IS_EMPTY,
                    new object[] { i }
                );

            if (!double.TryParse(worksheet.Cells[i, parseConfig.QtyColumnNumber].Value.ToString(), out double qty))
                throw new LocalizedException(
                    DepreciatedOrderResourceNames.QTY_IS_EMPTY,
                    new object[] { i, parseConfig.QtyColumnNumber });

            if (string.IsNullOrEmpty(vendorCode))
                throw new LocalizedException(
                    DepreciatedOrderResourceNames.VENDOR_CODE_IS_EMPTY,
                    new object[] { i, parseConfig.VendorCodeColumnNumber });

            itemsToReturn.Add(new ProductMovementItemFromFile {
                VendorCode = vendorCode,
                Qty = qty
            });
        }

        return itemsToReturn;
    }

    public List<ProductPlacementMovementVendorCode> GetProductPlacementMovementFromXlsx(string pathToFile, PlacementMovementsStorageParseConfiguration configuration) {
        List<ProductPlacementMovementVendorCode> parsedSpecifications = new();
        List<ProductPlacement> productPlacements = new();

        NumberStyles style = NumberStyles.AllowThousands;
        CultureInfo culture = CultureInfo.InvariantCulture;
        FileInfo inputtedFile = new(pathToFile);

        if (!inputtedFile.Extension.ToLower().Equals(".xlsx"))
            if (inputtedFile.Extension.ToLower().Equals(".xls"))
                inputtedFile = new FileInfo(HelperXlsManager.ConvertXlsToXlsx(inputtedFile.FullName));

        using ExcelPackage package = new(inputtedFile);

        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();


        for (int i = configuration.StartRow; i <= configuration.EndRow; i++) {
            ProductPlacementMovementVendorCode specification = new() {
                VendorCode = worksheet.Cells[i, configuration.ColumnVendorCode].Text
            };


            string[] qtyString = worksheet.Cells[i, configuration.ColumnQty].Text.Split(',');


            string qty = string.Concat(qtyString[0].Where(c => !char.IsWhiteSpace(c)));

            specification.Qty = int.Parse(qty);

            string[] PlacementString = worksheet.Cells[i, configuration.ColumnPlacement].Text.Split('-');

            if (PlacementString.Length > 0 && PlacementString[0] != null)
                specification.StorageNumber = PlacementString[0];
            if (PlacementString.Length > 1 && PlacementString[1] != null)
                specification.RowNumber = PlacementString[1];
            if (PlacementString.Length > 2 && PlacementString[2] != null)
                specification.CellNumber = PlacementString[2];

            parsedSpecifications.Add(specification);
        }

        return parsedSpecifications;
    }

    private static bool ProductSpecificationIsValid(ProductSpecificationWithVendorCode specification) {
        if (string.IsNullOrEmpty(specification.VendorCode)) return false;

        if (string.IsNullOrEmpty(specification.Name)) return false;

        return !string.IsNullOrEmpty(specification.Code);
    }

    private static bool PackingListItemIsValid(PackingListItemWithVendorCode item) {
        return !string.IsNullOrEmpty(item.VendorCode);
    }
}