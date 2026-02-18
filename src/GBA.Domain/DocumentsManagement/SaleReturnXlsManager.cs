using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using GBA.Common.Extensions;
using GBA.Common.Helpers;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Supplies.Returns;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public sealed class SaleReturnXlsManager : BaseXlsManager, ISaleReturnXlsManager {
    public (string xlsxFile, string pdfFile) ExportPlInvoicePzToXlsx(string path, SaleReturn saleReturn) {
        string fileName = Path.Combine(path, $"PZ_{saleReturn.Sale.SaleNumber.Value}_{DateTime.Now.ToString("MM.yyyy")}_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("PZ");

            //Set printer settings
            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Set column's width
            worksheet.SetColumnWidth(1.78, 1);
            worksheet.SetColumnWidth(3.27, 2);
            worksheet.SetColumnWidth(3.27, 3);
            worksheet.SetColumnWidth(3.27, 4);
            worksheet.SetColumnWidth(3.27, 5);
            worksheet.SetColumnWidth(3.87, 6);
            worksheet.SetColumnWidth(7.21, 7);
            worksheet.SetColumnWidth(7.21, 8);
            worksheet.SetColumnWidth(7.21, 9);
            worksheet.SetColumnWidth(7.21, 10);
            worksheet.SetColumnWidth(7.21, 11);
            worksheet.SetColumnWidth(10.91, 12);
            worksheet.SetColumnWidth(5.11, 13);
            worksheet.SetColumnWidth(7.21, 14);
            worksheet.SetColumnWidth(13.27, 15);
            worksheet.SetColumnWidth(8.47, 16);
            worksheet.SetColumnWidth(8.51, 17);
            worksheet.SetColumnWidth(13.01, 18);
            worksheet.SetColumnWidth(16.99, 19);

            worksheet.SetRowHeight(12.71, new[] { 1, 5, 6, 8, 9, 10 });
            worksheet.SetRowHeight(6.21, new[] { 2, 3 });
            worksheet.SetRowHeight(5.11, new[] { 4 });
            worksheet.SetRowHeight(25.62, new[] { 7 });

            using (ExcelRange range = worksheet.Cells[1, 2, 6, 11]) {
                range.Merge = true;
                range.Value = "(pieczęć)";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[1, 12, 5, 15]) {
                range.Merge = true;
                range.Value = saleReturn.Client.FullName;
                //"\"TOCAR\" MARCIN TOKARSKI";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[6, 12, 6, 15]) {
                range.Merge = true;
                range.Value = "Dostawca";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[1, 16, 2, 17]) {
                range.Merge = true;
                range.Value = "PZ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[3, 16, 6, 17]) {
                range.Merge = true;
                range.Value = "przyjęcie zewnętrzne";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[1, 18, 1, 18]) {
                range.Merge = true;
                range.Value = "Nr Bieżący";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[2, 18, 3, 18]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[4, 18, 5, 18]) {
                range.Merge = true;
                range.Value = "Nr  magazynowy";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[6, 18, 6, 18]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[1, 19, 1, 19]) {
                range.Merge = true;
                range.Value = "Egz.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[2, 19, 3, 19]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[4, 19, 5, 19]) {
                range.Merge = true;
                range.Value = "Data wystawienia";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[6, 19, 6, 19]) {
                range.Merge = true;
                range.Value = saleReturn.FromDate.ToString("dd.MM.yyyy");
                //"06.12.2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[7, 2, 7, 6]) {
                range.Merge = true;
                range.Value = "Środek transportu";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[7, 7, 7, 11]) {
                range.Merge = true;
                range.Value = "Zamówienie";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[7, 12, 7, 12]) {
                range.Merge = true;
                range.Value = "Przeznaczenie";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[7, 13, 7, 14]) {
                range.Merge = true;
                range.Value = "Data wysyłki";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[7, 15, 7, 15]) {
                range.Merge = true;
                range.Value = "Data otrzymania";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[7, 16, 7, 19]) {
                range.Merge = true;
                range.Value = "Numer i data faktury - specyfikacji";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[8, 2, 8, 6]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[8, 7, 8, 11]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[8, 12, 8, 12]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[8, 13, 8, 14]) {
                range.Merge = true;
                range.Value =
                    saleReturn.Sale.ChangedToInvoice.HasValue
                        ? saleReturn.Sale.ChangedToInvoice?.ToString("dd.MM.yyyy")
                        : saleReturn.Sale.Updated.ToString("dd.MM.yyyy");
                //"29.11.2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[8, 15, 8, 15]) {
                range.Merge = true;
                range.Value = saleReturn.FromDate.ToString("dd.MM.yyyy");
                //"06.12.2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[8, 16, 8, 19]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "Faktura VAT Korekta {0}/{1}/{2} od {3}",
                        string.Format(
                            "{0:D2}",
                            Convert.ToInt64(
                                saleReturn.Number.Substring(
                                    saleReturn.Sale.ClientAgreement.Agreement.Organization.Code.Length,
                                    saleReturn.Number.Length - saleReturn.Sale.ClientAgreement.Agreement.Organization.Code.Length
                                )
                            )
                        ),
                        string.Format("{0:D2}", saleReturn.FromDate.Month),
                        saleReturn.FromDate.Year,
                        saleReturn.FromDate.ToString("dd.MM.yyyy")
                    );
                //string.Format(
                //    "{0}/{1}/{2}",
                //    string.Format(
                //        "{0:D2}",
                //        saleReturn.Number.StartsWith("P")
                //            ? Convert.ToInt64(saleReturn.Number.Substring(1, saleReturn.Number.Length - 1))
                //            : Convert.ToInt64(saleReturn.Number)
                //    ),
                //    string.Format("{0:D2}", saleReturn.FromDate.Month),
                //    saleReturn.FromDate.Year
                //);
                //@"Faktura VAT Korekta 01/12/2018 od 06.12.2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            //Table header
            using (ExcelRange range = worksheet.Cells[9, 2, 9, 6]) {
                range.Merge = true;
                range.Value = @"Kod towaru/ materiału";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[9, 7, 9, 11]) {
                range.Merge = true;
                range.Value = @"Nazwa towaru/materiału/opakowania";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[9, 12, 9, 14]) {
                range.Merge = true;
                range.Value = "Ilość";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[9, 15, 9, 15]) {
                range.Merge = true;
                range.Value = "Cena";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[9, 16, 9, 17]) {
                range.Merge = true;
                range.Value = "Wartość";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[9, 18, 9, 18]) {
                range.Merge = true;
                range.Value = "Konto syntet.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[9, 19, 9, 19]) {
                range.Merge = true;
                range.Value = "Zapas";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[10, 2, 10, 6]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[10, 7, 10, 11]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[10, 12, 10, 12]) {
                range.Merge = true;
                range.Value = "Dostarczona";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[10, 13, 10, 13]) {
                range.Merge = true;
                range.Value = "j.m.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[10, 14, 10, 14]) {
                range.Merge = true;
                range.Value = "Przyjęta";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[10, 15, 10, 15]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[10, 16, 10, 17]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[10, 18, 10, 18]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[10, 19, 10, 19]) {
                range.Merge = true;
                range.Value = "Ilość";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            int row = 11;

            //Table body
            decimal totalAmountLocal = 0m;

            foreach (SaleReturnItem item in saleReturn.SaleReturnItems)
                if (item.ProductIncomeItem != null && item.ProductIncomeItem.ConsignmentItems.Any()) {
                    foreach (ConsignmentItem consignmentItem in item.ProductIncomeItem.ConsignmentItems) {
                        using (ExcelRange range = worksheet.Cells[row, 2, row, 6]) {
                            range.Merge = true;
                            range.Value = item.OrderItem.Product.VendorCode;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 7, row, 11]) {
                            range.Merge = true;
                            range.Value = item.OrderItem.Product.Name;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                            range.Merge = true;
                            range.Value = consignmentItem.Qty;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                            range.Merge = true;
                            range.Value = item.OrderItem.Product.MeasureUnit.Name;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                            range.Merge = true;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                        }

                        decimal pricePerItem = consignmentItem.RootConsignmentItem?.Price ?? consignmentItem.Price;

                        decimal totalAmount = decimal.Round(pricePerItem * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);

                        using (ExcelRange range = worksheet.Cells[row, 15, row, 15]) {
                            range.Merge = true;
                            range.Value = decimal.Round(pricePerItem, 2, MidpointRounding.AwayFromZero);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Numberformat.Format = "0.00";
                            range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 16, row, 17]) {
                            range.Merge = true;
                            range.Value = totalAmount;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Numberformat.Format = "0.00";
                            range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 18, row, 18]) {
                            range.Merge = true;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 19, row, 19]) {
                            range.Merge = true;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                        }

                        worksheet.SetRowHeight(12.71, row);
                        row++;

                        totalAmountLocal = decimal.Round(totalAmountLocal + totalAmount, 2, MidpointRounding.AwayFromZero);
                    }
                } else {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 6]) {
                        range.Merge = true;
                        range.Value = item.OrderItem.Product.VendorCode;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 7, row, 11]) {
                        range.Merge = true;
                        range.Value = item.OrderItem.Product.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                        range.Merge = true;
                        range.Value = item.Qty;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                        range.Merge = true;
                        range.Value = item.OrderItem.Product.MeasureUnit.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                    }

                    decimal pricePerItem = decimal.Round(item.Amount / Convert.ToDecimal(item.Qty), 4, MidpointRounding.AwayFromZero);

                    decimal totalAmount = decimal.Round(pricePerItem * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);

                    using (ExcelRange range = worksheet.Cells[row, 15, row, 15]) {
                        range.Merge = true;
                        range.Value = decimal.Round(pricePerItem, 2, MidpointRounding.AwayFromZero);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Numberformat.Format = "0.00";
                        range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 16, row, 17]) {
                        range.Merge = true;
                        range.Value = totalAmount;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Numberformat.Format = "0.00";
                        range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 18, row, 18]) {
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 19, row, 19]) {
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                    }

                    worksheet.SetRowHeight(12.71, row);
                    row++;

                    totalAmountLocal = decimal.Round(totalAmountLocal + totalAmount, 2, MidpointRounding.AwayFromZero);
                }

            //Totals
            using (ExcelRange range = worksheet.Cells[row, 2, row, 6]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 11]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                range.Merge = true;
                range.Value = saleReturn.SaleReturnItems.Sum(i => i.Qty);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 15, row, 15]) {
                range.Merge = true;
                range.Value = "Razem: ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 16, row, 16]) {
                range.Merge = true;
                range.Value = totalAmountLocal;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 17, row, 17]) {
                range.Merge = true;
                range.Value = "PLN";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 18, row, 18]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 19, row, 19]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            worksheet.SetRowHeight(12.71, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 6]) {
                range.Merge = true;
                range.Value = "Wystawił";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 11]) {
                range.Merge = true;
                range.Value = "Zatwierdził";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 12, row, 19]) {
                range.Merge = true;
                range.Value = "Wymienione ilości";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            worksheet.SetRowHeight(12.71, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 6]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 11]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 12, row, 13]) {
                range.Merge = true;
                range.Value = "Dostarczył";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                range.Merge = true;
                range.Value = "Data";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 15, row, 17]) {
                range.Merge = true;
                range.Value = "Przyjął";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 18, row, 19]) {
                range.Merge = true;
                range.Value = "Ewidencja ilościowo - wartościowa";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            worksheet.SetRowHeight(12.71, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 6]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 11]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 12, row, 13]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 15, row, 17]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            using (ExcelRange range = worksheet.Cells[row, 18, row, 19]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Hair);
            }

            worksheet.SetRowHeight(12.71, row);

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Times New Roman";
            }

            package.Workbook.Properties.Title = "PZ Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            //Saving the file.
            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportPlSaleReturnToXlsx(string path, SaleReturn saleReturn) {
        string fileName = Path.Combine(path, $"{saleReturn.Number}_{DateTime.Now.ToString("MM.yyyy")}_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Korekta");

            //Set printer settings
            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.05m, 0.05m, 0.1m, 0.1m, true);

            //Set column's width
            worksheet.SetColumnWidth(0.58, 1);
            worksheet.SetColumnWidth(5.88, 2);
            worksheet.SetColumnWidth(1.58, 3);
            worksheet.SetColumnWidth(4.01, 4);
            worksheet.SetColumnWidth(2.99, 5);
            worksheet.SetColumnWidth(3.57, 6);
            worksheet.SetColumnWidth(2.11, 7);
            worksheet.SetColumnWidth(7.71, 8);
            worksheet.SetColumnWidth(3.11, 9);
            worksheet.SetColumnWidth(6.21, 10);
            worksheet.SetColumnWidth(2.87, 11);
            worksheet.SetColumnWidth(4.27, 12);
            worksheet.SetColumnWidth(7.71, 13);
            worksheet.SetColumnWidth(1.68, 14);
            worksheet.SetColumnWidth(1.68, 15);
            worksheet.SetColumnWidth(4.89, 16);
            worksheet.SetColumnWidth(2.49, 17);
            worksheet.SetColumnWidth(2.59, 18);
            worksheet.SetColumnWidth(2.31, 19);
            worksheet.SetColumnWidth(3.57, 20);
            worksheet.SetColumnWidth(0.58, 21);
            worksheet.SetColumnWidth(1.58, 22);
            worksheet.SetColumnWidth(2.00, 23);
            worksheet.SetColumnWidth(4.27, 24);
            worksheet.SetColumnWidth(0.88, 25);
            worksheet.SetColumnWidth(2.99, 26);
            worksheet.SetColumnWidth(2.21, 27);
            worksheet.SetColumnWidth(6.61, 28);
            worksheet.SetColumnWidth(4.37, 29);
            worksheet.SetColumnWidth(4.37, 30);
            worksheet.SetColumnWidth(8.81, 31);
            worksheet.SetColumnWidth(6.41, 32);
            worksheet.SetColumnWidth(2.31, 33);
            worksheet.SetColumnWidth(2.71, 34);
            worksheet.SetColumnWidth(2.31, 35);
            worksheet.SetColumnWidth(2.99, 36);
            worksheet.SetColumnWidth(2.99, 37);
            worksheet.SetColumnWidth(2.71, 38);
            worksheet.SetColumnWidth(6.51, 39);
            worksheet.SetColumnWidth(1.28, 40);

            worksheet.SetRowHeight(19.61, new[] { 1, 2 });
            worksheet.SetRowHeight(17.41, new[] { 3, 4, 5, 6, 7 });
            worksheet.SetRowHeight(16.00, new[] { 8, 10, 11, 12, 13, 14, 15, 16, 17 });
            worksheet.SetRowHeight(13.31, new[] { 9 });
            worksheet.SetRowHeight(12.41, new[] { 18 });
            worksheet.SetRowHeight(44.57, new[] { 19 });

            using (ExcelRange range = worksheet.Cells[1, 10, 1, 24]) {
                range.Merge = true;
                range.Value = "FAKTURA VAT KOREKTA";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[2, 10, 2, 13]) {
                range.Merge = true;
                range.Value = "nr";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[2, 14, 2, 24]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "{0}/{1}/{2}",
                        string.Format(
                            "{0:D2}",
                            saleReturn.Number.StartsWith("P")
                                ? Convert.ToInt64(saleReturn.Number.Substring(1, saleReturn.Number.Length - 1))
                                : Convert.ToInt64(saleReturn.Number)
                        ),
                        string.Format("{0:D2}", saleReturn.FromDate.Month),
                        saleReturn.FromDate.Year
                    );
                //01/12/2018
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[3, 10, 3, 24]) {
                range.Merge = true;
                range.Value = "oryginał / kopia";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
            }

            using (ExcelRange range = worksheet.Cells[2, 25, 2, 31]) {
                range.Merge = true;
                range.Value = "Miejsce wystawienia: ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 13;
            }

            using (ExcelRange range = worksheet.Cells[3, 25, 3, 31]) {
                range.Merge = true;
                range.Value = "Data wystawienia:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 13;
            }

            using (ExcelRange range = worksheet.Cells[4, 25, 4, 31]) {
                range.Merge = true;
                range.Value = "Dotyczy faktury VAT nr:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 13;
            }

            using (ExcelRange range = worksheet.Cells[5, 25, 5, 31]) {
                range.Merge = true;
                range.Value = "z dnia:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 13;
            }

            using (ExcelRange range = worksheet.Cells[6, 25, 6, 31]) {
                range.Merge = true;
                range.Value = "Sposób zapłaty:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 13;
            }

            using (ExcelRange range = worksheet.Cells[7, 25, 7, 31]) {
                range.Merge = true;
                range.Value = "Termin zapłaty:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 13;
            }

            using (ExcelRange range = worksheet.Cells[2, 32, 2, 39]) {
                range.Merge = true;
                range.Value = "Przemysl";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 13;
            }

            using (ExcelRange range = worksheet.Cells[3, 32, 3, 39]) {
                range.Merge = true;
                range.Value = saleReturn.FromDate.ToString("dd.MM.yyyy");
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 13;
            }

            using (ExcelRange range = worksheet.Cells[4, 32, 4, 39]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "F{0}/{1}",
                        string.Format(
                            "{0:D4}",
                            Convert.ToInt64(
                                saleReturn.Sale.SaleNumber.Value.Substring(
                                    saleReturn.Sale.ClientAgreement.Agreement.Organization.Code.Length,
                                    saleReturn.Sale.SaleNumber.Value.Length - saleReturn.Sale.ClientAgreement.Agreement.Organization.Code.Length
                                )
                            )
                        ),
                        saleReturn.Sale.ChangedToInvoice.HasValue
                            ? saleReturn.Sale.ChangedToInvoice.Value.ToString("MM.yyyy")
                            : saleReturn.Sale.Updated.ToString("MM.yyyy")
                    );
                //saleReturn.Sale.SaleNumber.Value.StartsWith("P")
                //    ? Convert.ToInt64(saleReturn.Sale.SaleNumber.Value.Substring(1, saleReturn.Sale.SaleNumber.Value.Length - 1))
                //    : Convert.ToInt64(saleReturn.Sale.SaleNumber.Value);
                //"F 120/11/2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 13;
            }

            using (ExcelRange range = worksheet.Cells[5, 32, 5, 39]) {
                range.Merge = true;
                range.Value =
                    saleReturn.Sale.ChangedToInvoice.HasValue
                        ? saleReturn.Sale.ChangedToInvoice.Value.ToString("dd.MM.yyyy")
                        : saleReturn.Sale.Updated.ToString("dd.MM.yyyy");
                //"29.11.2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 13;
            }

            using (ExcelRange range = worksheet.Cells[6, 32, 6, 39]) {
                range.Merge = true;

                switch (saleReturn.Sale.SaleInvoiceDocument.PaymentType) {
                    case SalePaymentType.Cash:
                        range.Value = "Gotowka";
                        break;
                    case SalePaymentType.Transfer:
                        range.Value = "Przelew";
                        break;
                    case SalePaymentType.CashAfterDelivery:
                    default:
                        range.Value = "Pobranie";
                        break;
                }
                //"Przelew";

                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 13;
            }

            using (ExcelRange range = worksheet.Cells[7, 32, 7, 39]) {
                range.Merge = true;
                range.Value = saleReturn.Sale.ClientAgreement.Agreement.NumberDaysDebt;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 13;
            }

            //Supplier info
            using (ExcelRange range = worksheet.Cells[8, 2, 8, 6]) {
                range.Merge = true;
                range.Value = "Sprzedawca:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[10, 2, 12, 14]) {
                range.Merge = true;
                range.Value = "CONCORD.PL Sp z o.o.,\r37-700, Przemyśl, ul. Gen.Jakuba Jasińskiego 58,\r12193013182740072619290001 Bank BPS S.A.";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 11;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[13, 2, 14, 2]) {
                range.Merge = true;
            }

            using (ExcelRange range = worksheet.Cells[13, 2, 14, 14]) {
                range.Merge = true;
                range.Value = "NIP 8133680920";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 11;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[10, 2, 14, 14]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            //Client info

            string clientInfo =
                string.Format(
                    "{0},\r\n{1}",
                    saleReturn.Client.FullName,
                    saleReturn.Client.ActualAddress
                );

            using (ExcelRange range = worksheet.Cells[8, 21, 8, 27]) {
                range.Merge = true;
                range.Value = "Nabywca:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[10, 21, 12, 39]) {
                range.Merge = true;
                range.Value = clientInfo;
                //"\"TOCAR\" MARCIN TOKARSKI,\r38-500 Sanok, ul. Krakowska 92";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 11;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[13, 21, 14, 24]) {
                range.Merge = true;
            }

            using (ExcelRange range = worksheet.Cells[13, 25, 14, 39]) {
                range.Merge = true;
                range.Value = $"NIP {saleReturn.Client.TIN}";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 11;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[10, 21, 14, 39]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 2, 16, 7]) {
                range.Merge = true;
                range.Value = "Przyczyna korekty:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
            }

            using (ExcelRange range = worksheet.Cells[17, 2, 17, 7]) {
                range.Merge = true;
                range.Value = "zwrot klienta";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Font.UnderLine = true;
            }

            //Table header
            using (ExcelRange range = worksheet.Cells[19, 2, 19, 4]) {
                range.Merge = true;
                range.Value = "Poz.\rFaktury";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[19, 5, 19, 13]) {
                range.Merge = true;
                range.Value = "Nazwa towaru/usługi";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[19, 14, 19, 17]) {
                range.Merge = true;
                range.Value = "Symbol\rPKWiU/\rPKOB";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[19, 18, 19, 21]) {
                range.Merge = true;
                range.Value = "Jm";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[19, 22, 19, 25]) {
                range.Merge = true;
                range.Value = "Ilość";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[19, 26, 19, 29]) {
                range.Merge = true;
                range.Value = "Cena jednostkowa netto";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[19, 30, 19, 31]) {
                range.Merge = true;
                range.Value = "Wartość netto";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[19, 32, 19, 33]) {
                range.Merge = true;
                range.Value = "Stawka VAT [%]";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[19, 34, 19, 36]) {
                range.Merge = true;
                range.Value = "Kwota VAT";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[19, 37, 19, 39]) {
                range.Merge = true;
                range.Value = "Wartość brutto";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            int row = 20;

            decimal vatRate = saleReturn.Sale.SaleInvoiceDocument.Vat;

            decimal totalNetPrice = decimal.Zero;
            decimal totalGrossPrice = decimal.Zero;
            decimal totalVatAmount = decimal.Zero;

            decimal totalNetPriceKr = decimal.Zero;
            decimal totalGrossPriceKr = decimal.Zero;
            decimal totalVatAmountKr = decimal.Zero;

            //Simple row items
            foreach (SaleReturnItem item in saleReturn.SaleReturnItems) {
                using (ExcelRange range = worksheet.Cells[row, 2, row, 4]) {
                    range.Merge = true;
                    range.Value = 1;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 13]) {
                    range.Merge = true;
                    range.Value =
                        string.Format(
                            "{0} {1}",
                            item.OrderItem.Product.VendorCode,
                            item.OrderItem.Product.NamePL
                        );
                    //"3005-21 Zawór siłownika sprzęgła";
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 14, row, 17]) {
                    range.Merge = true;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 18, row, 21]) {
                    range.Merge = true;
                    range.Value = item.OrderItem.Product.MeasureUnit.Name;
                    //"szt";
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 22, row, 25]) {
                    range.Merge = true;
                    range.Value = item.Qty;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Numberformat.Format = "0.000";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                decimal localPricePerItem =
                    decimal.Round(
                        item.OrderItem.PricePerItemWithoutVat * item.OrderItem.ExchangeRateAmount,
                        4,
                        MidpointRounding.AwayFromZero
                    );

                decimal currentNetPrice = decimal.Round(localPricePerItem * Convert.ToDecimal(item.Qty), 4, MidpointRounding.AwayFromZero);

                decimal vatAmount = decimal.Round(currentNetPrice * vatRate / 100m, 4, MidpointRounding.AwayFromZero);

                decimal currentGrossPrice = decimal.Round(currentNetPrice + vatAmount, 4, MidpointRounding.AwayFromZero);

                totalNetPrice = decimal.Round(totalNetPrice + currentNetPrice, 4, MidpointRounding.AwayFromZero);
                totalGrossPrice = decimal.Round(totalGrossPrice + currentGrossPrice, 4, MidpointRounding.AwayFromZero);
                totalVatAmount = decimal.Round(totalVatAmount + vatAmount, 4, MidpointRounding.AwayFromZero);

                using (ExcelRange range = worksheet.Cells[row, 26, row, 29]) {
                    range.Merge = true;
                    range.Value = localPricePerItem;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                    range.Merge = true;
                    range.Value = currentNetPrice;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                    range.Merge = true;
                    range.Value = vatRate;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                    range.Merge = true;
                    range.Value = vatAmount;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                    range.Merge = true;
                    range.Value = currentGrossPrice;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                worksheet.SetRowHeight(16.00, row);
                row++;

                double currentQty = item.OrderItem.Qty - item.Qty;

                using (ExcelRange range = worksheet.Cells[row, 2, row, 4]) {
                    range.Merge = true;
                    range.Value = "po korekcie";
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 13]) {
                    range.Merge = true;
                    range.Value =
                        string.Format(
                            "{0} {1}",
                            item.OrderItem.Product.VendorCode,
                            item.OrderItem.Product.NamePL
                        );
                    //"00000-TT Zawór siłownika";
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 14, row, 17]) {
                    range.Merge = true;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 18, row, 21]) {
                    range.Merge = true;
                    range.Value = item.OrderItem.Product.MeasureUnit.Name;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 22, row, 25]) {
                    range.Merge = true;
                    range.Value = currentQty;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Numberformat.Format = "0.000";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                localPricePerItem =
                    decimal.Round(
                        item.OrderItem.PricePerItemWithoutVat * item.OrderItem.ExchangeRateAmount,
                        4,
                        MidpointRounding.AwayFromZero
                    );

                currentNetPrice = decimal.Round(localPricePerItem * Convert.ToDecimal(currentQty), 4, MidpointRounding.AwayFromZero);

                vatAmount = decimal.Round(currentNetPrice * vatRate / 100m, 4, MidpointRounding.AwayFromZero);

                currentGrossPrice = decimal.Round(currentNetPrice + vatAmount, 4, MidpointRounding.AwayFromZero);

                totalNetPriceKr = decimal.Round(totalNetPriceKr + currentNetPrice, 4, MidpointRounding.AwayFromZero);
                totalGrossPriceKr = decimal.Round(totalGrossPriceKr + currentGrossPrice, 4, MidpointRounding.AwayFromZero);
                totalVatAmountKr = decimal.Round(totalVatAmountKr + vatAmount, 4, MidpointRounding.AwayFromZero);

                using (ExcelRange range = worksheet.Cells[row, 26, row, 29]) {
                    range.Merge = true;
                    range.Value = localPricePerItem;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                    range.Merge = true;
                    range.Value = currentNetPrice;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                    range.Merge = true;
                    range.Value = vatRate;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                    range.Merge = true;
                    range.Value = vatAmount;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                    range.Merge = true;
                    range.Value = currentGrossPrice;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                worksheet.SetRowHeight(30.41, row);
                row++;
            }

            // "po krekcie" item
            //foreach (SaleReturnItem item in saleReturn.SaleReturnItems) {
            //    double currentQty = item.OrderItem.Qty - item.Qty;

            //    using (ExcelRange range = worksheet.Cells[row, 2, row, 4]) {
            //        range.Merge = true;
            //        range.Value = "po korekcie";
            //        range.Style.WrapText = true;
            //        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            //        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            //        range.Style.Font.Size = 12;
            //        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            //    }
            //    using (ExcelRange range = worksheet.Cells[row, 5, row, 13]) {
            //        range.Merge = true;
            //        range.Value =
            //            string.Format(
            //                "{0} {1}",
            //                item.OrderItem.Product.VendorCode,
            //                item.OrderItem.Product.NamePL
            //            );
            //        //"00000-TT Zawór siłownika";
            //        range.Style.WrapText = true;
            //        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            //        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            //        range.Style.Font.Size = 12;
            //        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            //    }
            //    using (ExcelRange range = worksheet.Cells[row, 14, row, 17]) {
            //        range.Merge = true;
            //        range.Style.WrapText = true;
            //        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            //        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            //        range.Style.Font.Size = 12;
            //        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            //    }
            //    using (ExcelRange range = worksheet.Cells[row, 18, row, 21]) {
            //        range.Merge = true;
            //        range.Value = item.OrderItem.Product.MeasureUnit.Name;
            //        range.Style.WrapText = true;
            //        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            //        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            //        range.Style.Font.Size = 12;
            //        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            //    }
            //    using (ExcelRange range = worksheet.Cells[row, 22, row, 25]) {
            //        range.Merge = true;
            //        range.Value = currentQty;
            //        range.Style.WrapText = true;
            //        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            //        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            //        range.Style.Font.Size = 12;
            //        range.Style.Numberformat.Format = "0.000";
            //        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            //    }

            //    decimal localPricePerItem =
            //        decimal.Round(
            //            item.OrderItem.PricePerItemWithoutVat * item.OrderItem.ExchangeRateAmount,
            //            4,
            //            MidpointRounding.AwayFromZero
            //        );

            //    decimal currentNetPrice = decimal.Round(localPricePerItem * Convert.ToDecimal(currentQty), 4, MidpointRounding.AwayFromZero);

            //    decimal vatAmount = decimal.Round(currentNetPrice * vatRate / 100m, 4, MidpointRounding.AwayFromZero);

            //    decimal currentGrossPrice = decimal.Round(currentNetPrice + vatAmount, 4, MidpointRounding.AwayFromZero);

            //    totalNetPriceKr = decimal.Round(totalNetPriceKr + currentNetPrice, 4, MidpointRounding.AwayFromZero);
            //    totalGrossPriceKr = decimal.Round(totalGrossPriceKr + currentGrossPrice, 4, MidpointRounding.AwayFromZero);
            //    totalVatAmountKr = decimal.Round(totalVatAmountKr + vatAmount, 4, MidpointRounding.AwayFromZero);

            //    using (ExcelRange range = worksheet.Cells[row, 26, row, 29]) {
            //        range.Merge = true;
            //        range.Value = localPricePerItem;
            //        range.Style.WrapText = true;
            //        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            //        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            //        range.Style.Font.Size = 12;
            //        range.Style.Numberformat.Format = "0.00";
            //        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            //    }
            //    using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
            //        range.Merge = true;
            //        range.Value = currentNetPrice;
            //        range.Style.WrapText = true;
            //        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            //        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            //        range.Style.Font.Size = 12;
            //        range.Style.Numberformat.Format = "0.00";
            //        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            //    }
            //    using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
            //        range.Merge = true;
            //        range.Value = vatRate;
            //        range.Style.WrapText = true;
            //        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            //        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            //        range.Style.Font.Size = 12;
            //        range.Style.Numberformat.Format = "0.00";
            //        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            //    }
            //    using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
            //        range.Merge = true;
            //        range.Value = vatAmount;
            //        range.Style.WrapText = true;
            //        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            //        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            //        range.Style.Font.Size = 12;
            //        range.Style.Numberformat.Format = "0.00";
            //        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            //    }
            //    using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
            //        range.Merge = true;
            //        range.Value = currentGrossPrice;
            //        range.Style.WrapText = true;
            //        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            //        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            //        range.Style.Font.Size = 12;
            //        range.Style.Numberformat.Format = "0.00";
            //        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            //    }

            //    worksheet.SetRowHeight(30.41, row);
            //    row++;
            //}

            worksheet.SetRowHeight(12.41, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 20, row, 29]) {
                range.Merge = true;
                range.Value = "Razem przed korektą";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 11;
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                range.Merge = true;
                range.Value = totalNetPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = "X";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmount;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(16.00, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 28, row, 29]) {
                range.Merge = true;
                range.Value = "w tym";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 11;
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                range.Merge = true;
                range.Value = totalNetPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = vatRate;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmount;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }


            worksheet.SetRowHeight(16.00, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = "8";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(14.41, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = "5";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(14.41, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = "0";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(15.41, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = "zw";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(15.41, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 20, row, 29]) {
                range.Merge = true;
                range.Value = "Razem po korekcie";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 11;
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                range.Merge = true;
                range.Value = totalNetPriceKr;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = "X";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmountKr;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPriceKr;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(16.00, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 28, row, 29]) {
                range.Merge = true;
                range.Value = "w tym";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 11;
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                range.Merge = true;
                range.Value = totalNetPriceKr;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = vatRate;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmountKr;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPriceKr;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(14.41, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = "8";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(15.41, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = "5";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(16.00, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = "0";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(15.41, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = "zw";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = 0;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(16.00, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 13, row, 29]) {
                range.Merge = true;
                range.Value = "Zmniejszenie wartości netto  - podatku - należności*)";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                range.Merge = true;
                range.Value = totalNetPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = "X";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmount;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(16.00, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 13, row, 29]) {
                range.Merge = true;
                range.Value = "Zwiększenie wartości netto  - podatku - należności*)";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                range.Merge = true;
                range.Value = totalNetPriceKr;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 33]) {
                range.Merge = true;
                range.Value = "X";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmountKr;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPriceKr;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(16.00, row);
            row++;

            worksheet.SetRowHeight(12.41, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Razem do zapłaty/do zwrotu:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 11;
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 12]) {
                range.Merge = true;
                range.Value = totalGrossPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                range.Merge = true;
                range.Value = "zł";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
            }

            worksheet.SetRowHeight(16.00, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 4]) {
                range.Merge = true;
                range.Value = "Słownie: ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 11;
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 23]) {
                range.Merge = true;
                range.Value = 328.67;
                range.Value =
                    string.Format(
                        "{0} zł. {1} gr.",
                        totalGrossPrice.ToText(true, false),
                        (decimal.Round(totalGrossPrice % 1, 2) * 100m).ToText(false, false)
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            worksheet.SetRowHeight(16.00, row);
            row++;

            worksheet.SetRowHeight(16.00, row);
            row++;

            worksheet.SetRowHeight(16.00, row);
            row++;

            worksheet.SetRowHeight(16.00, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 13]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 39]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(16.00, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 13]) {
                range.Merge = true;
                range.Value = "data i podpis odbiorcy faktury korygującej";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 39]) {
                range.Merge = true;
                range.Value = "podpis wystawcy faktury korygującej";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            worksheet.SetRowHeight(16.00, row);
            row++;

            worksheet.SetRowHeight(16.00, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "*) niepotrzebne skreślić";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            worksheet.SetRowHeight(16.00, row);

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Arial";
            }

            package.Workbook.Properties.Title = "Korekta Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            //Saving the file.
            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsFile, string pdfFile) ExportUkSaleReturnToXlsx(string path, SaleReturn saleReturn, IEnumerable<DocumentMonth> months) {
        string fileName = Path.Combine(path, $"{saleReturn.Number}_{DateTime.Now.ToString("MM.yyyy")}_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Повернення");

            //Set printer settings
            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.Cells.Style.Font.Name = "Arial";
            worksheet.Cells.Style.Font.Size = 8;
            worksheet.DefaultRowHeight = 10.20;

            worksheet.SetColumnWidth(0.67, 1);
            worksheet.SetColumnWidth(3.5, 2);
            worksheet.SetColumnWidth(2.99, 3, 38);
            worksheet.SetColumnWidth(12, 8);
            worksheet.SetColumnWidth(12, 23);
            worksheet.SetColumnWidth(12, 24);

            int column = 2;
            int row = 2;

            worksheet.SetRowHeight(21, row);

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 36]) {
                range.Value =
                    string.Format(
                        "Накладна на повернення від покупця № {0} від {1} {2} {3} р.",
                        Convert.ToInt32(
                            !string.IsNullOrEmpty(saleReturn.Sale.ClientAgreement.Agreement.Organization.Code)
                                ? saleReturn.Number.Substring(
                                    saleReturn.Sale.ClientAgreement.Agreement.Organization.Code.Length,
                                    saleReturn.Number.Length - saleReturn.Sale.ClientAgreement.Agreement.Organization.Code.Length
                                )
                                : saleReturn.Number
                        ),
                        saleReturn.FromDate.Day,
                        months.FirstOrDefault(m => m.Number == saleReturn.FromDate.Month)?.Name.ToLower() ?? string.Empty,
                        saleReturn.FromDate.Year
                    );
                range.Merge = true;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            row += 2;

            worksheet.SetRowHeight(12.60, row);

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 4]) {
                range.Value = "Постачальник:";
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.Font.UnderLine = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 5, row, column + 36]) {
                range.Value = saleReturn.Sale.ClientAgreement.Agreement.Organization.Name;
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, column + 5, row, column + 36]) {
                range.Value = "Не є платником податку на прибуток на загальних підставах";
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            worksheet.SetRowHeight(3.75, row + 1);

            row += 2;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 3]) {
                range.Value = "Покупець:";
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 5, row, column + 36]) {
                range.Value = saleReturn.Client.FullName;
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, column + 5, row, column + 36]) {
                string clientInfo = string.Empty;

                if (saleReturn.Client.RegionCode != null) {
                    if (!string.IsNullOrEmpty(saleReturn.Client.RegionCode.City)) clientInfo += $"{saleReturn.Client.RegionCode.City}, ";

                    if (!string.IsNullOrEmpty(saleReturn.Client.RegionCode.District)) clientInfo += $"{saleReturn.Client.RegionCode.District}, ";

                    if (!string.IsNullOrEmpty(saleReturn.Client.ActualAddress)) clientInfo += $"{saleReturn.Client.ActualAddress}, ";

                    if (!string.IsNullOrEmpty(saleReturn.Client.MobileNumber)) clientInfo += $"тел.: {saleReturn.Client.MobileNumber}";
                }

                range.Value = clientInfo;
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            worksheet.SetRowHeight(3.60, row + 1);

            row += 2;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 3]) {
                range.Value = "Договір:";
                range.Merge = true;
                range.Style.Font.Size = 10;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 4, row, column + 36]) {
                range.Value = saleReturn.Sale.ClientAgreement.Agreement.Name;
                range.Merge = true;
                range.Style.Font.Size = 10;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            worksheet.SetRowHeight(3.60, row + 1);

            row += 2;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 3]) {
                range.Value = "Склад:";
                range.Merge = true;
                range.Style.Font.Size = 10;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 4, row, column + 36]) {
                range.Value = saleReturn.Storage.Name;
                range.Merge = true;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            worksheet.SetRowHeight(3.60, row + 1);

            row += 2;

            //Table Header
            using (ExcelRange range = worksheet.Cells[row, column, row, column + 1]) {
                range.Value = "№";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 2, row, column + 9]) {
                range.Value = "Артикул";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 10, row, column + 13]) {
                range.Value = "Розм.";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 14, row, column + 22]) {
                range.Value = "Товар";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 23, row, column + 28]) {
                range.Value = "Кількість";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 29, row, column + 32]) {
                range.Value = "Ціна";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 33, row, column + 36]) {
                range.Value = "Сума";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            worksheet.SetRowHeight(27.60, row);

            row++;

            int indexer = 1;

            foreach (SaleReturnItem returnItem in saleReturn.SaleReturnItems) {
                decimal currentPrice = decimal.Round(returnItem.Amount / Convert.ToDecimal(returnItem.Qty), 2, MidpointRounding.AwayFromZero);
                if (returnItem.SaleReturnItemProductPlacements.Any()) {
                    foreach (SaleReturnItemProductPlacement item in returnItem.SaleReturnItemProductPlacements) {
                        using (ExcelRange range = worksheet.Cells[row, column, row, column + 1]) {
                            range.Value = indexer;
                            range.Merge = true;
                            range.Style.Font.Size = 11;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        using (ExcelRange range = worksheet.Cells[row, column + 2, row, column + 9]) {
                            range.Value = returnItem.OrderItem.Product.VendorCode;
                            range.Merge = true;
                            range.Style.Font.Size = 14;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        string placement = string.Empty;
                        placement += item.ProductPlacement.StorageNumber + "-" + item.ProductPlacement.RowNumber + "-" + item.ProductPlacement.CellNumber + " ";
                        using (ExcelRange range = worksheet.Cells[row, column + 10, row, column + 13]) {
                            range.Style.WrapText = true;
                            if (returnItem.SaleReturnItemProductPlacements != null)
                                range.Value = placement;
                            else
                                range.Value = "";
                            range.Merge = true;
                            range.Style.Font.Size = 11;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        using (ExcelRange range = worksheet.Cells[row, column + 14, row, column + 22]) {
                            range.Value = returnItem.OrderItem.Product.NameUA;
                            range.Merge = true;
                            range.Style.Font.Size = 11;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        using (ExcelRange range = worksheet.Cells[row, column + 23, row, column + 25]) {
                            range.Value = item.Qty;
                            range.Merge = true;
                            range.Style.Font.Size = 14;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        using (ExcelRange range = worksheet.Cells[row, column + 26, row, column + 28]) {
                            range.Value = returnItem.OrderItem.Product.MeasureUnit.Name;
                            range.Merge = true;
                            range.Style.Font.Size = 11;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        using (ExcelRange range = worksheet.Cells[row, column + 29, row, column + 32]) {
                            range.Value = currentPrice;
                            range.Merge = true;
                            range.Style.Font.Size = 11;
                            range.Style.Numberformat.Format = "0.00";
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        using (ExcelRange range = worksheet.Cells[row, column + 33, row, column + 36]) {
                            range.Value = decimal.Round(currentPrice * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);
                            range.Merge = true;
                            range.Style.Font.Size = 11;
                            range.Style.Numberformat.Format = "0.00";
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        worksheet.SetRowHeight(27.60, row);

                        row++;
                        indexer++;
                    }
                } else {
                    using (ExcelRange range = worksheet.Cells[row, column, row, column + 1]) {
                        range.Value = indexer;
                        range.Merge = true;
                        range.Style.Font.Size = 11;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 2, row, column + 9]) {
                        range.Value = returnItem.OrderItem.Product.VendorCode;
                        range.Merge = true;
                        range.Style.Font.Size = 14;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    string placement = string.Empty;
                    foreach (SaleReturnItemProductPlacement item in returnItem.SaleReturnItemProductPlacements)
                        placement += item.ProductPlacement.StorageNumber + "-" + item.ProductPlacement.RowNumber + "-" + item.ProductPlacement.CellNumber + " ";
                    using (ExcelRange range = worksheet.Cells[row, column + 10, row, column + 13]) {
                        range.Style.WrapText = true;
                        if (returnItem.SaleReturnItemProductPlacements != null)
                            range.Value = placement;
                        else
                            range.Value = "";
                        range.Merge = true;
                        range.Style.Font.Size = 11;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 14, row, column + 22]) {
                        range.Value = returnItem.OrderItem.Product.NameUA;
                        range.Merge = true;
                        range.Style.Font.Size = 11;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 23, row, column + 25]) {
                        range.Value = returnItem.Qty;
                        range.Merge = true;
                        range.Style.Font.Size = 14;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 26, row, column + 28]) {
                        range.Value = returnItem.OrderItem.Product.MeasureUnit.Name;
                        range.Merge = true;
                        range.Style.Font.Size = 11;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 29, row, column + 32]) {
                        range.Value = currentPrice;
                        range.Merge = true;
                        range.Style.Font.Size = 11;
                        range.Style.Numberformat.Format = "0.00";
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 33, row, column + 36]) {
                        range.Value = range.Value = decimal.Round(currentPrice * Convert.ToDecimal(returnItem.Qty), 2, MidpointRounding.AwayFromZero);
                        range.Merge = true;
                        range.Style.Font.Size = 11;
                        range.Style.Numberformat.Format = "0.00";
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    worksheet.SetRowHeight(27.60, row);

                    row++;
                    indexer++;
                }
            }

            worksheet.SetRowHeight(6.60, row);
            row++;

            worksheet.SetRowHeight(15, row);

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 12]) {
                range.Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                range.Style.Font.Size = 6;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 29, row, column + 31]) {
                range.Value = "Разом:";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 33, row, column + 36]) {
                range.Value = saleReturn.TotalAmount;
                range.Style.Font.Size = 11;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Font.Bold = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            worksheet.SetRowHeight(6.60, row + 1);

            row += 2;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 36]) {
                string amountValue = $"Всього найменувань {indexer - 1}, на суму {decimal.Round(saleReturn.TotalAmount, 2)} ";

                switch (saleReturn.Currency?.Code ?? "") {
                    case "UAH":
                        amountValue += "грн.";
                        break;
                    case "PLN":
                        amountValue += "злт.";
                        break;
                    case "USD":
                        amountValue += "дол.";
                        break;
                    case "EUR":
                    default:
                        amountValue += "євро";
                        break;
                }

                range.Value = amountValue;
                range.Merge = true;
                range.Style.Font.Size = 8;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            row++;

            string totalAmount = saleReturn.TotalAmount.ToText(true, true);

            int fullNumber = Convert.ToInt32(Math.Truncate(saleReturn.TotalAmount));
            int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

            string endKeyWord;

            switch (saleReturn.Currency?.Code ?? "") {
                case "UAH":
                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "гривень";
                    else
                        switch (endNumber) {
                            case 1:
                                endKeyWord = "гривня";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "гривні";
                                break;
                            default:
                                endKeyWord = "гривень";
                                break;
                        }

                    break;
                case "PLN":
                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "злотих";
                    else
                        switch (endNumber) {
                            case 1:
                                endKeyWord = "злотий";
                                break;
                            case 2:
                            case 3:
                            case 4:
                            default:
                                endKeyWord = "злотих";
                                break;
                        }

                    break;
                case "USD":
                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "доларів";
                    else
                        switch (endNumber) {
                            case 1:
                                endKeyWord = "доллар";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "доллара";
                                break;
                            default:
                                endKeyWord = "доларів";
                                break;
                        }

                    break;
                case "EUR":
                default:
                    endKeyWord = "євро";
                    break;
            }

            totalAmount += $" {endKeyWord} {decimal.Round(decimal.Round(saleReturn.TotalAmount % 1, 2) * 100, 0)} ";

            int fullNumberDecimals = Convert.ToInt32(Math.Round(saleReturn.TotalAmount % 1, 2) * 100);
            int endNumberDecimals = Convert.ToInt32(fullNumberDecimals.ToString().Last().ToString());

            switch (saleReturn.Currency?.Code ?? "") {
                case "UAH":
                    if (fullNumberDecimals > 10 && fullNumberDecimals < 20)
                        endKeyWord = "копійок";
                    else
                        switch (endNumberDecimals) {
                            case 1:
                                endKeyWord = "копійка";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "копійки";
                                break;
                            default:
                                endKeyWord = "копійок";
                                break;
                        }

                    break;
                case "PLN":
                    if (fullNumberDecimals > 10 && fullNumberDecimals < 20)
                        endKeyWord = "грошів";
                    else
                        switch (endNumberDecimals) {
                            case 1:
                                endKeyWord = "грош";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "гроша";
                                break;
                            default:
                                endKeyWord = "грошів";
                                break;
                        }

                    break;
                case "USD":
                    if (fullNumberDecimals > 10 && fullNumberDecimals < 20)
                        endKeyWord = "центів";
                    else
                        switch (endNumberDecimals) {
                            case 1:
                                endKeyWord = "цент";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "цента";
                                break;
                            default:
                                endKeyWord = "центів";
                                break;
                        }

                    break;
                case "EUR":
                default:
                    endKeyWord = "центів";
                    break;
            }

            totalAmount += endKeyWord;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 36]) {
                range.Value = totalAmount;
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            worksheet.SetRowHeight(6.60, ++row);

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 36]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            row += 2;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 5]) {
                range.Value = "Відвантажив(ла):";
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 9;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 6, row, column + 14]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 16, row, column + 20]) {
                range.Value = "Отримав(ла):";
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 9;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 21, row, column + 35]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsFile, string pdfFile) ExportUkSaleReturnToXlsxFromVatSales(string path, SaleReturn saleReturn, IEnumerable<DocumentMonth> months) {
        string fileName = Path.Combine(path, $"{saleReturn.Number}_{DateTime.Now.ToString("MM.yyyy")}_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Повернення");

            //Set printer settings
            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.Cells.Style.Font.Name = "Arial";
            worksheet.Cells.Style.Font.Size = 8;
            worksheet.DefaultRowHeight = 10.20;

            worksheet.SetColumnWidth(0.67, 1);
            worksheet.SetColumnWidth(3.5, 2);
            worksheet.SetColumnWidth(2.99, 3, 38);
            worksheet.SetColumnWidth(12, 8);
            worksheet.SetColumnWidth(12, 23);
            worksheet.SetColumnWidth(12, 24);

            int column = 2;
            int row = 2;

            worksheet.SetRowHeight(21, row);

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 36]) {
                range.Value =
                    string.Format(
                        "Накладна на повернення від покупця № {0} від {1} {2} {3} р.",
                        Convert.ToInt32(
                            !string.IsNullOrEmpty(saleReturn.Sale.ClientAgreement.Agreement.Organization.Code)
                                ? saleReturn.Number.Substring(
                                    saleReturn.Sale.ClientAgreement.Agreement.Organization.Code.Length,
                                    saleReturn.Number.Length - saleReturn.Sale.ClientAgreement.Agreement.Organization.Code.Length
                                )
                                : saleReturn.Number
                        ),
                        saleReturn.FromDate.Day,
                        months.FirstOrDefault(m => m.Number == saleReturn.FromDate.Month)?.Name.ToLower() ?? string.Empty,
                        saleReturn.FromDate.Year
                    );
                range.Merge = true;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            row += 2;

            worksheet.SetRowHeight(12.60, row);

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 4]) {
                range.Value = "Постачальник:";
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.Font.UnderLine = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 5, row, column + 36]) {
                range.Value = saleReturn.Sale.ClientAgreement.Agreement.Organization.Name;
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, column + 5, row, column + 36]) {
                PaymentRegister register = saleReturn.Sale.ClientAgreement.Agreement.Organization.PaymentRegisters.FirstOrDefault();

                string completeSupplierInfo =
                    string.Format(
                        "П/р {0}, у банку {1}, {2}, МФО {3},\r\n{4}, тел.: {5},\r\nкод за ЄДРПОУ {6}, ІПН {7},\r\nЄ платником податку на загальних підставах",
                        register?.AccountNumber ?? "",
                        register?.BankName ?? "",
                        register?.City ?? "",
                        register?.SortCode ?? "",
                        saleReturn.Sale.ClientAgreement.Agreement.Organization.Address,
                        saleReturn.Sale.ClientAgreement.Agreement.Organization.PhoneNumber,
                        saleReturn.Sale.ClientAgreement.Agreement.Organization.USREOU,
                        saleReturn.Sale.ClientAgreement.Agreement.Organization.TIN
                    );

                range.Value = completeSupplierInfo;
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            worksheet.SetRowHeight(50, row);

            worksheet.SetRowHeight(3.75, row + 1);

            row += 2;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 3]) {
                range.Value = "Покупець:";
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 5, row, column + 36]) {
                range.Value = saleReturn.Client.FullName;
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, column + 5, row, column + 36]) {
                string clientInfo = string.Empty;
                if (saleReturn.Client.RegionCode != null) {
                    if (!string.IsNullOrEmpty(saleReturn.Client.RegionCode.City)) clientInfo += $"{saleReturn.Client.RegionCode.City}, ";

                    if (!string.IsNullOrEmpty(saleReturn.Client.RegionCode.District)) clientInfo += $"{saleReturn.Client.RegionCode.District}, ";
                }

                if (!string.IsNullOrEmpty(saleReturn.Client.ActualAddress)) clientInfo += $"{saleReturn.Client.ActualAddress}, ";

                if (!string.IsNullOrEmpty(saleReturn.Client.MobileNumber)) clientInfo += $"тел.: {saleReturn.Client.MobileNumber}";

                range.Value = clientInfo;
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            worksheet.SetRowHeight(3.60, row + 1);

            row += 2;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 3]) {
                range.Value = "Договір:";
                range.Merge = true;
                range.Style.Font.Size = 10;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 4, row, column + 36]) {
                range.Value =
                    string.Format(
                        "№ {0} від {1}",
                        saleReturn.Sale.ClientAgreement.Agreement.Number,
                        saleReturn.Sale.ClientAgreement.Agreement.Created.ToString("dd.MM.yyyy")
                    );
                range.Merge = true;
                range.Style.Font.Size = 10;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            worksheet.SetRowHeight(3.60, row + 1);

            row += 2;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 3]) {
                range.Value = "Склад:";
                range.Merge = true;
                range.Style.Font.Size = 10;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 4, row, column + 36]) {
                range.Value = saleReturn.Storage.Name;
                range.Merge = true;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            worksheet.SetRowHeight(3.60, row + 1);

            row += 2;

            //Table Header
            using (ExcelRange range = worksheet.Cells[row, column, row, column + 1]) {
                range.Value = "№";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 2, row, column + 9]) {
                range.Value = "Артикул";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 10, row, column + 13]) {
                range.Value = "Розм.";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 14, row, column + 22]) {
                range.Value = "Товар";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 23, row, column + 28]) {
                range.Value = "Кількість";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 29, row, column + 32]) {
                range.Value = "Ціна з ПДВ";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 33, row, column + 36]) {
                range.Value = "Сума з ПДВ";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            worksheet.SetRowHeight(27.60, row);

            row++;

            int indexer = 1;

            decimal vatAmount = 0m;
            decimal exchangeRate = 1m;

            foreach (SaleReturnItem returnItem in saleReturn.SaleReturnItems) {
                decimal localAmount = decimal.Round(returnItem.Amount / Convert.ToDecimal(returnItem.Qty), 2, MidpointRounding.AwayFromZero);
                if (returnItem.SaleReturnItemProductPlacements.Any()) {
                    foreach (SaleReturnItemProductPlacement item in returnItem.SaleReturnItemProductPlacements) {
                        exchangeRate = returnItem.ExchangeRateAmount;
                        vatAmount += returnItem.VatAmount;
                        //decimal.Round(
                        //    vatAmount +
                        //    decimal.Round(
                        //        returnItem.OrderItem.PricePerItem * Convert.ToDecimal(returnItem.Qty),
                        //        2,
                        //        MidpointRounding.AwayFromZero
                        //    ) * 100 / 120,
                        //    14,
                        //    MidpointRounding.AwayFromZero
                        //);

                        using (ExcelRange range = worksheet.Cells[row, column, row, column + 1]) {
                            range.Value = indexer;
                            range.Merge = true;
                            range.Style.Font.Size = 11;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        using (ExcelRange range = worksheet.Cells[row, column + 2, row, column + 9]) {
                            range.Value = returnItem.OrderItem.Product.VendorCode;
                            range.Merge = true;
                            range.Style.Font.Size = 14;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        string placement = string.Empty;

                        using (ExcelRange range = worksheet.Cells[row, column + 10, row, column + 13]) {
                            range.Style.WrapText = true;
                            if (returnItem.SaleReturnItemProductPlacements != null)
                                range.Value = item.ProductPlacement.StorageNumber + "-" + item.ProductPlacement.RowNumber + "-" + item.ProductPlacement.CellNumber + " ";
                            else
                                range.Value = "";
                            range.Merge = true;
                            range.Style.Font.Size = 11;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        using (ExcelRange range = worksheet.Cells[row, column + 14, row, column + 22]) {
                            range.Value = returnItem.OrderItem.Product.NameUA;
                            range.Merge = true;
                            range.Style.Font.Size = 11;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        using (ExcelRange range = worksheet.Cells[row, column + 23, row, column + 25]) {
                            range.Value = item.Qty;
                            range.Merge = true;
                            range.Style.Font.Size = 14;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        using (ExcelRange range = worksheet.Cells[row, column + 26, row, column + 28]) {
                            range.Value = returnItem.OrderItem.Product.MeasureUnit.Name;
                            range.Merge = true;
                            range.Style.Font.Size = 11;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        using (ExcelRange range = worksheet.Cells[row, column + 29, row, column + 32]) {
                            range.Value = localAmount;
                            range.Merge = true;
                            range.Style.Font.Size = 11;
                            range.Style.Numberformat.Format = "0.00";
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        using (ExcelRange range = worksheet.Cells[row, column + 33, row, column + 36]) {
                            range.Value = decimal.Round(localAmount * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);
                            range.Merge = true;
                            range.Style.Font.Size = 11;
                            range.Style.Numberformat.Format = "0.00";
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        worksheet.SetRowHeight(27.60, row);

                        row++;
                        indexer++;
                    }
                } else {
                    exchangeRate = returnItem.ExchangeRateAmount;
                    vatAmount += returnItem.VatAmount;
                    //decimal.Round(
                    //    vatAmount +
                    //    decimal.Round(
                    //        returnItem.OrderItem.PricePerItem * Convert.ToDecimal(returnItem.Qty),
                    //        2,
                    //        MidpointRounding.AwayFromZero
                    //    ) * 100 / 120,
                    //    14,
                    //    MidpointRounding.AwayFromZero
                    //);

                    using (ExcelRange range = worksheet.Cells[row, column, row, column + 1]) {
                        range.Value = indexer;
                        range.Merge = true;
                        range.Style.Font.Size = 11;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 2, row, column + 9]) {
                        range.Value = returnItem.OrderItem.Product.VendorCode;
                        range.Merge = true;
                        range.Style.Font.Size = 14;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    string placement = string.Empty;
                    foreach (SaleReturnItemProductPlacement item in returnItem.SaleReturnItemProductPlacements)
                        placement += item.ProductPlacement.StorageNumber + "-" + item.ProductPlacement.RowNumber + "-" + item.ProductPlacement.CellNumber + " ";
                    using (ExcelRange range = worksheet.Cells[row, column + 10, row, column + 13]) {
                        range.Style.WrapText = true;
                        if (returnItem.SaleReturnItemProductPlacements != null)
                            range.Value = placement;
                        else
                            range.Value = "";
                        range.Merge = true;
                        range.Style.Font.Size = 11;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 14, row, column + 22]) {
                        range.Value = returnItem.OrderItem.Product.NameUA;
                        range.Merge = true;
                        range.Style.Font.Size = 11;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 23, row, column + 25]) {
                        range.Value = returnItem.Qty;
                        range.Merge = true;
                        range.Style.Font.Size = 14;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 26, row, column + 28]) {
                        range.Value = returnItem.OrderItem.Product.MeasureUnit.Name;
                        range.Merge = true;
                        range.Style.Font.Size = 11;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 29, row, column + 32]) {
                        range.Value = decimal.Round(returnItem.Amount / Convert.ToDecimal(returnItem.Qty), 2, MidpointRounding.AwayFromZero);
                        range.Merge = true;
                        range.Style.Font.Size = 11;
                        range.Style.Numberformat.Format = "0.00";
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 33, row, column + 36]) {
                        range.Value = returnItem.Amount;
                        range.Merge = true;
                        range.Style.Font.Size = 11;
                        range.Style.Numberformat.Format = "0.00";
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    worksheet.SetRowHeight(27.60, row);

                    row++;
                    indexer++;
                }
            }

            worksheet.SetRowHeight(6.60, row);
            row++;

            worksheet.SetRowHeight(15, row);

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 12]) {
                range.Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                range.Style.Font.Size = 6;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 29, row, column + 31]) {
                range.Value = "Разом:";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 33, row, column + 36]) {
                range.Value = saleReturn.TotalAmount;
                range.Style.Font.Size = 11;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Font.Bold = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, column + 20, row, column + 26]) {
                range.Value = "У тому числі ПДВ:";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 33, row, column + 36]) {
                range.Value = vatAmount;
                range.Style.Font.Size = 11;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Font.Bold = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            worksheet.SetRowHeight(6.60, row + 1);

            row += 2;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 36]) {
                string amountValue = $"Всього найменувань {indexer - 1}, на суму {decimal.Round(saleReturn.TotalAmount, 2)} ";

                switch (saleReturn.Currency?.Code ?? "") {
                    case "UAH":
                        amountValue += "грн.";
                        break;
                    case "PLN":
                        amountValue += "злт.";
                        break;
                    case "USD":
                        amountValue += "дол.";
                        break;
                    case "EUR":
                    default:
                        amountValue += "євро";
                        break;
                }

                range.Value = amountValue;
                range.Merge = true;
                range.Style.Font.Size = 8;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            row++;

            string totalAmount = saleReturn.TotalAmount.ToText(true, true);

            int fullNumber = Convert.ToInt32(Math.Truncate(saleReturn.TotalAmount));
            int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

            string endKeyWord;

            switch (saleReturn.Currency?.Code ?? "") {
                case "UAH":
                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "гривень";
                    else
                        switch (endNumber) {
                            case 1:
                                endKeyWord = "гривня";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "гривні";
                                break;
                            default:
                                endKeyWord = "гривень";
                                break;
                        }

                    break;
                case "PLN":
                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "злотих";
                    else
                        switch (endNumber) {
                            case 1:
                                endKeyWord = "злотий";
                                break;
                            case 2:
                            case 3:
                            case 4:
                            default:
                                endKeyWord = "злотих";
                                break;
                        }

                    break;
                case "USD":
                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "доларів";
                    else
                        switch (endNumber) {
                            case 1:
                                endKeyWord = "доллар";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "доллара";
                                break;
                            default:
                                endKeyWord = "доларів";
                                break;
                        }

                    break;
                case "EUR":
                default:
                    endKeyWord = "євро";
                    break;
            }

            totalAmount += $" {endKeyWord} {decimal.Round(decimal.Round(saleReturn.TotalAmount % 1, 2) * 100, 0)} ";

            int fullNumberDecimals = Convert.ToInt32(Math.Round(saleReturn.TotalAmount % 1, 2) * 100);
            int endNumberDecimals = Convert.ToInt32(fullNumberDecimals.ToString().Last().ToString());

            switch (saleReturn.Currency?.Code ?? "") {
                case "UAH":
                    if (fullNumberDecimals > 10 && fullNumberDecimals < 20)
                        endKeyWord = "копійок";
                    else
                        switch (endNumberDecimals) {
                            case 1:
                                endKeyWord = "копійка";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "копійки";
                                break;
                            default:
                                endKeyWord = "копійок";
                                break;
                        }

                    break;
                case "PLN":
                    if (fullNumberDecimals > 10 && fullNumberDecimals < 20)
                        endKeyWord = "грошів";
                    else
                        switch (endNumberDecimals) {
                            case 1:
                                endKeyWord = "грош";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "гроша";
                                break;
                            default:
                                endKeyWord = "грошів";
                                break;
                        }

                    break;
                case "USD":
                    if (fullNumberDecimals > 10 && fullNumberDecimals < 20)
                        endKeyWord = "центів";
                    else
                        switch (endNumberDecimals) {
                            case 1:
                                endKeyWord = "цент";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "цента";
                                break;
                            default:
                                endKeyWord = "центів";
                                break;
                        }

                    break;
                case "EUR":
                default:
                    endKeyWord = "центів";
                    break;
            }

            totalAmount += endKeyWord;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 36]) {
                range.Value = totalAmount;
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            row++;

            totalAmount = $"У т.ч. ПДВ: {vatAmount.ToText(true, true)}";

            fullNumber = Convert.ToInt32(Math.Truncate(vatAmount));
            endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

            switch (saleReturn.Currency?.Code ?? "") {
                case "UAH":
                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "гривень";
                    else
                        switch (endNumber) {
                            case 1:
                                endKeyWord = "гривня";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "гривні";
                                break;
                            default:
                                endKeyWord = "гривень";
                                break;
                        }

                    break;
                case "PLN":
                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "злотих";
                    else
                        switch (endNumber) {
                            case 1:
                                endKeyWord = "злотий";
                                break;
                            case 2:
                            case 3:
                            case 4:
                            default:
                                endKeyWord = "злотих";
                                break;
                        }

                    break;
                case "USD":
                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "доларів";
                    else
                        switch (endNumber) {
                            case 1:
                                endKeyWord = "доллар";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "доллара";
                                break;
                            default:
                                endKeyWord = "доларів";
                                break;
                        }

                    break;
                case "EUR":
                default:
                    endKeyWord = "євро";
                    break;
            }

            totalAmount += $" {endKeyWord} {decimal.Round(decimal.Round(vatAmount % 1, 2) * 100, 0)} ";

            fullNumberDecimals = Convert.ToInt32(Math.Round(vatAmount % 1, 2) * 100);
            endNumberDecimals = Convert.ToInt32(fullNumberDecimals.ToString().Last().ToString());

            switch (saleReturn.Currency?.Code ?? "") {
                case "UAH":
                    if (fullNumberDecimals > 10 && fullNumberDecimals < 20)
                        endKeyWord = "копійок";
                    else
                        switch (endNumberDecimals) {
                            case 1:
                                endKeyWord = "копійка";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "копійки";
                                break;
                            default:
                                endKeyWord = "копійок";
                                break;
                        }

                    break;
                case "PLN":
                    if (fullNumberDecimals > 10 && fullNumberDecimals < 20)
                        endKeyWord = "грошів";
                    else
                        switch (endNumberDecimals) {
                            case 1:
                                endKeyWord = "грош";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "гроша";
                                break;
                            default:
                                endKeyWord = "грошів";
                                break;
                        }

                    break;
                case "USD":
                    if (fullNumberDecimals > 10 && fullNumberDecimals < 20)
                        endKeyWord = "центів";
                    else
                        switch (endNumberDecimals) {
                            case 1:
                                endKeyWord = "цент";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                endKeyWord = "цента";
                                break;
                            default:
                                endKeyWord = "центів";
                                break;
                        }

                    break;
                case "EUR":
                default:
                    endKeyWord = "центів";
                    break;
            }

            totalAmount += endKeyWord;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 36]) {
                range.Value = totalAmount;
                range.Merge = true;
                range.Style.Font.Size = 9;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            worksheet.SetRowHeight(6.60, ++row);

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 36]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            row += 2;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 5]) {
                range.Value = "Відвантажив(ла):";
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 9;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 6, row, column + 14]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 16, row, column + 20]) {
                range.Value = "Отримав(ла):";
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 9;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 21, row, column + 35]) {
                if (saleReturn.ClientAgreement?.Agreement?.Organization?.Name != "ТОВ «АМГ «КОНКОРД»")
                    range.Value = $"{saleReturn.CreatedBy.LastName} {saleReturn.CreatedBy.FirstName} {saleReturn.CreatedBy.MiddleName}";
                else
                    range.Value = "";
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Style.Font.Size = 8;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportSupplyReturnToXlsx(string path, SupplyReturn supplyReturn) {
        string fileName = Path.Combine(path, $"SupplyReturn_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        const string documentName = "Повернення постачальнику";

        string currencyCode;

        try {
            currencyCode = supplyReturn.ClientAgreement.Agreement.ProviderPricing.Currency.Code;
        } catch {
            currencyCode = "EUR";
        }

        const string informationAboutOrganization = "Не є платником податку на прибуток на загальних підставах";

        bool isValidRetrieveData = supplyReturn?.Supplier != null && supplyReturn.Organization != null && supplyReturn.ClientAgreement != null;

        if (!isValidRetrieveData) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("SupplyReturn Document");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Setting default width to columns
            worksheet.SetColumnWidth(1.7143, 1);
            worksheet.SetColumnWidth(3, 2);
            worksheet.SetColumnWidth(3, 3);
            worksheet.SetColumnWidth(3.5714, 4);
            worksheet.SetColumnWidth(3.2857, 5);
            worksheet.SetColumnWidth(3.2857, 6);
            worksheet.SetColumnWidth(3, 7);
            worksheet.SetColumnWidth(3, 8);
            worksheet.SetColumnWidth(2.1428, 9);
            worksheet.SetColumnWidth(2.1428, 10);
            worksheet.SetColumnWidth(3, 11);
            worksheet.SetColumnWidth(3, 12);
            worksheet.SetColumnWidth(3, 13);
            worksheet.SetColumnWidth(2.4285, 14);
            worksheet.SetColumnWidth(2.4285, 15);
            worksheet.SetColumnWidth(2.4285, 16);
            worksheet.SetColumnWidth(3, 17);
            worksheet.SetColumnWidth(2, 18);
            worksheet.SetColumnWidth(2, 19);
            worksheet.SetColumnWidth(2.4285, 20);
            worksheet.SetColumnWidth(1.8571, 21);
            worksheet.SetColumnWidth(2.1428, 22);
            worksheet.SetColumnWidth(2.5714, 23);
            worksheet.SetColumnWidth(2.2857, 24);
            worksheet.SetColumnWidth(2.2857, 25);
            worksheet.SetColumnWidth(2.2857, 26);
            worksheet.SetColumnWidth(2.2857, 27);
            worksheet.SetColumnWidth(2.5714, 28);
            worksheet.SetColumnWidth(2.5714, 29);
            worksheet.SetColumnWidth(2.2857, 30);
            worksheet.SetColumnWidth(3, 31);
            worksheet.SetColumnWidth(3, 32);
            worksheet.SetColumnWidth(3, 33);
            worksheet.SetColumnWidth(3, 34);
            worksheet.SetColumnWidth(3.8571, 35);
            worksheet.SetColumnWidth(3.8571, 36);
            worksheet.SetColumnWidth(3.8571, 37);
            worksheet.SetColumnWidth(3.8571, 38);

            //Document header

            //Setting document header height
            worksheet.SetRowHeight(11.3636, 1);
            worksheet.SetRowHeight(18.9394, 2);
            worksheet.SetRowHeight(11.3636, 3);
            worksheet.SetRowHeight(12.8788, 4);
            worksheet.SetRowHeight(12.1212, 5);
            worksheet.SetRowHeight(3.7879, 6);
            worksheet.SetRowHeight(12.8788, 7);
            worksheet.SetRowHeight(11.3636, 8);
            worksheet.SetRowHeight(3.7879, 9);
            worksheet.SetRowHeight(12.8788, 10);
            worksheet.SetRowHeight(3.7879, 11);
            worksheet.SetRowHeight(11.3636, 12);
            worksheet.SetRowHeight(11.3636, 13);

            using (ExcelRange range = worksheet.Cells[2, 2, 2, 38]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.Font.Name = "Arial";
                range.Value = string.Format("{0} № {1} від {2} р.",
                    documentName, supplyReturn.Number, supplyReturn.FromDate.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("uk-UA")));
            }

            using (ExcelRange range = worksheet.Cells[2, 2, 2, 38]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[4, 2, 4, 6]) {
                range.Merge = true;
                range.Value = "Постачальник: ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[4, 7, 4, 38]) {
                range.Merge = true;
                range.Value = supplyReturn.Supplier.Name;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[7, 2, 7, 6]) {
                range.Merge = true;
                range.Value = "Покупець: ";
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[7, 7, 7, 38]) {
                range.Merge = true;
                range.Value = supplyReturn.Organization.Name;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[8, 8, 8, 38]) {
                range.Merge = true;
                range.Value = informationAboutOrganization;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
            }

            if (!string.IsNullOrEmpty(supplyReturn.ClientAgreement.Agreement.Number)
                && !(supplyReturn.ClientAgreement.Agreement.FromDate == null || supplyReturn.ClientAgreement.Agreement.ToDate == null)) {
                using (ExcelRange range = worksheet.Cells[10, 2, 10, 5]) {
                    range.Merge = true;
                    range.Value = "Договір: ";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                    range.Style.Font.Name = "Arial";
                    range.Style.Font.Size = 10;
                    range.Style.WrapText = true;
                }

                using (ExcelRange range = worksheet.Cells[10, 6, 10, 38]) {
                    range.Merge = true;
                    range.Value = string.Format("{0} від {1}", supplyReturn.ClientAgreement.Agreement.Number, supplyReturn.ClientAgreement.Agreement.Created);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                    range.Style.Font.Name = "Arial";
                    range.Style.Font.Size = 10;
                    range.Style.WrapText = true;
                }
            }

            using (ExcelRange range = worksheet.Cells[12, 2, 13, 3]) {
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

            using (ExcelRange range = worksheet.Cells[12, 4, 13, 6]) {
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

            using (ExcelRange range = worksheet.Cells[12, 7, 13, 26]) {
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

            using (ExcelRange range = worksheet.Cells[12, 27, 13, 31]) {
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

            using (ExcelRange range = worksheet.Cells[12, 32, 13, 34]) {
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

            using (ExcelRange range = worksheet.Cells[12, 35, 13, 38]) {
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

            using (ExcelRange range = worksheet.Cells[12, 2, 13, 38]) {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(238, 238, 238));
            }

            int row = 14;

            int counter = 1;

            foreach (SupplyReturnItem supplyReturnItem in supplyReturn.SupplyReturnItems) {
                worksheet.SetRowHeight(12.12121, row);

                if (counter != supplyReturn.SupplyReturnItems.Count) {
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
                        range.Value = supplyReturnItem.Product.VendorCode;
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
                        range.Value = supplyReturnItem.Product.Name;
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
                        if (supplyReturnItem.Qty != 0)
                            range.Value = supplyReturnItem.Qty;
                        else
                            range.Value = "";
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

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                        range.Merge = true;
                        range.Value = supplyReturnItem.Product.MeasureUnit.Name;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 32, row, 34]) {
                        range.Merge = true;
                        if (supplyReturnItem.ConsignmentItem.Price != 0)
                            range.Value = supplyReturnItem.ConsignmentItem.Price;
                        else
                            range.Value = "";
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 35, row, 38]) {
                        range.Merge = true;
                        if (supplyReturnItem.TotalNetPrice != 0)
                            range.Value = supplyReturnItem.TotalNetPrice;
                        else
                            range.Value = "";
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Numberformat.Format = "0.00";
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
                        range.Value = supplyReturnItem.Product.VendorCode;
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
                        range.Value = supplyReturnItem.Product.Name;
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
                        if (supplyReturnItem.Qty != 0)
                            range.Value = supplyReturnItem.Qty;
                        else
                            range.Value = "";
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

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 31]) {
                        range.Merge = true;
                        range.Value = supplyReturnItem.Product.MeasureUnit.Name;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 32, row, 34]) {
                        range.Merge = true;
                        if (supplyReturnItem.ConsignmentItem.Price != 0)
                            range.Value = supplyReturnItem.ConsignmentItem.Price;
                        else
                            range.Value = "";
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 35, row, 38]) {
                        range.Merge = true;
                        if (supplyReturnItem.TotalNetPrice != 0)
                            range.Value = supplyReturnItem.TotalNetPrice;
                        else
                            range.Value = "";
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Medium;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                        range.Style.Numberformat.Format = "0.00";
                    }
                }

                row++;
                counter++;
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
            worksheet.SetRowHeight(11.3636, row + 9);
            worksheet.SetRowHeight(21.2121, row + 10);
            worksheet.SetRowHeight(11.3636, row + 11);

            using (ExcelRange range = worksheet.Cells[row + 1, 34, row + 1, 34]) {
                range.Style.Font.Name = "Arial";
                range.Value = "Разом:";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row + 1, 35, row + 1, 38]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Value = supplyReturn.TotalNetPrice;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row + 3, 2, row + 3, 38]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Value = string.Format("Всього найменувань {0}, на суму {1} {2}.",
                    supplyReturn.SupplyReturnItems.Count, string.Format("{0:0,0.00}", supplyReturn.TotalNetPrice), currencyCode);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 4, 2, row + 4, 38]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Value = supplyReturn.TotalNetPrice.ToCompleteText(currencyCode, false, true, true);
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row + 5, 2, row + 5, 38]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[row + 7, 2, row + 7, 18]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Від покупця* ";
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 7, 21, row + 7, 38]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Отримав(ла) ";
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 8, 2, row + 8, 18]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 8, 20, row + 8, 38]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 10, 2, row + 10, 18]) {
                range.Merge = true;
                range.Value = "* Відповідальний за здійснення господарської операції і правильність її оформлення";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[row + 10, 21, row + 10, 21]) {
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "За довіреністю";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row + 10, 30, row + 10, 30]) {
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "№";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }


            using (ExcelRange range = worksheet.Cells[row + 10, 35, row + 10, 35]) {
                range.Value = "від";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
            }

            package.Workbook.Properties.Title = "Tax Free Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportSaleReturnDetailReportToXlsx(string path, List<Client> clients) {
        string fileName = Path.Combine(path, $"SaleReturnDetailReport_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        bool isValidRetrieveData = clients != null;

        if (!isValidRetrieveData) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Sale Return Detail Report");

            worksheet.OutLineSummaryBelow = false;

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Setting default width to columns
            worksheet.SetColumnWidth(19.5714, 1);
            worksheet.SetColumnWidth(10.4286, 2);
            worksheet.SetColumnWidth(17, 3);
            worksheet.SetColumnWidth(17, 4);
            worksheet.SetColumnWidth(25.7143, 5);
            worksheet.SetColumnWidth(7.7143, 6);
            worksheet.SetColumnWidth(7.7143, 7);
            worksheet.SetColumnWidth(17.8571, 8);
            worksheet.SetColumnWidth(17.5714, 9);

            //Document header

            //Setting document header height
            worksheet.SetRowHeight(0, 1);
            worksheet.SetRowHeight(0, 2);
            worksheet.SetRowHeight(0, 3);
            worksheet.SetRowHeight(12.1212, 4);

            using (ExcelRange range = worksheet.Cells[4, 1, 4, 1]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Value = "Контрагент";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[4, 2, 4, 2]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Value = "Код";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[4, 3, 4, 3]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Value = "Дата повернення";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[4, 4, 4, 4]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Value = "Артикул";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[4, 5, 4, 5]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Value = "Назва";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[4, 6, 4, 6]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Value = "К-сть";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[4, 7, 4, 7]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Value = "Ціна";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[4, 8, 4, 8]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Value = "Причина";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[4, 9, 4, 9]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Value = "Примітки";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[4, 1, 4, 9]) {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 236, 197));
            }

            int row = 5;

            foreach (Client client in clients) {
                double totalQuantity = 0;

                decimal totalAmount = 0;

                foreach (SaleReturn saleReturn in client.SaleReturns)
                foreach (SaleReturnItem saleReturnItem in saleReturn.SaleReturnItems) {
                    totalQuantity += saleReturnItem.Qty;

                    totalAmount += saleReturnItem.Amount;
                }

                using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = client.FullName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    range.Style.WrapText = true;
                }

                using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = client.RegionCode?.Value ?? "";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = totalQuantity;
                    range.Style.Font.Bold = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = totalAmount;
                    range.Style.Font.Bold = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 1, row, 9]) {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 236, 197));
                }

                row++;

                foreach (SaleReturn saleReturn in client.SaleReturns)
                foreach (SaleReturnItem saleReturnItem in saleReturn.SaleReturnItems) {
                    using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = saleReturn.FromDate.ToString("dd.MM.yyyy");
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = saleReturnItem.OrderItem.Product.VendorCode;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = saleReturnItem.OrderItem.Product.Name;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                        range.Style.WrapText = true;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = saleReturnItem.Qty;
                        range.Style.Font.Bold = true;
                        range.Style.Numberformat.Format = "0";
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = saleReturnItem.Amount;
                        range.Style.Font.Bold = true;
                        range.Style.Numberformat.Format = "0.00";
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 8, row, 9]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = saleReturnItem.StatusName;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    }


                    using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = saleReturnItem.OrderItem.Comment;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                        range.Style.WrapText = true;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    }

                    ExcelRow excelRow = worksheet.Row(row);

                    excelRow.Collapsed = true;

                    excelRow.OutlineLevel = 1;

                    worksheet.SetRowHeight(12.1212, row);

                    row++;
                }
            }

            package.Workbook.Properties.Title = "SaleReturnDetail Report";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportSaleReturnGroupedByReasonReportToXlsx(
        string path,
        List<SaleReturn> saleReturns,
        List<SaleReturnItemStatusName> reasons,
        Dictionary<SaleReturnItemStatus, double> totalQuantitySaleReturnByReasons) {
        string fileName = Path.Combine(path, $"SaleReturnGroupedByReason_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

        bool isValidRetrieveData = saleReturns != null && reasons != null;

        if (!isValidRetrieveData) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Sale Return Grouped By Reason Report");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Setting default width to columns

            // Setting default width to 1 column
            worksheet.SetColumnWidth(19.4285, 1);

            for (int i = 0; i < reasons.Count; i++) worksheet.SetColumnWidth(27.4286, 2 + i);

            //Setting document header height
            worksheet.SetRowHeight(0, 1);
            worksheet.SetRowHeight(0, 2);
            worksheet.SetRowHeight(0, 3);
            worksheet.SetRowHeight(10.6060, 4);
            worksheet.SetRowHeight(12.1212, 5);

            using (ExcelRange range = worksheet.Cells[4, 1, 5, 1]) {
                range.Merge = true;
                range.Value = "Дата";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            int column = 2;

            foreach (SaleReturnItemStatusName reason in reasons) {
                using (ExcelRange range = worksheet.Cells[4, column, 5, column]) {
                    range.Merge = true;
                    range.Value = culture == "uk" ? reason.NameUK : reason.NamePL;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                column++;
            }

            using (ExcelRange range = worksheet.Cells[4, column, 5, column]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Value = "Висновки";
                range.Style.Font.Name = "Arial";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 236, 197));
            }

            int nextColumn = 1 + reasons.Count;

            using (ExcelRange range = worksheet.Cells[4, 1, 5, nextColumn]) {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 236, 197));
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
            }

            int row = 6;

            foreach (SaleReturn saleReturn in saleReturns) {
                column = 1;

                double totalQuantity = 0;

                using (ExcelRange range = worksheet.Cells[row, column, row, column]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    range.Value = saleReturn.FromDate.ToString("dd.MM.yyyy");
                }

                foreach (SaleReturnItemStatusName reason in reasons) {
                    column++;
                    using ExcelRange range = worksheet.Cells[row, column, row, column];
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    if (saleReturn.SaleReturnItems.Any(x => x.SaleReturnItemStatus == reason.SaleReturnItemStatus)) {
                        double value = saleReturn.SaleReturnItems.FirstOrDefault(x => x.SaleReturnItemStatus == reason.SaleReturnItemStatus)?.Qty ?? 0;
                        totalQuantity += value;
                        range.Value = value;
                    } else {
                        range.Value = "";
                    }

                    range.Style.Numberformat.Format = "0.000";
                }

                column++;

                using (ExcelRange range = worksheet.Cells[row, column, row, column]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Name = "Arial";
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Value = totalQuantity;
                    range.Style.Numberformat.Format = "0.000";
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 236, 197));
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                row++;
            }

            column = 2;

            using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Value = "Висновки";
                range.Style.Font.Name = "Arial";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 236, 197));
            }

            foreach (SaleReturnItemStatusName reason in reasons) {
                using (ExcelRange range = worksheet.Cells[row, column, row, column]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Name = "Arial";
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    if (totalQuantitySaleReturnByReasons.ContainsKey(reason.SaleReturnItemStatus))
                        range.Value = totalQuantitySaleReturnByReasons.FirstOrDefault(x => x.Key == reason.SaleReturnItemStatus).Value;
                    else
                        range.Value = "";
                    range.Style.Numberformat.Format = "0.000";
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 236, 197));
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                column++;
            }

            package.Workbook.Properties.Title = "SaleReturnGroupedByReason Report";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }
}