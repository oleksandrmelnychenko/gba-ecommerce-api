using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using GBA.Common.Extensions;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Consignments;
using GBA.Domain.EntityHelpers.Supplies;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public sealed class ProductsXlsManager : BaseXlsManager, IProductsXlsManager {
    public string ExportMissingVendorCodes(string path, List<string> missingVendorCodes) {
        string fileName = Path.Combine(path, $"missing_products_{Guid.NewGuid()}.xlsx");

        using ExcelPackage package = NewExcelPackage(fileName);
        int row = 1;
        const int column = 1;

        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Missing Products");

        worksheet.Cells[row, column].Value = "Vendor Code";

        row++;

        foreach (string vendorCode in missingVendorCodes) {
            worksheet.Cells[row, column].Value = vendorCode;

            row++;
        }

        //Adding default width/height for columns
        worksheet.Column(1).Width = 12.5;

        //Setting document properties.
        package.Workbook.Properties.Title = "Missing products";
        package.Workbook.Properties.Author = "Concord CRM";
        package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

        //Saving the file.
        package.Save();

        return fileName;
    }

    public (string xlsxFile, string pdfFile) ExportSadProductSpecification(
        string path,
        Sad sadPl,
        Sad sadUk,
        List<GroupedProductSpecification> plSpecifications,
        List<GroupedProductSpecification> ukSpecifications,
        bool isFromSale = false
    ) {
        string fileName = Path.Combine(path, $"EX_SAD_Specification_{sadPl.Number}_{DateTime.Now.ToString("dd.MM.yyyy")}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Polish");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(0.87, 1);
            worksheet.SetColumnWidth(5.91, 2);
            worksheet.SetColumnWidth(40.24, 3);
            worksheet.SetColumnWidth(7.11, 4);
            worksheet.SetColumnWidth(8.81, 5);
            worksheet.SetColumnWidth(12.97, 6);
            worksheet.SetColumnWidth(16.01, 7);
            worksheet.SetColumnWidth(9.88, 8);
            worksheet.SetColumnWidth(7.81, 9);
            worksheet.SetColumnWidth(10.57, 10);
            worksheet.SetColumnWidth(10.57, 11);
            worksheet.SetColumnWidth(10.57, 12);
            worksheet.SetColumnWidth(12.87, 13);
            worksheet.SetColumnWidth(0.61, 14);

            worksheet.SetRowHeight(12.60, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 });

            using (ExcelRange range = worksheet.Cells[1, 2, 1, 3]) {
                range.Merge = true;
                range.Value = "Oryginał / Kopia";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 1, 13]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "Przemysl, {0:dd.MM.yyyy}",
                        sadPl.FromDate
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            using (ExcelRange range = worksheet.Cells[2, 2, 4, 13]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "SPECYFIKACJA DO FAKTURY NR EX{0}/{1}/{2}",
                        string.Format(
                            "{0:D3}",
                            sadPl.Number.StartsWith("EX")
                                ? Convert.ToInt64(sadPl.Number.Substring(2, 10))
                                : Convert.ToInt64(sadPl.Number)
                        ),
                        string.Format("{0:D2}", sadPl.FromDate.Month),
                        sadPl.FromDate.Year
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[5, 2, 14, 3]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value =
                    "Sprzedawca: CONCORD.PL Sp z o.o.,\r37 - 700, Przemyśl, ul.Gen.Jakuba Jasińskiego 58,\rNIP 8133680920,\rIBAN NO EURO: PL55193013182740072619290003\rSWIFT : POLUPLPR\rBRANCH CODE: 1930 1318";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            string completeRecipient = "Nabywca: ";

            if (isFromSale) {
                if (sadPl.Sales.Any()) {
                    Client client = sadPl.Sales.First().ClientAgreement.Client;

                    if (!string.IsNullOrEmpty(client.FirstName) && !string.IsNullOrEmpty(client.LastName))
                        completeRecipient = $"{client.LastName} {client.FirstName} {client.MiddleName} ";
                    else
                        completeRecipient += client.FullName;

                    completeRecipient +=
                        string.IsNullOrEmpty(client.ActualAddress)
                            ? string.Empty
                            : client.ActualAddress + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.City)
                            ? string.Empty
                            : client.RegionCode.City + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.District)
                            ? string.Empty
                            : client.RegionCode.District + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.TIN)
                            ? string.Empty
                            : "\rNIP " + client.TIN;
                }
            } else {
                if (sadPl.OrganizationClient != null) {
                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.OrganizationClient.FullName)
                            ? string.Empty
                            : sadPl.OrganizationClient.FullName + "  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.OrganizationClient.Address)
                            ? string.Empty
                            : sadPl.OrganizationClient.Address + ", ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.OrganizationClient.City)
                            ? string.Empty
                            : sadPl.OrganizationClient.City + ", ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.OrganizationClient.Country)
                            ? string.Empty
                            : sadPl.OrganizationClient.Country + ", ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.OrganizationClient.NIP)
                            ? string.Empty
                            : "\rNIP " + sadPl.OrganizationClient.NIP;
                }
            }

            using (ExcelRange range = worksheet.Cells[5, 4, 14, 13]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = completeRecipient;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            using (ExcelRange range = worksheet.Cells[15, 2, 17, 2]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Lp.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 3, 17, 3]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Nazwa towaru lub usługi";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 4, 17, 4]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Jednostka miary";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 5, 17, 5]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Ilość towaru / usługi";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 6, 17, 6]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Kraj pochodzenia";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 7, 17, 7]) {
                range.Merge = true;
                range.Value = "Firma";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 8, 17, 8]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "marka";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 9, 17, 9]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Cena";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 10, 17, 10]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Wartość";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 11, 17, 11]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Waga netto";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 12, 17, 12]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Waga brutto";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 13, 17, 13]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Kod celny TARIC	";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 2, 17, 13]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            int row = 18;
            int rowIndexer = 1;

            double totalNetWeight = 0d;
            double totalGrossWeight = 0d;
            decimal totalPrice = 0m;

            foreach (GroupedProductSpecification specification in plSpecifications) {
                int startRow = row;
                int subRowIndexer = 1;

                double currentStepNetWeight = 0d;
                double currentStepGrossWeight = 0d;
                decimal currentStepTotalPrice = 0m;

                if (isFromSale)
                    foreach (OrderItem item in specification.OrderItems) {
                        string countryCode = "";
                        string supplierName = "";
                        string supplierMark = "";

                        double netWeight = 0d;

                        decimal currentUnitPrice = item.PricePerItem;

                        if (item.ProductLocations.Any()) {
                            ProductLocation location =
                                item
                                    .ProductLocations
                                    .First();

                            netWeight = Math.Round(location.ProductPlacement.PackingListPackageOrderItem.NetWeight * item.Qty, 3);

                            countryCode =
                                location.ProductPlacement?.PackingListPackageOrderItem?.Supplier?.Country != null
                                    ? location.ProductPlacement.PackingListPackageOrderItem.Supplier.Country.Code
                                    : "";

                            supplierName = location.ProductPlacement?.PackingListPackageOrderItem?.Supplier?.SupplierName ?? "";

                            supplierMark = location.ProductPlacement?.PackingListPackageOrderItem?.Supplier?.Brand ?? "";
                        }

                        decimal currentTotalPrice =
                            decimal.Round(
                                currentUnitPrice * Convert.ToDecimal(item.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        currentStepTotalPrice =
                            decimal.Round(
                                currentStepTotalPrice + currentTotalPrice,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        currentStepNetWeight =
                            Math.Round(currentStepNetWeight + netWeight, 3);

                        using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                            range.Merge = true;
                            range.Value = $"{rowIndexer}.{subRowIndexer}";
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                            range.Merge = true;
                            range.Value =
                                string.Format(
                                    "{0} {1}",
                                    item.Product.VendorCode,
                                    item.Product.Name
                                );
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                            range.Merge = true;
                            range.Value = item.Product.MeasureUnit.Name;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                            range.Merge = true;
                            range.Value = item.Qty;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                            range.Merge = true;
                            range.Value = countryCode;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                            range.Merge = true;
                            range.Value = supplierName;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                            range.Merge = true;
                            range.Value = supplierMark;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                            range.Merge = true;
                            range.Value = currentUnitPrice;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                            range.Merge = true;
                            range.Value = currentTotalPrice;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                            range.Merge = true;
                            range.Value = netWeight;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.000";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                            range.Merge = true;
                            range.Value = netWeight;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.000";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                            range.Merge = true;
                            range.Value = specification.SpecificationCode;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        worksheet.SetRowHeight(12.80, row);

                        row++;

                        subRowIndexer++;
                    }
                else
                    foreach (SadItem item in specification.SadItems) {
                        string countryCode;
                        string supplierName;
                        string supplierMark;

                        double netWeight;
                        double grossWeight = 0d;

                        decimal currentUnitPrice;

                        netWeight = item.TotalNetWeight;
                        currentUnitPrice = item.SupplyOrderUkraineCartItem.UnitPrice;

                        grossWeight =
                            sadPl.SadPallets.Any()
                                ? sadPl
                                    .SadPallets
                                    .SelectMany(pallet =>
                                        pallet
                                            .SadPalletItems
                                            .Where(i => i.SadItemId.Equals(item.Id)))
                                    .Aggregate(grossWeight, (current, palletItem) =>
                                        Math.Round(current + palletItem.TotalGrossWeight, 3, MidpointRounding.AwayFromZero))
                                : item.TotalGrossWeight;

                        countryCode =
                            item.SupplyOrderUkraineCartItem?.Supplier?.Country != null
                                ? item.SupplyOrderUkraineCartItem.Supplier.Country.Code
                                : "";

                        supplierName = item.SupplyOrderUkraineCartItem?.Supplier?.SupplierName ?? "";

                        supplierMark = item.SupplyOrderUkraineCartItem?.Supplier?.Brand;

                        decimal currentTotalPrice =
                            decimal.Round(
                                currentUnitPrice * Convert.ToDecimal(item.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        currentStepTotalPrice =
                            decimal.Round(
                                currentStepTotalPrice + currentTotalPrice,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        currentStepNetWeight =
                            Math.Round(currentStepNetWeight + netWeight, 3, MidpointRounding.AwayFromZero);

                        currentStepGrossWeight =
                            Math.Round(currentStepGrossWeight + grossWeight, 3, MidpointRounding.AwayFromZero);

                        using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                            range.Merge = true;
                            range.Value = $"{rowIndexer}.{subRowIndexer}";
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                            range.Merge = true;
                            range.Value =
                                string.Format(
                                    "{0} {1}",
                                    item.SupplyOrderUkraineCartItem.Product.VendorCode,
                                    item.SupplyOrderUkraineCartItem.Product.Name
                                );
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                            range.Merge = true;
                            range.Value = item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                            range.Merge = true;
                            range.Value = item.Qty;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                            range.Merge = true;
                            range.Value = countryCode;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                            range.Merge = true;
                            range.Value = supplierName;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                            range.Merge = true;
                            range.Value = supplierMark;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                            range.Merge = true;
                            range.Value = currentUnitPrice;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                            range.Merge = true;
                            range.Value = currentTotalPrice;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                            range.Merge = true;
                            range.Value = netWeight;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.000";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                            range.Merge = true;
                            range.Value = grossWeight;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.000";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                            range.Merge = true;
                            range.Value = specification.SpecificationCode;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        worksheet.SetRowHeight(12.80, row);

                        row++;

                        subRowIndexer++;
                    }

                totalPrice =
                    decimal.Round(totalPrice + currentStepTotalPrice, 2, MidpointRounding.AwayFromZero);

                totalNetWeight = Math.Round(totalNetWeight + currentStepNetWeight, 3, MidpointRounding.AwayFromZero);
                totalGrossWeight = Math.Round(totalGrossWeight + currentStepGrossWeight, 3, MidpointRounding.AwayFromZero);

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Merge = true;
                    range.Value = isFromSale
                        ? specification.OrderItems.Sum(i => i.Qty)
                        : specification.SadItems.Sum(i => i.Qty);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                    range.Merge = true;
                    range.Value = currentStepTotalPrice;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                    range.Merge = true;
                    range.Value = currentStepNetWeight;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.000";
                }

                using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                    range.Merge = true;
                    range.Value = currentStepGrossWeight;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.000";
                }

                using (ExcelRange range = worksheet.Cells[startRow, 2, row, 13]) {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
                }

                worksheet.SetRowHeight(12.80, row);

                row++;

                rowIndexer++;
            }

            using (ExcelRange range = worksheet.Cells[row, 2, row + 1, 4]) {
                range.Merge = true;
                range.Value = "Ogółem:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row + 1, 5]) {
                range.Merge = true;
                range.Value = isFromSale
                    ? plSpecifications.Sum(s => s.OrderItems.Sum(i => i.Qty))
                    : plSpecifications.Sum(s => s.SadItems.Sum(i => i.Qty));
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 10, row + 1, 10]) {
                range.Merge = true;
                range.Value = totalPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 11, row + 1, 11]) {
                range.Merge = true;
                range.Value = totalNetWeight;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.000";
            }

            using (ExcelRange range = worksheet.Cells[row, 12, row + 1, 12]) {
                range.Merge = true;
                range.Value = totalGrossWeight > 0d ? totalGrossWeight : totalNetWeight;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.000";
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row + 1, 13]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = $"Ilość\rkodów: {rowIndexer - 1} ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 2, row + 1, 13]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            worksheet.SetRowHeight(12.80, row);

            row++;

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 3]) {
                range.Merge = true;
                range.Value = $"EUR   {totalPrice} ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            worksheet.SetRowHeight(12.80, row);

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Arial";
            }

            worksheet = package.Workbook.Worksheets.Add("Ukrainian");

            worksheet.PrinterSettings.TopMargin = 0.3;
            worksheet.PrinterSettings.BottomMargin = 0.3;
            worksheet.PrinterSettings.RightMargin = 0.1;
            worksheet.PrinterSettings.LeftMargin = 0.1;
            worksheet.PrinterSettings.HeaderMargin = 0.3;
            worksheet.PrinterSettings.FooterMargin = 0.3;
            worksheet.PrinterSettings.FitToPage = true;

            worksheet.SetColumnWidth(4.88, 1);
            worksheet.SetColumnWidth(18.91, 2);
            worksheet.SetColumnWidth(30.24, 3);
            worksheet.SetColumnWidth(7.11, 4);
            worksheet.SetColumnWidth(8.81, 5);
            worksheet.SetColumnWidth(12.97, 6);
            worksheet.SetColumnWidth(16.01, 7);
            worksheet.SetColumnWidth(9.88, 8);
            worksheet.SetColumnWidth(7.81, 9);
            worksheet.SetColumnWidth(10.57, 10);
            worksheet.SetColumnWidth(10.57, 11);
            worksheet.SetColumnWidth(10.57, 12);
            worksheet.SetColumnWidth(12.87, 13);
            worksheet.SetColumnWidth(12.87, 14);
            worksheet.SetColumnWidth(0.61, 15);

            worksheet.SetRowHeight(12.60, new[] { 1, 2, 3, 4, 5, 6, 7 });

            using (ExcelRange range = worksheet.Cells[1, 2, 1, 14]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "Перемишль, {0}",
                        sadUk.FromDate.Year
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            using (ExcelRange range = worksheet.Cells[2, 1, 4, 14]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "СПЕЦИФІКАЦІЯ ДО РАХУНКУ-ФАКТУРИ № {0}/{1}/{2}",
                        string.Format(
                            "{0:D3}",
                            sadUk.Number.StartsWith("EX")
                                ? Convert.ToInt64(sadUk.Number.Substring(2, 10))
                                : Convert.ToInt64(sadUk.Number)
                        ),
                        string.Format("{0:D2}", sadUk.FromDate.Month),
                        sadUk.FromDate.Year
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 1, 7, 1]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "№ п/п";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 2, 7, 2]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Код товару";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 3, 7, 3]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Назва товару";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 4, 7, 4]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Од. вим";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 5, 7, 5]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "к-сть";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 6, 7, 6]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Країна походження";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 7, 7, 7]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Виробник";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 8, 7, 8]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Торгова марка";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 9, 7, 9]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Ціна";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 10, 7, 10]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Вартість EURO";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 11, 7, 11]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Вага нетто";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 12, 7, 12]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Вага брутто";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 13, 7, 13]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Митний код вже був прихід";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(252, 250, 18));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 14, 7, 14]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Можливий митний код";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 2, 7, 14]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row = 8;

            rowIndexer = 1;

            totalNetWeight = 0d;
            totalGrossWeight = 0d;
            totalPrice = 0m;

            if (isFromSale)
                foreach (Sale sale in sadUk.Sales)
                foreach (OrderItem item in sale.Order.OrderItems) {
                    double grossWeight = 0d;
                    double netWeight = 0d;
                    decimal currentUnitPrice = item.PricePerItem;

                    string countryCode = "";
                    string supplierName = "";
                    string supplierMark = "";

                    if (item.ProductLocations.Any()) {
                        ProductLocation location =
                            item
                                .ProductLocations
                                .First();

                        netWeight = Math.Round(location.ProductPlacement.PackingListPackageOrderItem.NetWeight * item.Qty, 3);
                        grossWeight = Math.Round(location.ProductPlacement.PackingListPackageOrderItem.GrossWeight * item.Qty, 3);

                        countryCode = location.ProductPlacement?.PackingListPackageOrderItem?.Supplier?.Country?.Code ?? "";

                        supplierName = location.ProductPlacement?.PackingListPackageOrderItem?.Supplier?.SupplierName ?? "";

                        supplierMark = location.ProductPlacement?.PackingListPackageOrderItem?.Supplier?.Brand;
                    }

                    decimal currentTotalPrice =
                        decimal.Round(
                            currentUnitPrice * Convert.ToDecimal(item.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    totalPrice =
                        decimal.Round(
                            totalPrice + currentTotalPrice,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    totalNetWeight =
                        Math.Round(totalNetWeight + netWeight, 3, MidpointRounding.AwayFromZero);

                    totalGrossWeight =
                        Math.Round(totalGrossWeight + grossWeight, 3, MidpointRounding.AwayFromZero);

                    using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                        range.Merge = true;
                        range.Value = rowIndexer;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Merge = true;
                        range.Value = item.Product.VendorCode;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                        range.Merge = true;
                        range.Value = item.Product.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                        range.Merge = true;
                        range.Value = item.Product.MeasureUnit.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                        range.Merge = true;
                        range.Value = item.Qty;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                        range.Merge = true;
                        range.Value = countryCode;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                        range.Merge = true;
                        range.Value = supplierName;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                        range.Merge = true;
                        range.Value = supplierMark;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                        range.Merge = true;
                        range.Value = currentUnitPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                        range.Merge = true;
                        range.Value = currentTotalPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                        range.Merge = true;
                        range.Value = netWeight;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.000";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                        range.Merge = true;
                        range.Value = grossWeight;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.000";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                        range.Merge = true;
                        range.Value = item.UkProductSpecification?.SpecificationCode ?? "";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(252, 250, 18));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                        range.Merge = true;
                        range.Value = item.ProductSpecification?.SpecificationCode ?? "";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    worksheet.SetRowHeight(12.80, row);

                    row++;

                    rowIndexer++;
                }
            else
                foreach (SadItem item in sadUk.SadItems) {
                    double grossWeight = 0d;

                    double netWeight = item.TotalNetWeight;
                    decimal currentUnitPrice = item.SupplyOrderUkraineCartItem.UnitPrice;

                    grossWeight =
                        sadUk.SadPallets.Any()
                            ? sadUk
                                .SadPallets
                                .SelectMany(pallet =>
                                    pallet
                                        .SadPalletItems
                                        .Where(i => i.SadItemId.Equals(item.Id)))
                                .Aggregate(grossWeight, (current, palletItem) =>
                                    Math.Round(current + palletItem.TotalGrossWeight, 3, MidpointRounding.AwayFromZero))
                            : item.TotalGrossWeight;

                    string countryCode = item.SupplyOrderUkraineCartItem?.Supplier?.Country?.Code ?? "";

                    string supplierName = item.SupplyOrderUkraineCartItem?.Supplier?.SupplierName ?? "";

                    string supplierMark = item.SupplyOrderUkraineCartItem?.Supplier?.Brand;

                    decimal currentTotalPrice =
                        decimal.Round(
                            currentUnitPrice * Convert.ToDecimal(item.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    totalPrice =
                        decimal.Round(
                            totalPrice + currentTotalPrice,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    totalNetWeight =
                        Math.Round(totalNetWeight + netWeight, 3, MidpointRounding.AwayFromZero);

                    totalGrossWeight =
                        Math.Round(totalGrossWeight + grossWeight, 3, MidpointRounding.AwayFromZero);

                    using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                        range.Merge = true;
                        range.Value = rowIndexer;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Merge = true;
                        range.Value = item.SupplyOrderUkraineCartItem.Product.VendorCode;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                        range.Merge = true;
                        range.Value = item.SupplyOrderUkraineCartItem.Product.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                        range.Merge = true;
                        range.Value = item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                        range.Merge = true;
                        range.Value = item.Qty;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                        range.Merge = true;
                        range.Value = countryCode;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                        range.Merge = true;
                        range.Value = supplierName;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                        range.Merge = true;
                        range.Value = supplierMark;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                        range.Merge = true;
                        range.Value = currentUnitPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                        range.Merge = true;
                        range.Value = currentTotalPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                        range.Merge = true;
                        range.Value = netWeight;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.000";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                        range.Merge = true;
                        range.Value = grossWeight;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.000";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                        range.Merge = true;
                        range.Value = item.UkProductSpecification?.SpecificationCode ?? "";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(252, 250, 18));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                        range.Merge = true;
                        range.Value = item.ProductSpecification?.SpecificationCode ?? "";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    worksheet.SetRowHeight(12.80, row);

                    row++;

                    rowIndexer++;
                }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                range.Merge = true;
                range.Value = sadUk.SadItems.Sum(s => s.Qty);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                range.Merge = true;
                range.Value = totalPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                range.Merge = true;
                range.Value = totalNetWeight;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.000";
            }

            using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                range.Merge = true;
                range.Value = totalGrossWeight > 0d ? totalGrossWeight : totalNetWeight;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.000";
            }

            worksheet.SetRowHeight(12.80, row);

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Arial";
            }

            //Setting document properties.
            package.Workbook.Properties.Title = "Sad specification";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportOldSadProductSpecification(
        string path,
        Sad sadPl,
        Sad sadUk,
        List<GroupedProductSpecification> plSpecifications,
        List<GroupedProductSpecification> ukSpecifications,
        bool isFromSale = false
    ) {
        string fileName = Path.Combine(path, $"SAD_Specification_{sadPl.Number}_{DateTime.Now.ToString("dd.MM.yyyy")}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Polish");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(0.87, 1);
            worksheet.SetColumnWidth(5.91, 2);
            worksheet.SetColumnWidth(40.24, 3);
            worksheet.SetColumnWidth(7.11, 4);
            worksheet.SetColumnWidth(8.81, 5);
            worksheet.SetColumnWidth(10.97, 6);
            worksheet.SetColumnWidth(12.01, 7);
            worksheet.SetColumnWidth(9.88, 8);
            worksheet.SetColumnWidth(12.87, 9);
            worksheet.SetColumnWidth(0.61, 10);

            worksheet.SetRowHeight(12.60, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 });

            //12.80

            using (ExcelRange range = worksheet.Cells[1, 2, 1, 3]) {
                range.Merge = true;
                range.Value = "Oryginał / Kopia";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 1, 9]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        ", {0}",
                        sadPl.FromDate.ToString("dd.MM.yyyy")
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            using (ExcelRange range = worksheet.Cells[2, 2, 4, 9]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "SPECYFIKACJA DO FAKTURY NR EX{0}/{1}/{2}",
                        string.Format(
                            "{0:D3}",
                            sadPl.Number.StartsWith("EX")
                                ? Convert.ToInt64(sadPl.Number.Substring(2, 10))
                                : Convert.ToInt64(sadPl.Number)
                        ),
                        string.Format("{0:D2}", sadPl.FromDate.Month),
                        sadPl.FromDate.Year
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[5, 2, 14, 3]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value =
                    "Sprzedawca: CONCORD.PL Sp z o.o.,\r37-700, Przemyśl, ul. Gen.Jakuba Jasińskiego 58,\rNIP 8133680920,\r 12193013182740072619290001,  Bank BPS S.A.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            string completeRecipient = "Nabywca: ";

            if (isFromSale) {
                if (sadPl.Sales.Any()) {
                    Client client = sadPl.Sales.First().ClientAgreement.Client;

                    if (!string.IsNullOrEmpty(client.FirstName) && !string.IsNullOrEmpty(client.LastName))
                        completeRecipient = $"{client.LastName} {client.FirstName} {client.MiddleName} ";
                    else
                        completeRecipient += client.FullName;

                    completeRecipient +=
                        string.IsNullOrEmpty(client.ActualAddress)
                            ? string.Empty
                            : client.ActualAddress + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.City)
                            ? string.Empty
                            : client.RegionCode.City + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.District)
                            ? string.Empty
                            : client.RegionCode.District + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.TIN)
                            ? string.Empty
                            : "\rNIP " + client.TIN;
                }
            } else {
                //if (sadPl.OrganizationClient != null) {
                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.FullName)
                //            ? string.Empty
                //            : sadPl.OrganizationClient.FullName + "  ";

                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.Address)
                //            ? string.Empty
                //            : sadPl.OrganizationClient.Address + ", ";

                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.City)
                //            ? string.Empty
                //            : sadPl.OrganizationClient.City + ", ";

                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.Country)
                //            ? string.Empty
                //            : sadPl.OrganizationClient.Country + ", ";

                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.NIP)
                //            ? string.Empty
                //            : "\rNIP " + sadPl.OrganizationClient.NIP;
                //}
                if (sadPl.Statham != null) {
                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.Statham.LastName)
                            ? string.Empty
                            : sadPl.Statham.LastName + "  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.Statham.FirstName)
                            ? string.Empty
                            : sadPl.Statham.FirstName + "  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.Statham.MiddleName)
                            ? string.Empty
                            : sadPl.Statham.MiddleName + "  ";

                    if (sadPl.StathamPassport != null) {
                        completeRecipient +=
                            string.IsNullOrEmpty(sadPl.StathamPassport.City)
                                ? string.Empty
                                : sadPl.StathamPassport.City + "  ";

                        completeRecipient +=
                            string.IsNullOrEmpty(sadPl.StathamPassport.Street)
                                ? string.Empty
                                : sadPl.StathamPassport.Street + "  ";

                        completeRecipient +=
                            string.IsNullOrEmpty(sadPl.StathamPassport.PassportSeria)
                                ? string.Empty
                                : sadPl.StathamPassport.PassportSeria + "  ";

                        completeRecipient +=
                            string.IsNullOrEmpty(sadPl.StathamPassport.PassportNumber)
                                ? string.Empty
                                : sadPl.StathamPassport.PassportNumber + "  ";
                    }
                }
            }

            using (ExcelRange range = worksheet.Cells[5, 4, 14, 9]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = completeRecipient;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            using (ExcelRange range = worksheet.Cells[15, 2, 17, 2]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Lp.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 3, 17, 3]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Nazwa towaru lub usługi";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 4, 17, 4]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Jednostka miary";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 5, 17, 5]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Ilość towaru / usługi";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 6, 17, 6]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Cena";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 7, 17, 7]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Wartość";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 8, 17, 8]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Waga netto";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 9, 17, 9]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Kod celny TARIC	";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 2, 17, 9]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            int row = 18;
            int rowIndexer = 1;

            double totalNetWeight = 0d;
            decimal totalPrice = 0m;

            foreach (GroupedProductSpecification specification in plSpecifications) {
                int startRow = row;
                int subRowIndexer = 1;

                double currentStepNetWeight = 0d;
                decimal currentStepTotalPrice = 0m;

                if (isFromSale)
                    foreach (OrderItem item in specification.OrderItems) {
                        double netWeight = 0d;

                        decimal currentUnitPrice = item.PricePerItem;

                        if (item.ProductLocations.Any()) {
                            ProductLocation location =
                                item
                                    .ProductLocations
                                    .First();

                            netWeight = Math.Round(location.ProductPlacement.PackingListPackageOrderItem.NetWeight * item.Qty, 2);
                        }

                        decimal currentTotalPrice =
                            decimal.Round(
                                currentUnitPrice * Convert.ToDecimal(item.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        currentStepTotalPrice =
                            decimal.Round(
                                currentStepTotalPrice + currentTotalPrice,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        currentStepNetWeight =
                            Math.Round(currentStepNetWeight + netWeight, 2);

                        using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                            range.Merge = true;
                            range.Value = $"{rowIndexer}.{subRowIndexer}";
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                            range.Merge = true;
                            range.Value =
                                string.Format(
                                    "{0} {1}",
                                    item.Product.VendorCode,
                                    item.Product.Name
                                );
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                            range.Merge = true;
                            range.Value = item.Product.MeasureUnit.Name;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                            range.Merge = true;
                            range.Value = item.Qty;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                            range.Merge = true;
                            range.Value = currentUnitPrice;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                            range.Merge = true;
                            range.Value = currentTotalPrice;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                            range.Merge = true;
                            range.Value = netWeight;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.000";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                            range.Merge = true;
                            range.Value = specification.SpecificationCode;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        worksheet.SetRowHeight(12.80, row);

                        row++;

                        subRowIndexer++;
                    }
                else
                    foreach (SadItem item in specification.SadItems) {
                        double netWeight;

                        decimal currentUnitPrice;

                        netWeight = item.TotalGrossWeight;
                        currentUnitPrice = item.SupplyOrderUkraineCartItem.UnitPrice;

                        decimal currentTotalPrice =
                            decimal.Round(
                                currentUnitPrice * Convert.ToDecimal(item.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        currentStepTotalPrice =
                            decimal.Round(
                                currentStepTotalPrice + currentTotalPrice,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        currentStepNetWeight =
                            Math.Round(currentStepNetWeight + netWeight, 3, MidpointRounding.AwayFromZero);

                        using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                            range.Merge = true;
                            range.Value = $"{rowIndexer}.{subRowIndexer}";
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                            range.Merge = true;
                            range.Value =
                                string.Format(
                                    "{0} {1}",
                                    item.SupplyOrderUkraineCartItem.Product.VendorCode,
                                    item.SupplyOrderUkraineCartItem.Product.Name
                                );
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                            range.Merge = true;
                            range.Value = item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                            range.Merge = true;
                            range.Value = item.Qty;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                            range.Merge = true;
                            range.Value = currentUnitPrice;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                            range.Merge = true;
                            range.Value = currentTotalPrice;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                            range.Merge = true;
                            range.Value = netWeight;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.000";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                            range.Merge = true;
                            range.Value = specification.SpecificationCode;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        worksheet.SetRowHeight(12.80, row);

                        row++;

                        subRowIndexer++;
                    }

                totalPrice =
                    decimal.Round(totalPrice + currentStepTotalPrice, 2, MidpointRounding.AwayFromZero);

                totalNetWeight = Math.Round(totalNetWeight + currentStepNetWeight, 3, MidpointRounding.AwayFromZero);

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Merge = true;
                    range.Value = isFromSale
                        ? specification.OrderItems.Sum(i => i.Qty)
                        : specification.SadItems.Sum(i => i.Qty);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                    range.Merge = true;
                    range.Value = currentStepTotalPrice;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                    range.Merge = true;
                    range.Value = currentStepNetWeight;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.000";
                }

                using (ExcelRange range = worksheet.Cells[startRow, 2, row, 9]) {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
                }

                worksheet.SetRowHeight(12.80, row);

                row++;

                rowIndexer++;
            }

            using (ExcelRange range = worksheet.Cells[row, 2, row + 1, 4]) {
                range.Merge = true;
                range.Value = "Ogółem:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row + 1, 5]) {
                range.Merge = true;
                range.Value = isFromSale
                    ? plSpecifications.Sum(s => s.OrderItems.Sum(i => i.Qty))
                    : plSpecifications.Sum(s => s.SadItems.Sum(i => i.Qty));
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row + 1, 7]) {
                range.Merge = true;
                range.Value = totalPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 8, row + 1, 8]) {
                range.Merge = true;
                range.Value = totalNetWeight;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.000";
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row + 1, 9]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = $"Ilość\rkodów: {rowIndexer - 1} ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 2, row + 1, 9]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            worksheet.SetRowHeight(12.80, row);

            row++;

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 3]) {
                range.Merge = true;
                range.Value = $"EUR   {totalPrice} ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            worksheet.SetRowHeight(12.80, row);

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Arial";
            }

            worksheet = package.Workbook.Worksheets.Add("Ukrainian");

            worksheet.PrinterSettings.TopMargin = 0.3;
            worksheet.PrinterSettings.BottomMargin = 0.3;
            worksheet.PrinterSettings.RightMargin = 0.1;
            worksheet.PrinterSettings.LeftMargin = 0.1;
            worksheet.PrinterSettings.HeaderMargin = 0.3;
            worksheet.PrinterSettings.FooterMargin = 0.3;
            worksheet.PrinterSettings.FitToPage = true;

            worksheet.SetColumnWidth(0.87, 1);
            worksheet.SetColumnWidth(5.91, 2);
            worksheet.SetColumnWidth(40.24, 3);
            worksheet.SetColumnWidth(7.11, 4);
            worksheet.SetColumnWidth(8.81, 5);
            worksheet.SetColumnWidth(10.97, 6);
            worksheet.SetColumnWidth(12.01, 7);
            worksheet.SetColumnWidth(9.88, 8);
            worksheet.SetColumnWidth(12.87, 9);
            worksheet.SetColumnWidth(0.61, 10);

            worksheet.SetRowHeight(12.60, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 });

            //12.80

            using (ExcelRange range = worksheet.Cells[1, 2, 1, 3]) {
                range.Merge = true;
                range.Value = "Оригінал / Копія";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 1, 9]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        ", {0}",
                        sadPl.FromDate.ToString("dd.MM.yyyy")
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            using (ExcelRange range = worksheet.Cells[2, 2, 4, 9]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "СПЕЦИФІКАЦІЯ ДО РАХУНКУ-ФАКТУРИ NR EX{0}/{1}/{2}",
                        string.Format(
                            "{0:D3}",
                            sadPl.Number.StartsWith("EX")
                                ? Convert.ToInt64(sadPl.Number.Substring(2, 10))
                                : Convert.ToInt64(sadPl.Number)
                        ),
                        string.Format("{0:D2}", sadPl.FromDate.Month),
                        sadPl.FromDate.Year
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[5, 2, 14, 3]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value =
                    "Sprzedawca: CONCORD.PL Sp z o.o.,\r37-700, Przemyśl, ul. Gen.Jakuba Jasińskiego 58,\rNIP 8133680920,\r 12193013182740072619290001,  Bank BPS S.A.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            completeRecipient = "Nabywca: ";

            if (isFromSale) {
                if (sadPl.Sales.Any()) {
                    Client client = sadPl.Sales.First().ClientAgreement.Client;

                    if (!string.IsNullOrEmpty(client.FirstName) && !string.IsNullOrEmpty(client.LastName))
                        completeRecipient = $"{client.LastName} {client.FirstName} {client.MiddleName} ";
                    else
                        completeRecipient += client.FullName;

                    completeRecipient +=
                        string.IsNullOrEmpty(client.ActualAddress)
                            ? string.Empty
                            : client.ActualAddress + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.City)
                            ? string.Empty
                            : client.RegionCode.City + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.District)
                            ? string.Empty
                            : client.RegionCode.District + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.TIN)
                            ? string.Empty
                            : "\rNIP " + client.TIN;
                }
            } else {
                //if (sadPl.OrganizationClient != null) {
                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.FullName)
                //            ? string.Empty
                //            : sadPl.OrganizationClient.FullName + "  ";

                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.Address)
                //            ? string.Empty
                //            : sadPl.OrganizationClient.Address + ", ";

                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.City)
                //            ? string.Empty
                //            : sadPl.OrganizationClient.City + ", ";

                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.Country)
                //            ? string.Empty
                //            : sadPl.OrganizationClient.Country + ", ";

                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.NIP)
                //            ? string.Empty
                //            : "\rNIP " + sadPl.OrganizationClient.NIP;
                //}
                if (sadPl.Statham != null) {
                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.Statham.LastName)
                            ? string.Empty
                            : sadPl.Statham.LastName + "  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.Statham.FirstName)
                            ? string.Empty
                            : sadPl.Statham.FirstName + "  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.Statham.MiddleName)
                            ? string.Empty
                            : sadPl.Statham.MiddleName + "  ";

                    if (sadPl.StathamPassport != null) {
                        completeRecipient +=
                            string.IsNullOrEmpty(sadPl.StathamPassport.City)
                                ? string.Empty
                                : sadPl.StathamPassport.City + "  ";

                        completeRecipient +=
                            string.IsNullOrEmpty(sadPl.StathamPassport.Street)
                                ? string.Empty
                                : sadPl.StathamPassport.Street + "  ";

                        completeRecipient +=
                            string.IsNullOrEmpty(sadPl.StathamPassport.PassportSeria)
                                ? string.Empty
                                : sadPl.StathamPassport.PassportSeria + "  ";

                        completeRecipient +=
                            string.IsNullOrEmpty(sadPl.StathamPassport.PassportNumber)
                                ? string.Empty
                                : sadPl.StathamPassport.PassportNumber + "  ";
                    }
                }
            }

            using (ExcelRange range = worksheet.Cells[5, 4, 14, 9]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = completeRecipient;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            using (ExcelRange range = worksheet.Cells[15, 2, 17, 2]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "№ п/п";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 3, 17, 3]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Назва товару або послуги";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 4, 17, 4]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Од. Виміру";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 5, 17, 5]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "к-сть товару/послуги";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 6, 17, 6]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Ціна EUR";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 7, 17, 7]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Вартість EUR";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 8, 17, 8]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Вага нетто";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 9, 17, 9]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = "Митний код	";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[15, 2, 17, 9]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            row = 18;
            rowIndexer = 1;

            totalNetWeight = 0d;
            totalPrice = 0m;

            foreach (GroupedProductSpecification specification in ukSpecifications) {
                int startRow = row;
                int subRowIndexer = 1;

                double currentStepNetWeight = 0d;
                decimal currentStepTotalPrice = 0m;

                if (isFromSale)
                    foreach (OrderItem item in specification.OrderItems) {
                        double netWeight = 0d;

                        decimal currentUnitPrice = item.PricePerItem;

                        if (item.ProductLocations.Any()) {
                            ProductLocation location =
                                item
                                    .ProductLocations
                                    .First();

                            netWeight = Math.Round(location.ProductPlacement.PackingListPackageOrderItem.NetWeight * item.Qty, 2);
                        }

                        decimal currentTotalPrice =
                            decimal.Round(
                                currentUnitPrice * Convert.ToDecimal(item.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        currentStepTotalPrice =
                            decimal.Round(
                                currentStepTotalPrice + currentTotalPrice,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        currentStepNetWeight =
                            Math.Round(currentStepNetWeight + netWeight, 2);

                        using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                            range.Merge = true;
                            range.Value = $"{rowIndexer}.{subRowIndexer}";
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                            range.Merge = true;
                            range.Value =
                                string.Format(
                                    "{0} {1}",
                                    item.Product.VendorCode,
                                    item.Product.Name
                                );
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                            range.Merge = true;
                            range.Value = item.Product.MeasureUnit.Name;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                            range.Merge = true;
                            range.Value = item.Qty;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                            range.Merge = true;
                            range.Value = currentUnitPrice;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                            range.Merge = true;
                            range.Value = currentTotalPrice;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                            range.Merge = true;
                            range.Value = netWeight;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.000";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                            range.Merge = true;
                            range.Value = specification.SpecificationCode;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        worksheet.SetRowHeight(12.80, row);

                        row++;

                        subRowIndexer++;
                    }
                else
                    foreach (SadItem item in specification.SadItems) {
                        double netWeight;

                        decimal currentUnitPrice;

                        netWeight = item.TotalGrossWeight;
                        currentUnitPrice = item.SupplyOrderUkraineCartItem.UnitPrice;

                        decimal currentTotalPrice =
                            decimal.Round(
                                currentUnitPrice * Convert.ToDecimal(item.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        currentStepTotalPrice =
                            decimal.Round(
                                currentStepTotalPrice + currentTotalPrice,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        currentStepNetWeight =
                            Math.Round(currentStepNetWeight + netWeight, 3, MidpointRounding.AwayFromZero);

                        using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                            range.Merge = true;
                            range.Value = $"{rowIndexer}.{subRowIndexer}";
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                            range.Merge = true;
                            range.Value =
                                string.Format(
                                    "{0} {1}",
                                    item.SupplyOrderUkraineCartItem.Product.VendorCode,
                                    item.SupplyOrderUkraineCartItem.Product.Name
                                );
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                            range.Merge = true;
                            range.Value = item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                            range.Merge = true;
                            range.Value = item.Qty;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                            range.Merge = true;
                            range.Value = currentUnitPrice;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                            range.Merge = true;
                            range.Value = currentTotalPrice;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                            range.Merge = true;
                            range.Value = netWeight;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.000";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                            range.Merge = true;
                            range.Value = specification.SpecificationCode;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        worksheet.SetRowHeight(12.80, row);

                        row++;

                        subRowIndexer++;
                    }

                totalPrice =
                    decimal.Round(totalPrice + currentStepTotalPrice, 2, MidpointRounding.AwayFromZero);

                totalNetWeight = Math.Round(totalNetWeight + currentStepNetWeight, 3, MidpointRounding.AwayFromZero);

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Merge = true;
                    range.Value = isFromSale
                        ? specification.OrderItems.Sum(i => i.Qty)
                        : specification.SadItems.Sum(i => i.Qty);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                    range.Merge = true;
                    range.Value = currentStepTotalPrice;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                    range.Merge = true;
                    range.Value = currentStepNetWeight;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.000";
                }

                using (ExcelRange range = worksheet.Cells[startRow, 2, row, 9]) {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
                }

                worksheet.SetRowHeight(12.80, row);

                row++;

                rowIndexer++;
            }

            using (ExcelRange range = worksheet.Cells[row, 2, row + 1, 4]) {
                range.Merge = true;
                range.Value = "Всього:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row + 1, 5]) {
                range.Merge = true;
                range.Value = isFromSale
                    ? plSpecifications.Sum(s => s.OrderItems.Sum(i => i.Qty))
                    : plSpecifications.Sum(s => s.SadItems.Sum(i => i.Qty));
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row + 1, 7]) {
                range.Merge = true;
                range.Value = totalPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 8, row + 1, 8]) {
                range.Merge = true;
                range.Value = totalNetWeight;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.000";
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row + 1, 9]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = $"к-сть\rкодів: {rowIndexer - 1} ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 2, row + 1, 9]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            worksheet.SetRowHeight(12.80, row);

            row++;

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 3]) {
                range.Merge = true;
                range.Value = $"EUR   {totalPrice} ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            worksheet.SetRowHeight(12.80, row);

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Arial";
            }

            //Setting document properties.
            package.Workbook.Properties.Title = "Sad specification";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportProductCapitalizationToXlsx(string path, ProductCapitalization productCapitalization) {
        string fileName = Path.Combine(path, $"ProductCapitalization_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        const string documentName = "Оприбуткування товарів";

        const string currency = "EUR";

        bool isValidData = productCapitalization?.Organization != null && productCapitalization.Storage != null;

        if (!isValidData) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("ProductCapitalization Document");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Setting default width to columns
            worksheet.SetColumnWidth(2.6, 1);
            worksheet.SetColumnWidth(3, 2);
            worksheet.SetColumnWidth(3, 3);
            worksheet.SetColumnWidth(3.14, 4);
            worksheet.SetColumnWidth(3.14, 5);
            worksheet.SetColumnWidth(3, 6);
            worksheet.SetColumnWidth(3, 7);
            worksheet.SetColumnWidth(3, 8);
            worksheet.SetColumnWidth(2.7143, 9);
            worksheet.SetColumnWidth(3, 10);
            worksheet.SetColumnWidth(3, 11);
            worksheet.SetColumnWidth(3, 12);
            worksheet.SetColumnWidth(3, 13);
            worksheet.SetColumnWidth(3, 14);
            worksheet.SetColumnWidth(3, 15);
            worksheet.SetColumnWidth(3.05, 16);
            worksheet.SetColumnWidth(1.57, 17);
            worksheet.SetColumnWidth(1.42, 18);
            worksheet.SetColumnWidth(3, 19);
            worksheet.SetColumnWidth(3, 20);
            worksheet.SetColumnWidth(2, 21);
            worksheet.SetColumnWidth(2, 22);
            worksheet.SetColumnWidth(3, 23);
            worksheet.SetColumnWidth(3, 24);
            worksheet.SetColumnWidth(3, 25);
            worksheet.SetColumnWidth(2, 26);
            worksheet.SetColumnWidth(2, 27);
            worksheet.SetColumnWidth(3.71, 28);
            worksheet.SetColumnWidth(3.71, 29);
            worksheet.SetColumnWidth(3.71, 30);
            worksheet.SetColumnWidth(3.92, 31);
            worksheet.SetColumnWidth(3.8, 32);
            worksheet.SetColumnWidth(3.8, 33);
            worksheet.SetColumnWidth(3.8, 34);

            //Document header

            //Setting document header height
            worksheet.SetRowHeight(11.3636, 1);
            worksheet.SetRowHeight(18.9394, 2);
            worksheet.SetRowHeight(11.3636, 3);
            worksheet.SetRowHeight(12.8788, 4);
            worksheet.SetRowHeight(12.1212, 5);
            worksheet.SetRowHeight(3.7879, 6);
            worksheet.SetRowHeight(12.8788, 7);
            worksheet.SetRowHeight(3.7879, 8);
            worksheet.SetRowHeight(12.8788, 9);
            worksheet.SetRowHeight(3.7879, 10);
            worksheet.SetRowHeight(11.3636, 11);
            worksheet.SetRowHeight(11.3636, 12);

            using (ExcelRange range = worksheet.Cells[2, 2, 2, 32]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.Font.Name = "Arial";
                range.Value = string.Format("{0} № {1} від {2} р.",
                    documentName, productCapitalization.Number, productCapitalization.FromDate.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("uk-UA")));
            }

            using (ExcelRange range = worksheet.Cells[2, 2, 2, 34]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[4, 2, 4, 5]) {
                range.Merge = true;
                range.Value = "Організація: ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[4, 6, 4, 33]) {
                range.Merge = true;
                range.Value = productCapitalization.Organization.Name;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[7, 2, 7, 5]) {
                range.Merge = true;
                range.Value = "Склад: ";
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[7, 6, 7, 33]) {
                range.Merge = true;
                range.Value = productCapitalization.Storage.Name;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[9, 2, 9, 5]) {
                range.Merge = true;
                range.Value = "Валюта: ";
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[9, 6, 9, 33]) {
                range.Merge = true;
                range.Value = currency;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[11, 2, 12, 3]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "№";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[11, 4, 12, 6]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Артикул";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[11, 7, 12, 22]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Товар";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[11, 23, 12, 27]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Кількість";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[11, 28, 12, 30]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Ціна";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[11, 31, 12, 34]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Сума";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[11, 2, 12, 34]) {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(238, 238, 238));
            }

            int row = 13;

            int counter = 1;

            foreach (ProductCapitalizationItem productCapitalizationItem in productCapitalization.ProductCapitalizationItems) {
                worksheet.SetRowHeight(11.3636, row);

                if (productCapitalization.ProductCapitalizationItems.Count != counter) {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 3]) {
                        range.Merge = true;
                        range.Value = counter;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 4, row, 6]) {
                        range.Merge = true;
                        range.Value = productCapitalizationItem.Product.VendorCode;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 7;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 7, row, 22]) {
                        range.Merge = true;
                        range.Value = productCapitalizationItem.Product.Name;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 23, row, 25]) {
                        range.Merge = true;
                        range.Value = productCapitalizationItem.Qty;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 26, row, 27]) {
                        range.Merge = true;
                        range.Value = productCapitalizationItem.Product.MeasureUnit.Name;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 28, row, 30]) {
                        range.Merge = true;
                        range.Value = productCapitalizationItem.UnitPrice;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 31, row, 34]) {
                        range.Merge = true;
                        range.Value = productCapitalizationItem.TotalAmount;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }
                } else {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 3]) {
                        range.Merge = true;
                        range.Value = counter;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 4, row, 6]) {
                        range.Merge = true;
                        range.Value = productCapitalizationItem.Product.VendorCode;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 7;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 7, row, 22]) {
                        range.Merge = true;
                        range.Value = productCapitalizationItem.Product.Name;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 23, row, 25]) {
                        range.Merge = true;
                        range.Value = productCapitalizationItem.Qty;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 26, row, 27]) {
                        range.Merge = true;
                        range.Value = productCapitalizationItem.Product.MeasureUnit.Name;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 28, row, 30]) {
                        range.Merge = true;
                        range.Value = productCapitalizationItem.UnitPrice;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 31, row, 34]) {
                        range.Merge = true;
                        range.Value = productCapitalizationItem.TotalAmount;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }
                }

                counter++;
                row++;
            }

            worksheet.SetRowHeight(6.0606, row);

            worksheet.SetRowHeight(12.8788, row + 1);
            worksheet.SetRowHeight(6.8182, row + 2);
            worksheet.SetRowHeight(12.8788, row + 3);
            worksheet.SetRowHeight(12.8788, row + 4);
            worksheet.SetRowHeight(6.8182, row + 5);
            worksheet.SetRowHeight(11.3636, row + 6);
            worksheet.SetRowHeight(12.8788, row + 7);
            worksheet.SetRowHeight(11.3636, row + 8);
            worksheet.SetRowHeight(11.3636, row + 9);
            worksheet.SetRowHeight(12.8788, row + 10);
            worksheet.SetRowHeight(11.3636, row + 11);
            worksheet.SetRowHeight(11.3636, row + 12);
            worksheet.SetRowHeight(11.3636, row + 13);
            worksheet.SetRowHeight(11.3636, row + 14);
            worksheet.SetRowHeight(11.3636, row + 15);
            worksheet.SetRowHeight(11.3636, row + 16);

            using (ExcelRange range = worksheet.Cells[row + 1, 30, row + 1, 30]) {
                range.Style.Font.Name = "Arial";
                range.Value = "Разом:";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row + 1, 31, row + 1, 34]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Value = productCapitalization.TotalAmount;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row + 3, 2, row + 3, 33]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
                range.Value = string.Format("Всього найменувань {0}, на суму {1} {2}",
                    productCapitalization.ProductCapitalizationItems.Count, productCapitalization.TotalAmount, currency);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 4, 2, row + 4, 32]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Value = productCapitalization.TotalAmount.ToCompleteText(currency, false, true, true);
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row + 5, 2, row + 5, 34]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[row + 7, 2, row + 7, 9]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Голова комісії: ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 7, 10, row + 7, 17]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 8, 10, row + 8, 17]) {
                range.Merge = true;
                range.Value = "(підпис)";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 6;
            }

            using (ExcelRange range = worksheet.Cells[row + 10, 2, row + 10, 9]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Члени комісії: ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 10, 10, row + 10, 17]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }


            using (ExcelRange range = worksheet.Cells[row + 11, 10, row + 11, 17]) {
                range.Merge = true;
                range.Value = "(підпис)";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 6;
            }

            using (ExcelRange range = worksheet.Cells[row + 12, 10, row + 12, 17]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }


            using (ExcelRange range = worksheet.Cells[row + 13, 10, row + 13, 17]) {
                range.Merge = true;
                range.Value = "(підпис)";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 6;
            }


            using (ExcelRange range = worksheet.Cells[row + 14, 10, row + 14, 17]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }


            using (ExcelRange range = worksheet.Cells[row + 15, 10, row + 15, 17]) {
                range.Merge = true;
                range.Value = "(підпис)";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 6;
            }

            package.Workbook.Properties.Title = "Product Capitalization";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportProductTransferToXlsx(string path, ProductTransfer productTransfer) {
        string fileName = Path.Combine(path, $"ProductTransfer_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        const string documentName = "Накладна на переміщення";

        const string informationAboutOrganization = "Не є платником податку на прибуток на загальних підставах";

        bool isValidData = productTransfer.Organization != null &&
                           productTransfer.FromStorage != null &&
                           productTransfer.ToStorage != null;

        if (!isValidData) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("ProductTransfer Document");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Setting default width to columns
            worksheet.SetColumnWidth(1.7143, 1);
            worksheet.SetColumnWidth(3.1428, 2);
            worksheet.SetColumnWidth(2.5714, 3);
            worksheet.SetColumnWidth(3.8571, 4);
            worksheet.SetColumnWidth(3.8571, 5);
            worksheet.SetColumnWidth(3.7142, 6);
            worksheet.SetColumnWidth(3, 7);
            worksheet.SetColumnWidth(2.1428, 8);
            worksheet.SetColumnWidth(3.1428, 9);
            worksheet.SetColumnWidth(3, 10);
            worksheet.SetColumnWidth(3, 11);
            worksheet.SetColumnWidth(3, 12);
            worksheet.SetColumnWidth(3, 13);
            worksheet.SetColumnWidth(2.4286, 14);
            worksheet.SetColumnWidth(2.4285, 15);
            worksheet.SetColumnWidth(2.4285, 16);
            worksheet.SetColumnWidth(3, 17);
            worksheet.SetColumnWidth(3, 18);
            worksheet.SetColumnWidth(2.4285, 19);
            worksheet.SetColumnWidth(2.4285, 20);
            worksheet.SetColumnWidth(2.4285, 21);
            worksheet.SetColumnWidth(3, 22);
            worksheet.SetColumnWidth(3, 23);
            worksheet.SetColumnWidth(3, 24);
            worksheet.SetColumnWidth(3, 25);
            worksheet.SetColumnWidth(3, 26);
            worksheet.SetColumnWidth(3, 27);
            worksheet.SetColumnWidth(3, 28);
            worksheet.SetColumnWidth(2.7142, 29);
            worksheet.SetColumnWidth(3.1428, 30);
            worksheet.SetColumnWidth(3, 31);
            worksheet.SetColumnWidth(3, 32);
            worksheet.SetColumnWidth(3, 33);
            worksheet.SetColumnWidth(3, 34);

            //Document header

            //Setting document header height
            worksheet.SetRowHeight(10.6060, 1);
            worksheet.SetRowHeight(21.2121, 2);
            worksheet.SetRowHeight(11.3636, 3);
            worksheet.SetRowHeight(12.8788, 4);
            worksheet.SetRowHeight(11.3636, 5);
            worksheet.SetRowHeight(3.7879, 6);
            worksheet.SetRowHeight(12.8788, 7);
            worksheet.SetRowHeight(11.3636, 8);
            worksheet.SetRowHeight(3.7879, 9);
            worksheet.SetRowHeight(12.1212, 10);
            worksheet.SetRowHeight(11.3636, 11);
            worksheet.SetRowHeight(3.7879, 12);
            worksheet.SetRowHeight(11.3636, 13);
            worksheet.SetRowHeight(11.3636, 14);

            using (ExcelRange range = worksheet.Cells[2, 2, 2, 34]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.Font.Name = "Arial";
                range.Value = string.Format("{0} № {1} від {2} р.",
                    documentName, productTransfer.Number, productTransfer.FromDate.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("uk-UA")));
            }

            using (ExcelRange range = worksheet.Cells[2, 2, 2, 34]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[4, 2, 4, 6]) {
                range.Merge = true;
                range.Value = "Організація: ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[4, 7, 4, 34]) {
                range.Merge = true;
                range.Value = productTransfer.Organization.Name;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[5, 8, 5, 34]) {
                range.Merge = true;
                range.Value = informationAboutOrganization;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[7, 2, 7, 6]) {
                range.Merge = true;
                range.Value = "Відправник: ";
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[7, 7, 7, 34]) {
                range.Merge = true;
                range.Value = productTransfer.FromStorage.Name;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[10, 2, 10, 6]) {
                range.Merge = true;
                range.Value = "Отримувач: ";
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[10, 7, 10, 34]) {
                range.Merge = true;
                range.Value = productTransfer.ToStorage.Name;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[13, 2, 14, 3]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "№";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[13, 4, 14, 6]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Артикул";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[13, 7, 14, 26]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Товар";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[13, 27, 14, 31]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Кількість";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[13, 2, 14, 31]) {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(238, 238, 238));
            }

            int row = 15;

            int counter = 1;

            foreach (ProductTransferItem productTransferItem in productTransfer.ProductTransferItems) {
                worksheet.SetRowHeight(11.3636, row);

                if (productTransfer.ProductTransferItems.Count != counter) {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 3]) {
                        range.Merge = true;
                        range.Value = counter;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 4, row, 6]) {
                        range.Merge = true;
                        range.Value = productTransferItem.Product.VendorCode;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 7;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 7, row, 26]) {
                        range.Merge = true;
                        range.Value = productTransferItem.Product.Name;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                        range.Merge = true;
                        range.Value = productTransferItem.Qty;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                        range.Merge = true;
                        range.Value = productTransferItem.Product.MeasureUnit.Name;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }
                } else {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 3]) {
                        range.Merge = true;
                        range.Value = counter;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 4, row, 6]) {
                        range.Merge = true;
                        range.Value = productTransferItem.Product.VendorCode;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 7;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 7, row, 26]) {
                        range.Merge = true;
                        range.Value = productTransferItem.Product.Name;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                        range.Merge = true;
                        range.Value = productTransferItem.Qty;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                        range.Merge = true;
                        range.Value = productTransferItem.Product.MeasureUnit.Name;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }
                }

                counter++;
                row++;
            }

            worksheet.SetRowHeight(11.3636, row);
            worksheet.SetRowHeight(9.0909, row + 1);
            worksheet.SetRowHeight(11.3636, row + 2);
            worksheet.SetRowHeight(12.8788, row + 3);
            worksheet.SetRowHeight(11.3636, row + 4);
            worksheet.SetRowHeight(11.3636, row + 5);

            using (ExcelRange range = worksheet.Cells[row + 1, 2, row + 1, 34]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[row + 3, 2, row + 3, 7]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Value = "Відвантажив(ла): ";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 3, 8, row + 3, 16]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 3, 18, row + 3, 22]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Value = "Отримав(ла): ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 3, 23, row + 3, 33]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            package.Workbook.Properties.Title = "Product Transfer Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportProductIncomeDocumentToXlsx(string path, ProductIncome productIncome) {
        string fileName = Path.Combine(path, $"ProductIncome_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        bool isValidRetrieveData = productIncome?.ProductIncomeItems != null && productIncome.ProductIncomeItems.Any();

        const string documentName = "Прибуткова накладна";

        decimal totalAmount = productIncome?.TotalNetPrice ?? 0m;

        string supplierFullName = string.Empty;

        string organizationFullName = string.Empty;

        string agreementName = string.Empty;

        string currencyCode = string.Empty;

        if (isValidRetrieveData) {
            ProductIncomeItem tempProductIncomeItem = productIncome.ProductIncomeItems.First();

            switch (productIncome.ProductIncomeType) {
                case ProductIncomeType.SaleReturn:
                    supplierFullName = tempProductIncomeItem.SaleReturnItem?.SaleReturn?.Client?.FullName ?? "";
                    organizationFullName =
                        tempProductIncomeItem.SaleReturnItem?.OrderItem.Order?.Sale.ClientAgreement?.Agreement?.Organization?.FullName
                        ?? tempProductIncomeItem.SaleReturnItem?.OrderItem.Order?.Sale.ClientAgreement?.Agreement?.Organization?.Name
                        ?? "";
                    agreementName = tempProductIncomeItem.SaleReturnItem?.OrderItem?.Order?.Sale?.ClientAgreement?.Agreement?.Name ?? "";
                    currencyCode = tempProductIncomeItem.SaleReturnItem?.OrderItem?.Order?.Sale?.ClientAgreement?.Agreement?.Currency?.Code ?? "";
                    break;
                case ProductIncomeType.Capitalization:
                    supplierFullName = "";
                    organizationFullName =
                        tempProductIncomeItem.ProductCapitalizationItem?.ProductCapitalization?.Organization?.FullName
                        ?? tempProductIncomeItem.ProductCapitalizationItem?.ProductCapitalization?.Organization?.Name
                        ?? "";
                    agreementName = "";
                    currencyCode = "EUR";
                    break;
                case ProductIncomeType.IncomePl:
                case ProductIncomeType.IncomeUk: {
                    if (tempProductIncomeItem.ActReconciliationItem != null) {
                        if (tempProductIncomeItem.ActReconciliationItem.ActReconciliation?.SupplyOrderUkraine != null) {
                            supplierFullName = tempProductIncomeItem.ActReconciliationItem.ActReconciliation.SupplyOrderUkraine.ClientAgreement?.Client?.FullName ?? "";
                            organizationFullName =
                                tempProductIncomeItem.ActReconciliationItem.ActReconciliation.SupplyOrderUkraine.Organization?.FullName
                                ?? tempProductIncomeItem.ActReconciliationItem.ActReconciliation.SupplyOrderUkraine.Organization?.Name
                                ?? "";
                            agreementName = tempProductIncomeItem.ActReconciliationItem.ActReconciliation.SupplyOrderUkraine.ClientAgreement?.Agreement?.Name ?? "";
                            currencyCode = tempProductIncomeItem.ActReconciliationItem.ActReconciliation.SupplyOrderUkraine.ClientAgreement?.Agreement?.Currency?.Code ?? "";
                        } else if (tempProductIncomeItem.ActReconciliationItem.ActReconciliation?.SupplyInvoice?.SupplyOrder != null) {
                            supplierFullName = tempProductIncomeItem.ActReconciliationItem.ActReconciliation.SupplyInvoice.SupplyOrder.ClientAgreement?.Client?.FullName ?? "";
                            organizationFullName = tempProductIncomeItem.ActReconciliationItem.ActReconciliation.SupplyInvoice.SupplyOrder.Organization?.FullName
                                                   ?? tempProductIncomeItem.ActReconciliationItem.ActReconciliation.SupplyInvoice.SupplyOrder.Organization?.Name
                                                   ?? "";
                            agreementName = tempProductIncomeItem.ActReconciliationItem.ActReconciliation.SupplyInvoice.SupplyOrder.ClientAgreement?.Agreement?.Name ?? "";
                            currencyCode =
                                tempProductIncomeItem.ActReconciliationItem.ActReconciliation.SupplyInvoice.SupplyOrder.ClientAgreement?.Agreement?.Currency?.Code ??
                                "";
                        }
                    } else if (tempProductIncomeItem.SupplyOrderUkraineItem != null) {
                        supplierFullName = tempProductIncomeItem.SupplyOrderUkraineItem.SupplyOrderUkraine?.ClientAgreement?.Client?.FullName ?? "";
                        organizationFullName = tempProductIncomeItem.SupplyOrderUkraineItem.SupplyOrderUkraine?.Organization?.FullName
                                               ?? tempProductIncomeItem.SupplyOrderUkraineItem.SupplyOrderUkraine?.Organization?.Name
                                               ?? "";
                        agreementName = tempProductIncomeItem.SupplyOrderUkraineItem.SupplyOrderUkraine?.ClientAgreement?.Agreement?.Name ?? "";
                        currencyCode = tempProductIncomeItem.SupplyOrderUkraineItem.SupplyOrderUkraine?.ClientAgreement?.Agreement?.Currency?.Code ?? "";
                    } else if (tempProductIncomeItem.PackingListPackageOrderItem != null) {
                        supplierFullName = tempProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem?.SupplyOrderItem?.SupplyOrder?.Client?.FullName ?? "";
                        organizationFullName =
                            tempProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem?.SupplyOrderItem?.SupplyOrder?.Organization?.FullName
                            ?? tempProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem?.SupplyOrderItem?.SupplyOrder?.Organization?.Name
                            ?? "";
                        agreementName =
                            tempProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem?.SupplyOrderItem?.SupplyOrder?.ClientAgreement?.Agreement?.Name ??
                            "";
                        currencyCode =
                            tempProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem?.SupplyOrderItem?.SupplyOrder?.ClientAgreement?.Agreement
                                ?.Currency
                                ?.Code ?? "";
                    }

                    break;
                }
            }
        }

        if (!isValidRetrieveData) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("ProductIncome Document");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Setting default width to columns
            worksheet.SetColumnWidth(3, 1);
            worksheet.SetColumnWidth(2, 2);
            worksheet.SetColumnWidth(1.2857, 3);
            worksheet.SetColumnWidth(3.7143, 4);
            worksheet.SetColumnWidth(3.2857, 5);
            worksheet.SetColumnWidth(3.2857, 6);
            worksheet.SetColumnWidth(3, 7);
            worksheet.SetColumnWidth(3, 8);
            worksheet.SetColumnWidth(2.1428, 9);
            worksheet.SetColumnWidth(2.5714, 10);
            worksheet.SetColumnWidth(3, 11);
            worksheet.SetColumnWidth(3, 12);
            worksheet.SetColumnWidth(3, 13);
            worksheet.SetColumnWidth(2.5714, 14);
            worksheet.SetColumnWidth(2.5714, 15);
            worksheet.SetColumnWidth(3, 16);
            worksheet.SetColumnWidth(3, 17);
            worksheet.SetColumnWidth(3, 18);
            worksheet.SetColumnWidth(3, 19);
            worksheet.SetColumnWidth(2, 20);
            worksheet.SetColumnWidth(2, 21);
            worksheet.SetColumnWidth(3, 22);
            worksheet.SetColumnWidth(3, 23);
            worksheet.SetColumnWidth(3, 24);
            worksheet.SetColumnWidth(2, 25);
            worksheet.SetColumnWidth(2, 26);
            worksheet.SetColumnWidth(3.8571, 27);
            worksheet.SetColumnWidth(3.8571, 28);
            worksheet.SetColumnWidth(3.8571, 29);
            worksheet.SetColumnWidth(3.8571, 30);
            worksheet.SetColumnWidth(3.8571, 31);
            worksheet.SetColumnWidth(3.8571, 32);
            worksheet.SetColumnWidth(3.8571, 33);

            //Document header

            //Setting document header height
            worksheet.SetRowHeight(11.3636, 1);
            worksheet.SetRowHeight(0.7575, 2);
            worksheet.SetRowHeight(21.2121, 3);
            worksheet.SetRowHeight(11.3636, 4);
            worksheet.SetRowHeight(12.8788, 5);
            worksheet.SetRowHeight(11.3636, 6);
            worksheet.SetRowHeight(6.8182, 7);
            worksheet.SetRowHeight(12.8788, 8);
            worksheet.SetRowHeight(11.3636, 9);
            worksheet.SetRowHeight(6.8182, 10);
            worksheet.SetRowHeight(12.8788, 11);
            worksheet.SetRowHeight(3.7878, 12);
            worksheet.SetRowHeight(11.3636, 13);
            worksheet.SetRowHeight(11.3636, 14);

            using (ExcelRange range = worksheet.Cells[3, 2, 3, 33]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.Font.Name = "Arial";
                range.Value = string.Format("{0} № {1} від {2} р.",
                    documentName, productIncome.Number,
                    productIncome.FromDate.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("uk-UA")));
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[5, 2, 5, 6]) {
                range.Merge = true;
                range.Value = "Постачальник: ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[5, 7, 5, 33]) {
                range.Merge = true;
                range.Value = supplierFullName;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[8, 2, 8, 6]) {
                range.Merge = true;
                range.Value = "Покупець: ";
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[8, 7, 8, 33]) {
                range.Merge = true;
                range.Value = organizationFullName;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[11, 2, 11, 6]) {
                range.Merge = true;
                range.Value = "Договір: ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[11, 7, 11, 33]) {
                range.Merge = true;
                range.Value = agreementName;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[13, 2, 14, 3]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "№";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[13, 4, 14, 6]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Артикул";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[13, 7, 14, 21]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Товар";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[13, 22, 14, 26]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Кількість";
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[13, 27, 14, 29]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Ціна";
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[13, 30, 14, 33]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Сума";
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[13, 2, 14, 33]) {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(238, 238, 238));
            }

            int row = 15;

            int counter = 1;

            foreach (ProductIncomeItem productIncomeItem in productIncome.ProductIncomeItems) {
                worksheet.SetRowHeight(12.12121, row);

                string vendorCode = string.Empty;

                string productName = string.Empty;

                decimal unitPrice = 0;

                decimal totalPrice = 0;

                string unitOfMeasurementName = string.Empty;

                double qty = productIncomeItem.Qty;

                switch (productIncome.ProductIncomeType) {
                    case ProductIncomeType.SaleReturn when productIncomeItem.SaleReturnItem?.Id != null:
                        vendorCode = productIncomeItem.SaleReturnItem.OrderItem?.Product?.VendorCode ?? "";
                        productName = productIncomeItem.SaleReturnItem.OrderItem?.Product?.Name ?? "";
                        unitPrice = productIncomeItem.SaleReturnItem.OrderItem?.PricePerItem ?? 0;
                        totalPrice = productIncomeItem.SaleReturnItem.OrderItem?.TotalAmount ?? 0;
                        unitOfMeasurementName = productIncomeItem.SaleReturnItem.OrderItem?.Product?.MeasureUnit?.Name ?? "";
                        break;
                    case ProductIncomeType.Capitalization when productIncomeItem.ProductCapitalizationItem?.Id != null:
                        vendorCode = productIncomeItem.ProductCapitalizationItem.Product?.VendorCode ?? "";
                        productName = productIncomeItem.ProductCapitalizationItem.Product?.Name ?? "";
                        unitPrice = productIncomeItem.ProductCapitalizationItem.UnitPrice;
                        totalPrice = productIncomeItem.ProductCapitalizationItem.TotalAmount;
                        unitOfMeasurementName = productIncomeItem.ProductCapitalizationItem.Product?.MeasureUnit?.Name ?? "";
                        break;
                    case ProductIncomeType.IncomePl:
                    case ProductIncomeType.IncomeUk: {
                        if (productIncomeItem.ActReconciliationItem != null) {
                            vendorCode = productIncomeItem.ActReconciliationItem.Product?.VendorCode ?? "";
                            productName = productIncomeItem.ActReconciliationItem.Product?.Name ?? "";
                            unitPrice = productIncomeItem.ActReconciliationItem.UnitPrice;
                            totalPrice =
                                decimal.Round(
                                    productIncomeItem.ActReconciliationItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty),
                                    2,
                                    MidpointRounding.AwayFromZero
                                );
                            unitOfMeasurementName = productIncomeItem.ActReconciliationItem.Product?.MeasureUnit?.Name ?? "";
                        } else if (productIncomeItem.SupplyOrderUkraineItem != null) {
                            vendorCode = productIncomeItem.SupplyOrderUkraineItem.Product?.VendorCode ?? "";
                            productName = productIncomeItem.SupplyOrderUkraineItem.Product?.Name ?? "";
                            unitPrice = productIncomeItem.SupplyOrderUkraineItem.UnitPrice;
                            totalPrice = productIncomeItem.SupplyOrderUkraineItem.NetPrice;
                            unitOfMeasurementName = productIncomeItem.SupplyOrderUkraineItem.Product?.MeasureUnit?.Name ?? "";
                        } else if (productIncomeItem.PackingListPackageOrderItem != null) {
                            vendorCode = productIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem?.SupplyOrderItem?.Product?.VendorCode ?? "";
                            productName = productIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem?.SupplyOrderItem?.Product?.Name ?? "";
                            unitPrice = productIncomeItem.PackingListPackageOrderItem.UnitPrice;
                            totalPrice = productIncomeItem.PackingListPackageOrderItem.TotalNetPrice;
                            unitOfMeasurementName = productIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem?.SupplyOrderItem?.Product?.MeasureUnit?.Name ?? "";
                        }

                        break;
                    }
                }

                if (productIncome.ProductIncomeItems.Count != counter) {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 3]) {
                        range.Merge = true;
                        range.Value = counter;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 4, row, 6]) {
                        range.Merge = true;
                        range.Value = vendorCode;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 7;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 7, row, 21]) {
                        range.Merge = true;
                        range.Value = productName;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 22, row, 24]) {
                        range.Merge = true;
                        range.Value = qty;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 25, row, 26]) {
                        range.Merge = true;
                        range.Value = unitOfMeasurementName;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                        range.Merge = true;
                        range.Value = unitPrice;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 33]) {
                        range.Merge = true;
                        range.Value = totalPrice;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }
                } else {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 3]) {
                        range.Merge = true;
                        range.Value = counter;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 4, row, 6]) {
                        range.Merge = true;
                        range.Value = vendorCode;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 7;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 7, row, 21]) {
                        range.Merge = true;
                        range.Value = productName;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 22, row, 24]) {
                        range.Merge = true;
                        range.Value = qty;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 25, row, 26]) {
                        range.Merge = true;
                        range.Value = unitOfMeasurementName;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                        range.Merge = true;
                        range.Value = unitPrice;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 33]) {
                        range.Merge = true;
                        range.Value = totalPrice;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }
                }

                counter++;
                row++;
            }

            worksheet.SetRowHeight(6.8182, row);
            worksheet.SetRowHeight(12.1212, row + 1);
            worksheet.SetRowHeight(6.0606, row + 2);
            worksheet.SetRowHeight(11.3636, row + 3);
            worksheet.SetRowHeight(12.8788, row + 4);
            worksheet.SetRowHeight(6.8182, row + 5);
            worksheet.SetRowHeight(11.3636, row + 6);
            worksheet.SetRowHeight(12.8788, row + 7);
            worksheet.SetRowHeight(17.4242, row + 8);

            using (ExcelRange range = worksheet.Cells[row + 1, 29, row + 1, 29]) {
                range.Style.Font.Name = "Arial";
                range.Value = "Разом:";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row + 1, 30, row + 1, 33]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Value = totalAmount;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row + 3, 2, row + 3, 33]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Value =
                    string.Format(
                        "Всього найменувань {0}, на суму {1} {2}.",
                        productIncome.ProductIncomeItems.Count,
                        $"{totalAmount:0,0.00}",
                        currencyCode
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 4, 2, row + 4, 33]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Value = totalAmount.ToCompleteText(currencyCode, false, true, true);
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row + 5, 2, row + 5, 33]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[row + 7, 2, row + 7, 2]) {
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Відвантажив ";
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 7, 7, row + 7, 9]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 7, 10, row + 7, 16]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 7, 18, row + 7, 18]) {
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Отримав ";
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 7, 22, row + 7, 24]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 7, 25, row + 7, 33]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            package.Workbook.Properties.Title = "Product Income Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportAllProductsByStorageToXlsx(string path, List<ProductAvailability> productAvailabilities) {
        string fileName = Path.Combine(path, $"{"ProductAvailabilityByStorage"}_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        bool isValidRetrieveData = productAvailabilities != null;

        if (!isValidRetrieveData) return SaveFiles(fileName);

        double totalQty = 0;

        string storageName = productAvailabilities.FirstOrDefault()?.Storage?.Name ?? "";

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("ProductAvailabilityByStorage Document");

            worksheet.PrinterSettings.FitToPage = true;
            worksheet.PrinterSettings.FitToWidth = 1;
            worksheet.PrinterSettings.FitToHeight = 0;
            worksheet.PrinterSettings.Orientation = eOrientation.Portrait;

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(4, 1);
            worksheet.SetColumnWidth(23, 2);
            worksheet.SetColumnWidth(37, 3);
            worksheet.SetColumnWidth(10, 4);
            worksheet.SetColumnWidth(10, 5);
            worksheet.SetColumnWidth(10, 6);
            worksheet.SetColumnWidth(15, 7);
            worksheet.SetColumnWidth(15, 8);

            worksheet.SetRowHeight(11, 1);
            worksheet.SetRowHeight(38, 2);
            worksheet.SetRowHeight(17.45, 3);
            worksheet.SetRowHeight(14.5, 4);

            using (ExcelRange range = worksheet.Cells[2, 2, 2, 8]) {
                range.Merge = true;
                range.Value = string.Format("Наявність Товарів На Складі \"{0}\"", storageName);
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 14;
                range.Style.Font.Name = "Arial";
            }

            using (ExcelRange range = worksheet.Cells[3, 1, 4, 1]) {
                range.Merge = true;
                range.Value = "№";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 2, 4, 2]) {
                range.Merge = true;
                range.Value = "Код";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 3, 4, 3]) {
                range.Merge = true;
                range.Value = "Назва";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 4, 3, 7]) {
                range.Merge = true;
                range.Value = "Розміщення";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[4, 7, 4, 7]) {
                range.Merge = true;
                range.Value = "Кількість";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[4, 4, 4, 4]) {
                range.Value = "Склад";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[4, 5, 4, 5]) {
                range.Value = "Ряд";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[4, 6, 4, 6]) {
                range.Value = "Полиця";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 8, 4, 8]) {
                range.Merge = true;
                range.Value = "Загальна кількість";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 1, 4, 8]) {
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            int row = 5;

            int counter = 1;

            foreach (ProductAvailability productAvailability in productAvailabilities) {
                worksheet.SetRowHeight(14.5, row);

                int nextRow;
                if (productAvailability.Product.ProductPlacements.Count == 0 ||
                    productAvailability.Product.ProductPlacements.Count == 1)
                    nextRow = row;
                else
                    nextRow = row + productAvailability.Product.ProductPlacements.Count - 1;

                using (ExcelRange range = worksheet.Cells[row, 1, nextRow, 1]) {
                    range.Merge = true;
                    range.Value = counter;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 2, nextRow, 2]) {
                    range.Merge = true;
                    range.Value = productAvailability.Product.VendorCode;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 3, nextRow, 3]) {
                    range.Merge = true;
                    range.Value = productAvailability.Product.Name;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 8, nextRow, 8]) {
                    totalQty += productAvailability.Amount;
                    range.Merge = true;
                    range.Value = productAvailability.Amount;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0";
                }

                if (row.Equals(nextRow)) {
                    using (ExcelRange range = worksheet.Cells[row, 4, nextRow, 4]) {
                        range.Merge = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 5, nextRow, 5]) {
                        range.Merge = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 6, nextRow, 6]) {
                        range.Merge = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 7, nextRow, 7]) {
                        range.Merge = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }
                }

                using (ExcelRange range = worksheet.Cells[row, 1, nextRow, 8]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Style.Numberformat.Format = "0";
                }

                if (productAvailability.Product.ProductPlacements.Any())
                    foreach (ProductPlacement productPlacement in productAvailability.Product.ProductPlacements) {
                        using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                            range.Value = productPlacement.StorageNumber.Equals("N") ? string.Empty : productPlacement.StorageNumber;

                            range.Style.Numberformat.Format = "0";
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                            range.Value = productPlacement.RowNumber ?? "";
                            if (double.TryParse(productPlacement.RowNumber, out double rowNumber))
                                range.Value = rowNumber;
                            else
                                range.Value = "";

                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                            range.Value = productPlacement.CellNumber ?? "";
                            if (double.TryParse(productPlacement.CellNumber, out double cellNumber))
                                range.Value = cellNumber;
                            else
                                range.Value = "";

                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                            range.Value = productPlacement.Qty;
                            range.Style.Numberformat.Format = "0";
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        row++;
                    }
                else
                    row++;

                counter++;
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Разом:";
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = totalQty;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Style.Numberformat.Format = "0";
            }

            package.Workbook.Properties.Title = "Product Availability By Storage Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string, string) ExportAllConsignmentAvailabilityFilteredToXlsx(
        string path,
        IEnumerable<ConsignmentAvailabilityItem> availabilities) {
        string fileName = Path.Combine(path, $"{"ProductAvailabilityByStorage"}_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        string storageName = availabilities.FirstOrDefault()?.StorageName ?? "";

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Availabilities");

            worksheet.PrinterSettings.FitToPage = true;
            worksheet.PrinterSettings.FitToWidth = 1;
            worksheet.PrinterSettings.FitToHeight = 0;
            worksheet.PrinterSettings.Orientation = eOrientation.Portrait;

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(4, 1);
            worksheet.SetColumnWidth(23, 2);
            worksheet.SetColumnWidth(37, 3);

            worksheet.SetColumnWidth(20, 4);
            worksheet.SetColumnWidth(20, 5);
            worksheet.SetColumnWidth(20, 6);
            worksheet.SetColumnWidth(20, 7);
            worksheet.SetColumnWidth(20, 8);


            worksheet.SetColumnWidth(10, 9);
            worksheet.SetColumnWidth(10, 10);
            worksheet.SetColumnWidth(10, 11);
            worksheet.SetColumnWidth(15, 12);
            worksheet.SetColumnWidth(15, 13);

            worksheet.SetRowHeight(11, 1);
            worksheet.SetRowHeight(38, 2);
            worksheet.SetRowHeight(17.45, 3);
            worksheet.SetRowHeight(14.5, 4);

            using (ExcelRange range = worksheet.Cells[2, 2, 2, 8]) {
                range.Merge = true;
                range.Value = string.Format("Наявність Товарів На Складі \"{0}\"", storageName);
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 14;
                range.Style.Font.Name = "Arial";
            }

            using (ExcelRange range = worksheet.Cells[3, 1, 4, 1]) {
                range.Merge = true;
                range.Value = "№";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 2, 4, 2]) {
                range.Merge = true;
                range.Value = "Код";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 3, 4, 3]) {
                range.Merge = true;
                range.Value = "Назва";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }


            using (ExcelRange range = worksheet.Cells[3, 3, 4, 3]) {
                range.Merge = true;
                range.Value = "Назва";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }


            using (ExcelRange range = worksheet.Cells[3, 4, 4, 4]) {
                range.Merge = true;
                range.Value = "Вартість Нетто";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 5, 4, 5]) {
                range.Merge = true;
                range.Value = "Ціна Брутто УО";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 6, 4, 6]) {
                range.Merge = true;
                range.Value = "Ціна Брутто БО";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 7, 4, 7]) {
                range.Merge = true;
                range.Value = "Вартість Брутто";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 8, 4, 8]) {
                range.Merge = true;
                range.Value = "Вартість Брутто (Бух.)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 9, 3, 12]) {
                range.Merge = true;
                range.Value = "Розміщення";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[4, 12, 4, 12]) {
                range.Merge = true;
                range.Value = "Кількість";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[4, 9, 4, 9]) {
                range.Value = "Склад";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[4, 10, 4, 10]) {
                range.Value = "Ряд";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[4, 11, 4, 11]) {
                range.Value = "Полиця";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 13, 4, 13]) {
                range.Merge = true;
                range.Value = "Загальна кількість";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 1, 4, 13]) {
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            int row = 5;

            int counter = 1;

            double totalQty = 0;

            foreach (ConsignmentAvailabilityItem availability in availabilities) {
                totalQty += availability.Qty;

                worksheet.SetRowHeight(14.5, row);

                int nextRow;
                if (availability.Placements.Count == 0 ||
                    availability.Placements.Count == 1)
                    nextRow = row;
                else
                    nextRow = row + availability.Placements.Count - 1;

                using (ExcelRange range = worksheet.Cells[row, 1, nextRow, 1]) {
                    range.Merge = true;
                    range.Value = counter;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 2, nextRow, 2]) {
                    range.Merge = true;
                    range.Value = availability.VendorCode;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 3, nextRow, 3]) {
                    range.Merge = true;
                    range.Value = availability.ProductName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 4, nextRow, 4]) {
                    range.Merge = true;
                    range.Value = availability.NetPrice;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 5, nextRow, 5]) {
                    range.Merge = true;
                    range.Value = availability.UnitGrossPrice;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 6, nextRow, 6]) {
                    range.Merge = true;
                    range.Value = availability.UnitAccountingGrossPrice;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 7, nextRow, 7]) {
                    range.Merge = true;
                    range.Value = availability.GrossPrice;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 8, nextRow, 8]) {
                    range.Merge = true;
                    range.Value = availability.AccountingGrossPrice;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.00";
                }


                if (row.Equals(nextRow)) {
                    using (ExcelRange range = worksheet.Cells[row, 9, nextRow, 9]) {
                        range.Merge = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 10, nextRow, 10]) {
                        range.Merge = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 11, nextRow, 11]) {
                        range.Merge = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 12, nextRow, 12]) {
                        range.Merge = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }
                }

                using (ExcelRange range = worksheet.Cells[row, 2, nextRow, 13]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Style.Numberformat.Format = "0.00";
                }

                if (availability.Placements.Any()) {
                    using (ExcelRange range = worksheet.Cells[row, 13, nextRow, 13]) {
                        range.Merge = true;
                        range.Value = availability.Placements.Sum(x => x.Qty);
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0";
                    }

                    foreach (ProductPlacement productPlacement in availability.Placements) {
                        using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                            range.Value = productPlacement.StorageNumber.Equals("N") ? string.Empty : productPlacement.StorageNumber;

                            range.Style.Numberformat.Format = "0";
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                            range.Value = productPlacement.RowNumber.Equals("N") ? string.Empty : productPlacement.StorageNumber;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                            range.Value = productPlacement.CellNumber.Equals("N") ? string.Empty : productPlacement.StorageNumber;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                            range.Value = productPlacement.Qty;
                            range.Style.Numberformat.Format = "0";
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        row++;
                    }
                } else {
                    using (ExcelRange range = worksheet.Cells[row, 13, nextRow, 13]) {
                        range.Merge = true;
                        range.Value = availability.Qty;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0";
                    }

                    row++;
                }

                counter++;
            }

            using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Разом:";
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = totalQty;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Style.Numberformat.Format = "0";
            }

            package.Workbook.Properties.Title = "Product Availability By Storage Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }
}