using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.EntityHelpers;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public sealed class InvoiceXlsManager : BaseXlsManager, IInvoiceXlsManager {
    public (string xlsxFile, string pdfFile) ExportSupplyInvoicePzDocument(string path, SupplyInvoice invoice) {
        string fileName = Path.Combine(path, $"PZ_{invoice.Number}_{DateTime.Now.ToString("MM.yyyy")}_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("PZ");

            //Set printer settings
            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Set column's width
            worksheet.SetColumnWidth(18.01, 1);
            worksheet.SetColumnWidth(37.61, 2);
            worksheet.SetColumnWidth(11.91, 3);
            worksheet.SetColumnWidth(6.01, 4);
            worksheet.SetColumnWidth(8.11, 5);
            worksheet.SetColumnWidth(14.45, 6);
            worksheet.SetColumnWidth(10.51, 7);
            worksheet.SetColumnWidth(5.01, 8);
            worksheet.SetColumnWidth(14.01, 9);
            worksheet.SetColumnWidth(18.11, 10);

            worksheet.SetRowHeight(12.71, new[] { 1, 5, 6, 8, 9, 10 });
            worksheet.SetRowHeight(6.21, new[] { 2, 3 });
            worksheet.SetRowHeight(5.11, new[] { 4 });
            worksheet.SetRowHeight(25.62, new[] { 7 });

            using (ExcelRange range = worksheet.Cells[1, 1, 6, 2]) {
                range.Merge = true;
                range.Value = "(pieczęć)";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[1, 3, 5, 6]) {
                range.Merge = true;
                range.Value = invoice.SupplyOrder?.Client?.FullName;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[6, 3, 6, 6]) {
                range.Merge = true;
                range.Value = "Dostawca";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[1, 7, 2, 8]) {
                range.Merge = true;
                range.Value = "PZ ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[3, 7, 6, 8]) {
                range.Merge = true;
                range.Value = "przyjęcie zewnętrzne";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[1, 9, 1, 9]) {
                range.Merge = true;
                range.Value = "Nr Bieżący";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[2, 9, 3, 9]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[4, 9, 5, 9]) {
                range.Merge = true;
                range.Value = "Nr  magazynowy";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[6, 9, 6, 9]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[1, 10, 1, 10]) {
                range.Merge = true;
                range.Value = "Egz.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[2, 10, 3, 10]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[4, 10, 5, 10]) {
                range.Merge = true;
                range.Value = "Data wystawienia";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[6, 10, 6, 10]) {
                range.Merge = true;
                range.Value = DateTime.Now.ToString("dd/MM/yyyy");
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[7, 1, 7, 1]) {
                range.Merge = true;
                range.Value = "Środek transportu";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[7, 2, 7, 2]) {
                range.Merge = true;
                range.Value = "Zamówienie";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[7, 3, 7, 3]) {
                range.Merge = true;
                range.Value = "Przeznaczenie";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[7, 4, 7, 5]) {
                range.Merge = true;
                range.Value = "Data wysyłki";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[7, 6, 7, 6]) {
                range.Merge = true;
                range.Value = "Data otrzymania";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[7, 7, 7, 10]) {
                range.Merge = true;
                range.Value = "Numer i data faktury - specyfikacji";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[8, 1, 8, 1]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 2, 8, 2]) {
                range.Merge = true;
                range.Value = invoice.Number;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 3, 8, 3]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 4, 8, 5]) {
                range.Merge = true;
                range.Value =
                    invoice.DateFrom.HasValue
                        ? invoice.DateFrom?.ToString("dd.MM.yyyy")
                        : invoice.Created.ToString("dd.MM.yyyy");
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 6, 8, 6]) {
                range.Merge = true;
                range.Value = DateTime.Now.ToString("dd/MM/yyyy");
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 7, 8, 10]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "{0} - {1}",
                        invoice.Number,
                        invoice.DateFrom.HasValue
                            ? invoice.DateFrom?.ToString("dd.MM.yyyy")
                            : invoice.Created.ToString("dd.MM.yyyy")
                    );
                //"PI180629-7 - 09.10.2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[9, 1, 9, 1]) {
                range.Merge = true;
                range.Value = "Kod towaru/ materiału";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[9, 2, 9, 2]) {
                range.Merge = true;
                range.Value = "Nazwa towaru/materiału/opakowania";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[9, 3, 9, 5]) {
                range.Merge = true;
                range.Value = "Ilość";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[9, 6, 9, 6]) {
                range.Merge = true;
                range.Value = "Cena";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[9, 7, 9, 8]) {
                range.Merge = true;
                range.Value = "Wartość";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[9, 9, 9, 9]) {
                range.Merge = true;
                range.Value = "Konto syntet.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[9, 10, 9, 10]) {
                range.Merge = true;
                range.Value = "Zapas";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[10, 1, 10, 1]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[10, 2, 10, 2]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[10, 3, 10, 3]) {
                range.Merge = true;
                range.Value = "Dostarczona";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[10, 4, 10, 4]) {
                range.Merge = true;
                range.Value = "j.m.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[10, 5, 10, 5]) {
                range.Merge = true;
                range.Value = "Przyjęta";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[10, 6, 10, 6]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[10, 7, 10, 8]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[10, 9, 10, 9]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[10, 10, 10, 10]) {
                range.Merge = true;
                range.Value = "Ilość";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            int row = 11;

            List<PackingListPackageOrderItem> items = new();

            invoice.TotalNetPrice = 0m;

            foreach (PackingList packList in invoice.PackingLists)
            foreach (PackingListPackageOrderItem item in packList.PackingListPackageOrderItems) {
                items.Add(item);

                invoice.TotalNetPrice =
                    decimal.Round(
                        invoice.TotalNetPrice + item.TotalNetPrice,
                        2,
                        MidpointRounding.AwayFromZero
                    );
            }

            items = items.OrderBy(i => i.SupplyInvoiceOrderItem.Product.VendorCode).ToList();

            foreach (PackingListPackageOrderItem item in items) {
                using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                    range.Merge = true;
                    range.Value = item.SupplyInvoiceOrderItem.Product.VendorCode;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                    range.Merge = true;
                    range.Value = item.SupplyInvoiceOrderItem.Product.Name;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Merge = true;
                    range.Value = item.Qty;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Numberformat.Format = "0.000";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Merge = true;
                    range.Value = item.SupplyInvoiceOrderItem.Product.MeasureUnit.Name;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Merge = true;
                    range.Value = item.UnitPrice;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 8]) {
                    range.Merge = true;
                    range.Value = item.TotalNetPrice;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                worksheet.SetRowHeight(12.71, row);
                row++;
            }

            using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                range.Merge = true;
                range.Value = items.Sum(i => i.Qty);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Numberformat.Format = "0.000";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                range.Merge = true;
                range.Value = "Razem: ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                range.Merge = true;
                range.Value = invoice.TotalNetPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                range.Merge = true;
                range.Value = invoice.SupplyOrder?.ClientAgreement?.Agreement?.Currency?.Code;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.71, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                range.Merge = true;
                range.Value = "Kurs " + invoice.SupplyOrder?.ClientAgreement?.Agreement?.Currency?.Code;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                range.Merge = true;
                range.Value = invoice.ExchangeRate;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Numberformat.Format = "0.0000";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.71, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Numberformat.Format = "0.000";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                range.Merge = true;
                range.Value = "Razem: ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                range.Merge = true;
                range.Value =
                    decimal.Round(
                        invoice.TotalNetPrice * invoice.ExchangeRate,
                        2,
                        MidpointRounding.AwayFromZero
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                range.Merge = true;
                range.Value = "PLN";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.71, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                range.Merge = true;
                range.Value = "Wystawił";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                range.Merge = true;
                range.Value = "Zatwierdził";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[row, 3, row, 10]) {
                range.Merge = true;
                range.Value = "Wymienione ilości";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#CEFFCE"));
            }

            worksheet.SetRowHeight(12.71, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 3, row, 4]) {
                range.Merge = true;
                range.Value = "Dostarczył";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                range.Merge = true;
                range.Value = "Data";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[row, 6, row, 8]) {
                range.Merge = true;
                range.Value = "Przyjął";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 10]) {
                range.Merge = true;
                range.Value = "Ewidencja ilościowo - wartościowa";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFFFCE"));
            }

            worksheet.SetRowHeight(12.71, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 3, row, 4]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFFFCE"));
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 6, row, 8]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
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

    public (string xlsxFile, string pdfFile) ExportUkInvoiceProductSpecification(string path, SupplyInvoice invoice) {
        string fileName = Path.Combine(path, $"Specification_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Ukrainian");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(4.88, 1);
            worksheet.SetColumnWidth(18.91, 2);
            worksheet.SetColumnWidth(30.24, 3);
            worksheet.SetColumnWidth(7.11, 4);
            worksheet.SetColumnWidth(8.81, 5);
            worksheet.SetColumnWidth(8.97, 6);
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

            using (ExcelRange range = worksheet.Cells[2, 1, 4, 14]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "СПЕЦИФІКАЦІЯ ДО РАХУНКУ-ФАКТУРИ № {0}/{1}/{2}",
                        invoice.Number,
                        string.Format("{0:D2}", (invoice.DateFrom ?? invoice.Created).Month),
                        (invoice.DateFrom ?? invoice.Created).Year
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
                range.Value = $"Ціна {invoice.SupplyOrder.ClientAgreement.Agreement?.Currency?.Code ?? string.Empty}";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 10, 7, 10]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Value = $"Вартість {invoice.SupplyOrder.ClientAgreement.Agreement?.Currency?.Code ?? string.Empty}";
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

            int row = 8;

            int rowIndexer = 1;

            double totalNetWeight = 0d;
            double totalGrossWeight = 0d;
            decimal totalPrice = 0m;

            foreach (SupplyInvoiceOrderItem item in invoice.SupplyInvoiceOrderItems) {
                double netWeight = Math.Round(item.SupplyOrderItem.NetWeight * item.Qty, 3, MidpointRounding.AwayFromZero);
                double grossWeight = Math.Round(item.SupplyOrderItem.GrossWeight * item.Qty, 3, MidpointRounding.AwayFromZero);

                decimal currentUnitPrice = item.UnitPrice;

                string countryCode = invoice.SupplyOrder.ClientAgreement.Client.Country?.Code ?? "";

                string supplierName = invoice.SupplyOrder.ClientAgreement.Client?.SupplierName ?? "";

                string supplierMark = invoice.SupplyOrder.ClientAgreement.Client?.Brand ?? "";

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
                    range.Value = item.ProductSpecification?.SpecificationCode ?? "";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(252, 250, 18));
                }

                using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                    range.Merge = true;
                    range.Value = item.PlProductSpecification?.SpecificationCode ?? "";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                worksheet.SetRowHeight(12.80, row);

                row++;

                rowIndexer++;
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
            package.Workbook.Properties.Title = "Invoice specification";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportSpecificationToXlsx(string path, List<PackingListForSpecification> specifications,
        List<GroupedSpecificationByPackingList> grouped) {
        string fileName = Path.Combine(path, $"specification_{Guid.NewGuid()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            int row = 2;
            int column = 2;

            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Polish");

            //Adding default specification header
            worksheet.Cells[row, 2, row, 3].Merge = true;
            worksheet.Cells[row, 2, row, 3].Value = "Oryginał / Kopia";
            worksheet.Cells[row, 2, row, 3].Style.Font.Name = "Arial";
            worksheet.Cells[row, 2, row, 3].Style.Font.Size = 12;
            worksheet.Cells[row, 2, row, 3].Style.Font.Bold = true;
            worksheet.Cells[row, 2, row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 2, row, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Cells[row, 8, row, 11].Merge = true;
            worksheet.Cells[row, 8, row, 11].Value = DateTime.UtcNow.ToString("dd.MM.yyyy");
            worksheet.Cells[row, 8, row, 11].Style.Font.Name = "Arial";
            worksheet.Cells[row, 8, row, 11].Style.Font.Size = 9;
            worksheet.Cells[row, 8, row, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            worksheet.Cells[row, 8, row, 11].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            row++;

            PackingListForSpecification spec = specifications.FirstOrDefault();

            if (spec != null) {
                worksheet.Cells[row, 2, row + 3, 11].Merge = true;
                worksheet.Cells[row, 2, row + 3, 11].Value =
                    string.Format(
                        "SPECYFIKACJA {0}, {1} OD {2}",
                        spec.Client.FullName,
                        spec.SupplyInvoice.Number,
                        spec.SupplyInvoice.DateFrom.HasValue
                            ? spec.SupplyInvoice.DateFrom?.ToString("dd/MM/yyyy")
                            : spec.SupplyInvoice.Created.ToString("dd/MM/yyyy")
                    );
                worksheet.Cells[row, 2, row + 3, 11].Style.Font.Name = "Arial";
                worksheet.Cells[row, 2, row + 3, 11].Style.Font.Size = 18;
                worksheet.Cells[row, 2, row + 3, 11].Style.Font.Bold = true;
                worksheet.Cells[row, 2, row + 3, 11].Style.WrapText = true;
                worksheet.Cells[row, 2, row + 3, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 2, row + 3, 11].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[row, 2, row + 3, 11].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 2, row + 3, 11].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

                row += 4;

                worksheet.Cells[row, 2, row, 5].Merge = true;
                worksheet.Cells[row, 2, row, 5].Value = "Sprzedawca:";

                worksheet.Cells[row, 6, row, 11].Merge = true;
                worksheet.Cells[row, 6, row, 11].Value = "Nabywca:";

                worksheet.Cells[row, 2, row, 11].Style.Font.Name = "Arial";
                worksheet.Cells[row, 2, row, 11].Style.Font.Size = 12;
                worksheet.Cells[row, 2, row, 11].Style.Font.Bold = true;
                worksheet.Cells[row, 2, row, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[row, 2, row, 11].Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;

                row++;

                PaymentRegister recipientPaymentRegister =
                    spec.Organization.PaymentRegisters.FirstOrDefault();

                worksheet.Cells[row, 2, row + 6, 5].Merge = true;
                worksheet.Cells[row, 2, row + 6, 5].Value =
                    string.Format(
                        "{0},\r\n{1},\r\n{2},\r\n{3} {4}",
                        spec.Client.FullName,
                        spec.Client.ClientBankDetails?.BankAddress ?? string.Empty,
                        spec.Client.ClientBankDetails?.AccountNumber?.AccountNumber ?? string.Empty,
                        spec.Client.ClientBankDetails?.ClientBankDetailIbanNo?.IBANNO ?? string.Empty,
                        spec.Client.ClientBankDetails?.BankAndBranch ?? string.Empty
                    );

                worksheet.Cells[row, 6, row + 6, 11].Merge = true;
                worksheet.Cells[row, 6, row + 6, 11].Value =
                    string.Format(
                        "{0},\r\n{1},\r\n{2},\r\n{3} {4}",
                        spec.Organization.NamePl,
                        spec.Organization.Address,
                        spec.Organization.TIN,
                        recipientPaymentRegister?.IBAN ?? string.Empty,
                        recipientPaymentRegister?.BankName ?? string.Empty
                    );
            }

            worksheet.Cells[row, 2, row + 6, 11].Style.Font.Name = "Arial";
            worksheet.Cells[row, 2, row + 6, 11].Style.Font.Size = 10;
            worksheet.Cells[row, 2, row + 6, 11].Style.WrapText = true;
            worksheet.Cells[row, 2, row + 6, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            worksheet.Cells[row, 2, row + 6, 11].Style.VerticalAlignment = ExcelVerticalAlignment.Top;

            row += 7;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Lp.";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Nazwa towaru lub usługi";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Jednostka miary";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Ilość towaru/usługi";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Cena";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Wartość";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Waga netto";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Waga brutto";

            worksheet.Cells[row, 10, row + 1, 11].Merge = true;
            worksheet.Cells[row, 10, row + 1, 11].Value = "Kod celny TARIC";

            worksheet.Cells[row, 2, row + 1, 11].Style.Font.Name = "Arial";
            worksheet.Cells[row, 2, row + 1, 11].Style.Font.Size = 9;
            worksheet.Cells[row, 2, row + 1, 11].Style.Font.Bold = true;
            worksheet.Cells[row, 2, row + 1, 11].Style.WrapText = true;
            worksheet.Cells[row, 2, row + 1, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 2, row + 1, 11].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            worksheet.Cells[row, 2, row + 1, 11].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[row, 2, row + 1, 11].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            worksheet.Row(row++).Height = 19;
            worksheet.Row(row++).Height = 19;

            //Adding data to tables
            int itemNumber = 0;

            foreach (PackingListForSpecification item in specifications) {
                int orderItemNumber = 1;

                itemNumber++;

                foreach (PackingListPackageOrderItem orderItem in item.OrderItems) {
                    column = 2;

                    worksheet.Cells[row, column++].Value = $"{itemNumber}.{orderItemNumber}";
                    worksheet.Cells[row, column++].Value =
                        $"{orderItem.SupplyInvoiceOrderItem.Product.VendorCode} {orderItem.SupplyInvoiceOrderItem.SupplyOrderItem.Product.NamePL}";
                    worksheet.Cells[row, column++].Value = orderItem.SupplyInvoiceOrderItem.Product.MeasureUnit.NamePl;
                    worksheet.Cells[row, column++].Value = orderItem.Qty;
                    worksheet.Cells[row, column++].Value = orderItem.SupplyInvoiceOrderItem.UnitPrice;
                    worksheet.Cells[row, column++].Value = Math.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.SupplyInvoiceOrderItem.UnitPrice, 2);
                    worksheet.Cells[row, column++].Value = Math.Round(Math.Round(orderItem.NetWeight, 3) * orderItem.Qty, 3);
                    worksheet.Cells[row, column].Value = Math.Round(Math.Round(orderItem.GrossWeight, 3) * orderItem.Qty, 3);

                    worksheet.Cells[row, 10, row, 11].Merge = true;
                    worksheet.Cells[row, 10, row, 11].Value = item.ProductSpecificationCode;

                    worksheet.Row(row).Style.Font.Name = "Arial";
                    worksheet.Row(row).Style.Font.Size = 9;
                    worksheet.Row(row).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Row(row).Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    worksheet.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    worksheet.Row(row++).Height = 25;

                    orderItemNumber++;
                }

                worksheet.Cells[row, 2, row, 5].Merge = true;

                worksheet.Cells[row, 6].Value = Math.Round(item.OrderItems.Sum(i => i.Qty), 2);
                worksheet.Cells[row, 7].Value = Math.Round(item.OrderItems.Sum(i => Convert.ToDecimal(i.Qty) * i.SupplyInvoiceOrderItem.SupplyOrderItem.UnitPrice), 2);
                worksheet.Cells[row, 8].Value = Math.Round(item.OrderItems.Sum(i => Math.Round(Math.Round(i.NetWeight, 3) * i.Qty, 3)), 3);
                worksheet.Cells[row, 9].Value = Math.Round(item.OrderItems.Sum(i => Math.Round(Math.Round(i.GrossWeight, 3) * i.Qty, 3)), 3);

                worksheet.Cells[row, 10, row, 11].Merge = true;

                worksheet.Row(row).Style.Font.Name = "Arial";
                worksheet.Row(row).Style.Font.Size = 9;
                worksheet.Row(row).Style.Font.Bold = true;
                worksheet.Row(row).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Row(row).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Row(row++).Height = 25;
            }

            worksheet.Cells[row, 2, row, 5].Merge = true;
            worksheet.Cells[row, 2, row, 5].Value = "Ogółem:";
            worksheet.Cells[row, 2, row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            worksheet.Cells[row, 2, row, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            decimal total = Math.Round(specifications.Sum(s => s.OrderItems.Sum(i => Convert.ToDecimal(i.Qty) * i.SupplyInvoiceOrderItem.SupplyOrderItem.UnitPrice)), 2);

            worksheet.Cells[row, 6].Value = Math.Round(specifications.Sum(s => s.OrderItems.Sum(i => i.Qty)), 2);
            worksheet.Cells[row, 7].Value = total;
            worksheet.Cells[row, 8].Value = Math.Round(specifications.Sum(s => s.OrderItems.Sum(i => Math.Round(Math.Round(i.NetWeight, 3) * i.Qty, 3))), 3);
            worksheet.Cells[row, 9].Value = Math.Round(specifications.Sum(s => s.OrderItems.Sum(i => Math.Round(Math.Round(i.GrossWeight, 3) * i.Qty, 3))), 3);

            worksheet.Cells[row, 10].Value = "Ilość kodów:";
            worksheet.Cells[row, 10].Style.WrapText = true;

            worksheet.Cells[row, 11].Value = itemNumber;

            worksheet.Cells[row, 6, row, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 6, row, 11].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Row(row).Style.Font.Name = "Arial";
            worksheet.Row(row).Style.Font.Size = 9;
            worksheet.Row(row).Style.Font.Bold = true;
            worksheet.Row(row).Height = 25;

            //worksheet.Cells[2, 2, row, 11].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            row += 4;

            worksheet.Cells[row, 4, row, 5].Merge = true;
            worksheet.Cells[row, 4, row, 5].Value = "Ilość towaru/usługi";

            worksheet.Cells[row, 3].Value = "Nazwa towaru lub usługi";
            worksheet.Cells[row, 6].Value = "Jednostka miary";
            worksheet.Cells[row, 7].Value = "Kod celny TARIC";

            worksheet.Cells[row, 3, row, 7].Style.Font.Name = "Arial";
            worksheet.Cells[row, 3, row, 7].Style.Font.Size = 9;
            worksheet.Cells[row, 3, row, 7].Style.Font.Bold = true;
            worksheet.Cells[row, 3, row, 7].Style.WrapText = true;
            worksheet.Cells[row, 3, row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 3, row, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            worksheet.Cells[row, 3, row, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[row, 3, row, 7].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            worksheet.Row(row).Height = 45;

            worksheet.Cells[row - 2, 9, row - 1, 9].Merge = true;
            worksheet.Cells[row - 2, 9, row - 1, 9].Value = total;

            worksheet.Cells[row - 2, 10, row - 1, 10].Merge = true;
            worksheet.Cells[row - 2, 10, row - 1, 10].Value =
                spec?.Client != null && spec.Client.ClientAgreements.Any()
                    ? spec.Client.ClientAgreements.First().Agreement.Currency.Code.ToUpper()
                    : "EUR";

            worksheet.Cells[row - 2, 9, row - 1, 10].Style.Font.Name = "Arial";
            worksheet.Cells[row - 2, 9, row - 1, 10].Style.Font.Size = 9;
            worksheet.Cells[row - 2, 9, row - 1, 10].Style.Font.Bold = true;
            worksheet.Cells[row - 2, 9, row - 1, 10].Style.WrapText = true;
            worksheet.Cells[row - 2, 9, row - 1, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row - 2, 9, row - 1, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            row++;

            foreach (GroupedSpecificationByPackingList groupedItem in grouped) {
                column = 3;

                worksheet.Cells[row, column].Value = groupedItem.SpecificationName;
                worksheet.Cells[row, column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[row, column++].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells[row, column, row, column + 1].Merge = true;
                worksheet.Cells[row, column, row, column + 1].Value = groupedItem.TotalQty;
                worksheet.Cells[row, column, row, column + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells[row, column++, row, column++].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells[row, column].Value = groupedItem.MeasureUnitNamePl;
                worksheet.Cells[row, column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, column++].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells[row, column].Value = groupedItem.SpecificationCode;
                worksheet.Cells[row, column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, column].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Row(row++).Height = 25;
            }

            //Adding default width/height for columns
            worksheet.Column(1).Width = 0.6;
            worksheet.Column(2).Width = 6.5;
            worksheet.Column(3).Width = 38.5;
            worksheet.Column(4).Width = 11;
            worksheet.Column(5).Width = 9;
            worksheet.Column(6).Width = 12.5;
            worksheet.Column(7).Width = 13;
            worksheet.Column(8).Width = 10;
            worksheet.Column(9).Width = 10;
            worksheet.Column(10).Width = 10.2;
            worksheet.Column(11).Width = 10.2;

            worksheet.Row(1).Height = 6;

            row = 2;
            column = 2;

            worksheet = package.Workbook.Worksheets.Add("Ukrainian");

            //Adding default specification header
            worksheet.Cells[row, 2, row, 3].Merge = true;
            worksheet.Cells[row, 2, row, 3].Value = "Оригінал / Копія";
            worksheet.Cells[row, 2, row, 3].Style.Font.Name = "Arial";
            worksheet.Cells[row, 2, row, 3].Style.Font.Size = 12;
            worksheet.Cells[row, 2, row, 3].Style.Font.Bold = true;
            worksheet.Cells[row, 2, row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 2, row, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Cells[row, 8, row, 13].Merge = true;
            worksheet.Cells[row, 8, row, 13].Value = DateTime.UtcNow.ToString("dd.MM.yyyy");
            worksheet.Cells[row, 8, row, 13].Style.Font.Name = "Arial";
            worksheet.Cells[row, 8, row, 13].Style.Font.Size = 9;
            worksheet.Cells[row, 8, row, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            worksheet.Cells[row, 8, row, 13].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            row++;

            if (spec != null) {
                worksheet.Cells[row, 2, row + 3, 13].Merge = true;
                worksheet.Cells[row, 2, row + 3, 13].Value =
                    string.Format(
                        "СПЕЦИФІКАЦІЯ {0}, {1} ВІД {2}",
                        spec.Client.FullName,
                        spec.SupplyInvoice.Number,
                        spec.SupplyInvoice.DateFrom.HasValue
                            ? spec.SupplyInvoice.DateFrom?.ToString("dd/MM/yyyy")
                            : spec.SupplyInvoice.Created.ToString("dd/MM/yyyy")
                    );
                worksheet.Cells[row, 2, row + 3, 13].Style.Font.Name = "Arial";
                worksheet.Cells[row, 2, row + 3, 13].Style.Font.Size = 18;
                worksheet.Cells[row, 2, row + 3, 13].Style.Font.Bold = true;
                worksheet.Cells[row, 2, row + 3, 13].Style.WrapText = true;
                worksheet.Cells[row, 2, row + 3, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 2, row + 3, 13].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[row, 2, row + 3, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 2, row + 3, 13].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

                row += 4;

                worksheet.Cells[row, 2, row, 5].Merge = true;
                worksheet.Cells[row, 2, row, 5].Value = "Продавець:";

                worksheet.Cells[row, 6, row, 13].Merge = true;
                worksheet.Cells[row, 6, row, 13].Value = "Покупець:";

                worksheet.Cells[row, 2, row, 13].Style.Font.Name = "Arial";
                worksheet.Cells[row, 2, row, 13].Style.Font.Size = 12;
                worksheet.Cells[row, 2, row, 13].Style.Font.Bold = true;
                worksheet.Cells[row, 2, row, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[row, 2, row, 13].Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;

                row++;

                PaymentRegister recipientPaymentRegister =
                    spec.Organization.PaymentRegisters.FirstOrDefault();

                worksheet.Cells[row, 2, row + 6, 5].Merge = true;
                worksheet.Cells[row, 2, row + 6, 5].Value =
                    string.Format(
                        "{0},\r\n{1},\r\n{2},\r\n{3} {4}",
                        spec.Client.FullName,
                        spec.Client.ClientBankDetails?.BankAddress ?? string.Empty,
                        spec.Client.ClientBankDetails?.AccountNumber?.AccountNumber ?? string.Empty,
                        spec.Client.ClientBankDetails?.ClientBankDetailIbanNo?.IBANNO ?? string.Empty,
                        spec.Client.ClientBankDetails?.BankAndBranch ?? string.Empty
                    );

                worksheet.Cells[row, 6, row + 6, 13].Merge = true;
                worksheet.Cells[row, 6, row + 6, 13].Value =
                    string.Format(
                        "{0},\r\n{1},\r\n{2},\r\n{3} {4}",
                        spec.Organization.NameUk,
                        spec.Organization.Address,
                        spec.Organization.TIN,
                        recipientPaymentRegister?.IBAN ?? string.Empty,
                        recipientPaymentRegister?.BankName ?? string.Empty
                    );
            }

            worksheet.Cells[row, 2, row + 6, 13].Style.Font.Name = "Arial";
            worksheet.Cells[row, 2, row + 6, 13].Style.Font.Size = 10;
            worksheet.Cells[row, 2, row + 6, 13].Style.WrapText = true;
            worksheet.Cells[row, 2, row + 6, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            worksheet.Cells[row, 2, row + 6, 13].Style.VerticalAlignment = ExcelVerticalAlignment.Top;

            row += 7;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "#";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Назва товару або послуги";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Од. Виміру";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "К-сть товару/послуги";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Ціна";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Вартість";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Вага нетто";

            column++;

            worksheet.Cells[row, column, row + 1, column].Merge = true;
            worksheet.Cells[row, column, row + 1, column].Value = "Вага брутто";

            worksheet.Cells[row, 10, row + 1, 11].Merge = true;
            worksheet.Cells[row, 10, row + 1, 11].Value = "Митний код вже був прихід";

            worksheet.Cells[row, 12, row + 1, 13].Merge = true;
            worksheet.Cells[row, 12, row + 1, 13].Value = "Можливий митний код";

            worksheet.Cells[row, 2, row + 1, 13].Style.Font.Name = "Arial";
            worksheet.Cells[row, 2, row + 1, 13].Style.Font.Size = 9;
            worksheet.Cells[row, 2, row + 1, 13].Style.Font.Bold = true;
            worksheet.Cells[row, 2, row + 1, 13].Style.WrapText = true;
            worksheet.Cells[row, 2, row + 1, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 2, row + 1, 13].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            worksheet.Cells[row, 2, row + 1, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[row, 2, row + 1, 13].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            worksheet.Row(row++).Height = 19;
            worksheet.Row(row++).Height = 19;

            //Adding data to tables
            itemNumber = 0;

            foreach (PackingListForSpecification item in specifications) {
                int orderItemNumber = 1;

                itemNumber++;

                foreach (PackingListPackageOrderItem orderItem in item.OrderItems) {
                    column = 2;

                    worksheet.Cells[row, column++].Value = $"{itemNumber}.{orderItemNumber}";
                    worksheet.Cells[row, column++].Value =
                        $"{orderItem.SupplyInvoiceOrderItem.Product.VendorCode} {orderItem.SupplyInvoiceOrderItem.Product.NameUA}";
                    worksheet.Cells[row, column++].Value = orderItem.SupplyInvoiceOrderItem.Product.MeasureUnit.NameUk;
                    worksheet.Cells[row, column++].Value = orderItem.Qty;
                    worksheet.Cells[row, column++].Value = orderItem.SupplyInvoiceOrderItem.UnitPrice;
                    worksheet.Cells[row, column++].Value = Math.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.SupplyInvoiceOrderItem.UnitPrice, 2);
                    worksheet.Cells[row, column++].Value = Math.Round(Math.Round(orderItem.NetWeight, 3) * orderItem.Qty, 3);
                    worksheet.Cells[row, column].Value = Math.Round(Math.Round(orderItem.GrossWeight, 3) * orderItem.Qty, 3);

                    worksheet.Cells[row, 10, row, 11].Merge = true;
                    worksheet.Cells[row, 10, row, 11].Value = item.ProductSpecificationCode;

                    worksheet.Cells[row, 12, row, 13].Merge = true;
                    worksheet.Cells[row, 12, row, 13].Value = orderItem.ProductSpecification?.SpecificationCode ?? "";

                    worksheet.Row(row).Style.Font.Name = "Arial";
                    worksheet.Row(row).Style.Font.Size = 9;
                    worksheet.Row(row).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Row(row).Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    worksheet.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    worksheet.Row(row++).Height = 25;

                    orderItemNumber++;
                }

                worksheet.Cells[row, 2, row, 5].Merge = true;

                worksheet.Cells[row, 6].Value = Math.Round(item.OrderItems.Sum(i => i.Qty), 2);
                worksheet.Cells[row, 7].Value = Math.Round(item.OrderItems.Sum(i => Convert.ToDecimal(i.Qty) * i.SupplyInvoiceOrderItem.SupplyOrderItem.UnitPrice), 2);
                worksheet.Cells[row, 8].Value = Math.Round(item.OrderItems.Sum(i => Math.Round(Math.Round(i.NetWeight, 3) * i.Qty, 3)), 3);
                worksheet.Cells[row, 9].Value = Math.Round(item.OrderItems.Sum(i => Math.Round(Math.Round(i.GrossWeight, 3) * i.Qty, 3)), 3);

                worksheet.Cells[row, 10, row, 11].Merge = true;
                worksheet.Cells[row, 12, row, 13].Merge = true;

                worksheet.Row(row).Style.Font.Name = "Arial";
                worksheet.Row(row).Style.Font.Size = 9;
                worksheet.Row(row).Style.Font.Bold = true;
                worksheet.Row(row).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Row(row).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Row(row++).Height = 25;
            }

            worksheet.Cells[row, 2, row, 5].Merge = true;
            worksheet.Cells[row, 2, row, 5].Value = "Всього:";
            worksheet.Cells[row, 2, row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            worksheet.Cells[row, 2, row, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            total = Math.Round(specifications.Sum(s => s.OrderItems.Sum(i => Convert.ToDecimal(i.Qty) * i.SupplyInvoiceOrderItem.SupplyOrderItem.UnitPrice)), 2);

            worksheet.Cells[row, 6].Value = Math.Round(specifications.Sum(s => s.OrderItems.Sum(i => i.Qty)), 2);
            worksheet.Cells[row, 7].Value = total;
            worksheet.Cells[row, 8].Value = Math.Round(specifications.Sum(s => s.OrderItems.Sum(i => Math.Round(Math.Round(i.NetWeight, 3) * i.Qty, 3))), 3);
            worksheet.Cells[row, 9].Value = Math.Round(specifications.Sum(s => s.OrderItems.Sum(i => Math.Round(Math.Round(i.GrossWeight, 3) * i.Qty, 3))), 3);

            worksheet.Cells[row, 10].Value = "К-сть кодів:";
            worksheet.Cells[row, 10].Style.WrapText = true;

            worksheet.Cells[row, 11].Value = itemNumber;

            worksheet.Cells[row, 6, row, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 6, row, 11].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Row(row).Style.Font.Name = "Arial";
            worksheet.Row(row).Style.Font.Size = 9;
            worksheet.Row(row).Style.Font.Bold = true;
            worksheet.Row(row).Height = 25;

            //worksheet.Cells[2, 2, row, 11].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            row += 4;

            worksheet.Cells[row, 4, row, 5].Merge = true;
            worksheet.Cells[row, 4, row, 5].Value = "К-сть товару/послуги";

            worksheet.Cells[row, 3].Value = "Назва товару або послуги";
            worksheet.Cells[row, 6].Value = "Од. Виміру";
            worksheet.Cells[row, 7].Value = "Митний код";

            worksheet.Cells[row, 3, row, 7].Style.Font.Name = "Arial";
            worksheet.Cells[row, 3, row, 7].Style.Font.Size = 9;
            worksheet.Cells[row, 3, row, 7].Style.Font.Bold = true;
            worksheet.Cells[row, 3, row, 7].Style.WrapText = true;
            worksheet.Cells[row, 3, row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 3, row, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            worksheet.Cells[row, 3, row, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[row, 3, row, 7].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            worksheet.Row(row).Height = 45;

            worksheet.Cells[row - 2, 9, row - 1, 9].Merge = true;
            worksheet.Cells[row - 2, 9, row - 1, 9].Value = total;

            worksheet.Cells[row - 2, 10, row - 1, 10].Merge = true;
            worksheet.Cells[row - 2, 10, row - 1, 10].Value =
                spec?.Client != null && spec.Client.ClientAgreements.Any()
                    ? spec.Client.ClientAgreements.First().Agreement.Currency.Code.ToUpper()
                    : "EUR";

            worksheet.Cells[row - 2, 9, row - 1, 10].Style.Font.Name = "Arial";
            worksheet.Cells[row - 2, 9, row - 1, 10].Style.Font.Size = 9;
            worksheet.Cells[row - 2, 9, row - 1, 10].Style.Font.Bold = true;
            worksheet.Cells[row - 2, 9, row - 1, 10].Style.WrapText = true;
            worksheet.Cells[row - 2, 9, row - 1, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row - 2, 9, row - 1, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            row++;

            foreach (GroupedSpecificationByPackingList groupedItem in grouped) {
                column = 3;

                worksheet.Cells[row, column].Value = groupedItem.SpecificationName;
                worksheet.Cells[row, column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[row, column++].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells[row, column, row, column + 1].Merge = true;
                worksheet.Cells[row, column, row, column + 1].Value = groupedItem.TotalQty;
                worksheet.Cells[row, column, row, column + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells[row, column++, row, column++].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells[row, column].Value = groupedItem.MeasureUnitNameUk;
                worksheet.Cells[row, column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, column++].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells[row, column].Value = groupedItem.SpecificationCode;
                worksheet.Cells[row, column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, column].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Row(row++).Height = 25;
            }

            //Adding default width/height for columns
            worksheet.Column(1).Width = 0.6;
            worksheet.Column(2).Width = 6.5;
            worksheet.Column(3).Width = 38.5;
            worksheet.Column(4).Width = 11;
            worksheet.Column(5).Width = 9;
            worksheet.Column(6).Width = 12.5;
            worksheet.Column(7).Width = 13;
            worksheet.Column(8).Width = 10;
            worksheet.Column(9).Width = 10;
            worksheet.Column(10).Width = 10.2;
            worksheet.Column(11).Width = 10.2;
            worksheet.Column(12).Width = 10.2;
            worksheet.Column(13).Width = 10.2;

            worksheet.Row(1).Height = 6;

            //Setting document properties.
            package.Workbook.Properties.Title = "Packing list specification";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            //Saving the file.
            package.Save();
        }

        return SaveFiles(fileName);
    }
}