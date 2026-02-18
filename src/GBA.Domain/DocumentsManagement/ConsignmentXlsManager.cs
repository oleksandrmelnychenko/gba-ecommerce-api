using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.EntityHelpers.Consignments;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public sealed class ConsignmentXlsManager : BaseXlsManager, IConsignmentXlsManager {
    public (string xlsxFile, string pdfFile) ExportGetRemainingProductsByStorageDocumentToXlsx(
        string path,
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
        double totalQtyFiltered) {
        string fileName = Path.Combine(path, $"RemainingProductsReport_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        if (remainingConsignments == null) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Remaining Products Report");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(5.14285, 1);
            worksheet.SetColumnWidth(18, 2);
            worksheet.SetColumnWidth(24.5714, 3);
            worksheet.SetColumnWidth(36.5714, 4);
            worksheet.SetColumnWidth(45.4286, 5);
            worksheet.SetColumnWidth(12.85714, 6);
            worksheet.SetColumnWidth(13.8571, 7);
            worksheet.SetColumnWidth(16, 8);
            worksheet.SetColumnWidth(11, 9);
            worksheet.SetColumnWidth(13.1428, 10);
            worksheet.SetColumnWidth(13.1428, 11);

            worksheet.SetRowHeight(22.7273, 1);

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 1]) {
                range.Value = "№";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 2, 1, 2]) {
                range.Value = "Дата";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 3, 1, 3]) {
                range.Value = "Код виробника";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 1, 4]) {
                range.Value = "Назва товару";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 5, 1, 5]) {
                range.Value = "Постачальник";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 6, 1, 6]) {
                range.Value = "К-сть";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 7, 1, 7]) {
                range.Value = "Ціна Нетто";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 8, 1, 8]) {
                range.Value = "Ціна Брутто";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 9, 1, 9]) {
                range.Value = "Ціна Брутто (Бух)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 10, 1, 10]) {
                range.Value = "Валюта";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 11, 1, 11]) {
                range.Value = "Вага";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 11]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 236, 197));
            }

            int row = 2;

            int counter = 1;

            foreach (RemainingConsignment remainingConsignment in remainingConsignments) {
                worksheet.SetRowHeight(15.9091, row);

                using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                    range.Value = counter;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                    range.Value = remainingConsignment.FromDate.ToString("dd.MM.yyyy hh:mm");
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Value = remainingConsignment.Product.VendorCode;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Value = remainingConsignment.Product.Name;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Value = remainingConsignment.SupplierName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Value = remainingConsignment.RemainingQty;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                    range.Value = remainingConsignment.NetPrice;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                    range.Value = remainingConsignment.GrossPrice;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                    range.Value = remainingConsignment.AccountingGrossPrice;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                    range.Value = remainingConsignment.CurrencyName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                    range.Value = remainingConsignment.Weight;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    range.Style.Numberformat.Format = "0.000";
                }

                using (ExcelRange range = worksheet.Cells[row, 1, row, 11]) {
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Name = "Arial";
                    range.Style.Font.Size = 10;
                }

                counter++;

                row++;
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                range.Value = "Загальна к-сть";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                range.Value = "Загальна (EUR)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                range.Value = "Загальна (EUR) (Бух)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                range.Value = "Загальна (UAH)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                range.Value = "Загальна (UAH) (Бух)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                range.Value = "К-сть за вибраний період";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                range.Value = "За вибраний період (EUR)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                range.Value = "За вибраний період (EUR) (Бух)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                range.Value = "За вибраний період (UAH)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                range.Value = "За вибраний період (UAH) (Бух)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 14]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                range.Value = "Сума:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                range.Value = totalQty;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                range.Value = totalEuro;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                range.Value = accountingTotalEuro;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                range.Value = totalLocal;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                range.Value = accountingTotalLocal;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                range.Value = totalQtyFiltered;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                range.Value = totalEuroFiltered;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                range.Value = accountingTotalEuroFiltered;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                range.Value = totalLocalFiltered;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                range.Value = accountingTotalLocalFiltered;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 14]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
            }

            package.Workbook.Properties.Title = "RemainingProducts Report";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportGetGroupedConsignmentByStorageDocumentToXlsx(
        string path,
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
        double totalQtyFiltered) {
        string fileName = Path.Combine(path, $"GroupedConsignmentByStorageReport_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        if (groupedConsignments == null) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Grouped Consignment By Storage Report");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(5.14285, 1);
            worksheet.SetColumnWidth(18, 2);
            worksheet.SetColumnWidth(21, 3);
            worksheet.SetColumnWidth(21, 4);
            worksheet.SetColumnWidth(45.4286, 5);
            worksheet.SetColumnWidth(20, 6);
            worksheet.SetColumnWidth(20, 7);
            worksheet.SetColumnWidth(20, 8);
            worksheet.SetColumnWidth(16, 9);

            worksheet.SetRowHeight(22.7273, 1);

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 1]) {
                range.Value = "№";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 2, 1, 2]) {
                range.Value = "Дата";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 3, 1, 3]) {
                range.Value = "Номер Приходу";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 1, 4]) {
                range.Value = "Номер Інвойса";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 5, 1, 5]) {
                range.Value = "Постачальник";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 6, 1, 6]) {
                range.Value = "Організація";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 7, 1, 7]) {
                range.Value = "Вартість Брутто";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 8, 1, 8]) {
                range.Value = "Вартість Брутто (Бух)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 9, 1, 9]) {
                range.Value = "Вага";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 9]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 236, 197));
            }

            int row = 2;

            int counter = 1;

            foreach (GroupedConsignment groupedConsignment in groupedConsignments) {
                worksheet.SetRowHeight(15.9091, row);

                using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                    range.Value = counter;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                    range.Value = TimeZoneInfo.ConvertTimeFromUtc(groupedConsignment.FromDate,
                        TimeZoneInfo.FindSystemTimeZoneById(CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                            ? "FLE Standard Time"
                            : "Central European Standard Time")).ToString("dd.MM.yyyy HH:mm");
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Value = groupedConsignment.ProductIncomeNumber;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Value = groupedConsignment.InvoiceNumber;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Value = groupedConsignment.SupplierName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Value = groupedConsignment.OrganizationName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                    range.Value = groupedConsignment.TotalGrossPrice;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                    range.Value = groupedConsignment.AccountingTotalGrossPrice;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                    range.Value = groupedConsignment.TotalWeight;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    range.Style.Numberformat.Format = "0.000";
                }

                using (ExcelRange range = worksheet.Cells[row, 1, row, 9]) {
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Name = "Arial";
                    range.Style.Font.Size = 10;
                }

                counter++;

                row++;
            }

            using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                range.Value = "Загальна к-сть";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                range.Value = "Загальна (EUR)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                range.Value = "Загальна (EUR) (Бух)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                range.Value = "К-сть за вибраний період";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                range.Value = "За вибраний період (EUR)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                range.Value = "За вибраний період (UAH)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                range.Value = "За вибраний період (EUR)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                range.Value = "За вибраний період (EUR) (Бух)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                range.Value = "За вибраний період (UAH)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                range.Value = "За вибраний період (UAH) (Бух)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 3, row, 12]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                range.Value = "Сума:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                range.Value = totalQty;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                range.Value = totalEuro;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                range.Value = accountingTotalEuro;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                range.Value = totalLocal;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                range.Value = accountingTotalLocal;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                range.Value = totalQtyFiltered;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                range.Value = totalEuroFiltered;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                range.Value = accountingTotalEuroFiltered;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                range.Value = totalLocalFiltered;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                range.Value = accountingTotalLocalFiltered;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[row, 3, row, 12]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
            }

            package.Workbook.Properties.Title = "GroupedConsignmentByStorage Report";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportClientMovementInfoFilteredDocumentToXlsx(
        string path,
        IEnumerable<ClientMovementConsignmentInfo> clientMovementConsignmentInfos) {
        string fileName = Path.Combine(path, $"ClientMovementInfoReport_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        bool isValidRetrieveData = clientMovementConsignmentInfos != null;

        if (!isValidRetrieveData) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Client Movement Info Report");

            worksheet.OutLineSummaryBelow = false;

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(6, 1);
            worksheet.SetColumnWidth(18.2857, 2);
            worksheet.SetColumnWidth(13.8571, 3);
            worksheet.SetColumnWidth(13.5714, 4);
            worksheet.SetColumnWidth(19.4285, 5);
            worksheet.SetColumnWidth(19.7142, 6);
            worksheet.SetColumnWidth(22, 7);
            worksheet.SetColumnWidth(21.7143, 8);
            worksheet.SetColumnWidth(18.4286, 9);
            worksheet.SetColumnWidth(25.1428, 10);
            worksheet.SetColumnWidth(12.8571, 11);
            worksheet.SetColumnWidth(11.4285, 12);
            worksheet.SetColumnWidth(12.8571, 13);
            worksheet.SetColumnWidth(21.7143, 14);

            worksheet.SetRowHeight(22.7273, 1);

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 1]) {
                range.Value = "№";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 2, 1, 2]) {
                range.Value = "Тип документа";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 3, 1, 3]) {
                range.Value = "Номер";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 1, 4]) {
                range.Value = "Дата";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 5, 1, 5]) {
                range.Value = "Організація";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 6, 1, 6]) {
                range.Value = "Відповідальний";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 7, 1, 7]) {
                range.Value = "Дата редагування";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 8, 1, 8]) {
                range.Value = "Код виробника";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 9, 1, 9]) {
                range.Value = "Назва товару";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 10, 1, 10]) {
                range.Value = "Митний код";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 11, 1, 11]) {
                range.Value = "Ціна";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 12, 1, 12]) {
                range.Value = "Кількість";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 13, 1, 13]) {
                range.Value = "Сума";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 14, 1, 14]) {
                range.Value = "Кількість позицій";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 14]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 236, 197));
            }

            int row = 2;

            int counter = 1;

            foreach (ClientMovementConsignmentInfo clientMovementConsignmentInfo in clientMovementConsignmentInfos) {
                using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                    range.Value = counter++;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                    range.Value = clientMovementConsignmentInfo.DocumentTypeName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Value = clientMovementConsignmentInfo.DocumentNumber;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Value = TimeZoneInfo.ConvertTimeFromUtc(clientMovementConsignmentInfo.DocumentFromDate,
                        TimeZoneInfo.FindSystemTimeZoneById(CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                            ? "FLE Standard Time"
                            : "Central European Standard Time")).ToString("dd.MM.yyyy HH:mm");
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Value = clientMovementConsignmentInfo.OrganizationName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Value = clientMovementConsignmentInfo.Responsible;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                    range.Value = TimeZoneInfo.ConvertTimeFromUtc(clientMovementConsignmentInfo.DocumentUpdatedDate,
                        TimeZoneInfo.FindSystemTimeZoneById(CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                            ? "FLE Standard Time"
                            : "Central European Standard Time")).ToString("dd.MM.yyyy HH:mm");
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                    range.Value = clientMovementConsignmentInfo.TotalEuroAmount;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                    range.Value = clientMovementConsignmentInfo.InfoItems.Count;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 1, row, 14]) {
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Name = "Arial";
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                }

                row++;

                foreach (ClientMovementConsignmentInfoItem clientMovementConsignmentInfoItem in clientMovementConsignmentInfo.InfoItems) {
                    using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                        range.Value = counter++;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                        range.Value = clientMovementConsignmentInfoItem.Product.VendorCode;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                        range.Value = clientMovementConsignmentInfoItem.Product.Name;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                        range.Value = clientMovementConsignmentInfoItem.ProductSpecificationCode;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                        range.Value = decimal.Round(
                            Convert.ToDecimal(clientMovementConsignmentInfoItem.ItemQty) * clientMovementConsignmentInfoItem.PricePerItem,
                            2,
                            MidpointRounding.AwayFromZero);
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                        range.Value = clientMovementConsignmentInfoItem.ItemQty;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 1, row, 14]) {
                        range.Style.WrapText = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 10;
                    }

                    ExcelRow excelRow = worksheet.Row(row);

                    excelRow.Collapsed = true;

                    excelRow.OutlineLevel = 1;

                    worksheet.SetRowHeight(12.1212, row);

                    row++;
                }
            }

            package.Workbook.Properties.Title = "ClientMovementInfo Report";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportIncomeMovementConsignmentDocumentToXlsx(
        string path,
        IEnumerable<IncomeConsignmentInfo> incomeConsignmentInfos) {
        string fileName = Path.Combine(path, $"IncomeMovementConsignmentReport_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        bool isValidRetrieveData = incomeConsignmentInfos != null;

        if (!isValidRetrieveData) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Income Movement Consignment Report");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(5, 1);
            worksheet.SetColumnWidth(20, 2);
            worksheet.SetColumnWidth(20, 3);
            worksheet.SetColumnWidth(20, 4);
            worksheet.SetColumnWidth(17.2857, 5);
            worksheet.SetColumnWidth(16.4286, 6);
            worksheet.SetColumnWidth(18.1429, 7);
            worksheet.SetColumnWidth(20.1429, 8);
            worksheet.SetColumnWidth(8.1429, 9);
            worksheet.SetColumnWidth(9.1429, 10);
            worksheet.SetColumnWidth(9.1429, 11);
            worksheet.SetColumnWidth(9.1429, 12);
            worksheet.SetColumnWidth(10.1428, 13);
            worksheet.SetColumnWidth(11.4286, 14);
            worksheet.SetColumnWidth(14, 15);
            worksheet.SetColumnWidth(16.8571, 16);
            worksheet.SetColumnWidth(14.8571, 17);
            worksheet.SetColumnWidth(10, 18);

            worksheet.SetRowHeight(34.091, 1);

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 1]) {
                range.Value = "№";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 2, 1, 2]) {
                range.Value = "Склад";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 3, 1, 3]) {
                range.Value = "Постачальник";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 1, 4]) {
                range.Value = "Організація";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 5, 1, 5]) {
                range.Value = "Дата приходу на склад";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 6, 1, 6]) {
                range.Value = "№ документа приходу";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 7, 1, 7]) {
                range.Value = "№ прихідного інвойсу";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 8, 1, 8]) {
                range.Value = "Дата прихідного інвойсу";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 9, 1, 9]) {
                range.Value = "Ціна нетто (інвойса)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 10, 1, 10]) {
                range.Value = "Сума нетто (інвойса)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 11, 1, 11]) {
                range.Value = "Ціна брутто УО";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 12, 1, 12]) {
                range.Value = "Ціна брутто БО";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 13, 1, 13]) {
                range.Value = "Вага";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 14, 1, 14]) {
                range.Value = "К-сть в приході";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 15, 1, 15]) {
                range.Value = "Залишок (к-сть)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 16, 1, 16]) {
                range.Value = "З якого інвойсу №";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 17, 1, 17]) {
                range.Value = "З якого інвойсу дата";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 18, 1, 18]) {
                range.Value = "Ціна повернення";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 19, 1, 19]) {
                range.Value = "Різниця";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 19]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 236, 197));
            }

            int row = 2;

            int counter = 1;

            foreach (IncomeConsignmentInfo incomeConsignmentInfo in incomeConsignmentInfos) {
                worksheet.SetRowHeight(15.9091, row);

                using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                    range.Value = counter++;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                    range.Value = incomeConsignmentInfo.StorageName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Value = incomeConsignmentInfo.SupplierName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Value = incomeConsignmentInfo.OrganizationName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Value = incomeConsignmentInfo.IncomeToStorageDate.HasValue
                        ? TimeZoneInfo.ConvertTimeFromUtc(incomeConsignmentInfo.IncomeToStorageDate.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                                ? "FLE Standard Time"
                                : "Central European Standard Time")).ToString("dd.MM.yyyy HH:mm")
                        : "";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Value = incomeConsignmentInfo.IncomeToStorageNumber;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                    range.Value = incomeConsignmentInfo.IncomeInvoiceNumber;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                    range.Value = incomeConsignmentInfo.IncomeInvoiceDate.HasValue
                        ? TimeZoneInfo.ConvertTimeFromUtc(incomeConsignmentInfo.IncomeInvoiceDate.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                                ? "FLE Standard Time"
                                : "Central European Standard Time")).ToString("dd.MM.yyyy HH:mm")
                        : "";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                    range.Value = incomeConsignmentInfo.NetPrice;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                    range.Value = incomeConsignmentInfo.TotalNetPrice;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                    range.Value = incomeConsignmentInfo.GrossPrice;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                    range.Value = incomeConsignmentInfo.AccountingGrossPrice;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                    range.Value = incomeConsignmentInfo.Weight;
                    range.Style.Numberformat.Format = "0.000";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                    range.Value = incomeConsignmentInfo.IncomeQty;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 15, row, 15]) {
                    range.Value = incomeConsignmentInfo.RemainingQty;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 16, row, 16]) {
                    range.Value = incomeConsignmentInfo.FromInvoiceNumber;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 17, row, 17]) {
                    range.Value = incomeConsignmentInfo.FromInvoiceDate.HasValue
                        ? TimeZoneInfo.ConvertTimeFromUtc(incomeConsignmentInfo.FromInvoiceDate.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                                ? "FLE Standard Time"
                                : "Central European Standard Time")).ToString("dd.MM.yyyy HH:mm")
                        : "";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 18, row, 18]) {
                    range.Value = incomeConsignmentInfo.ReturnPrice;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 19, row, 19]) {
                    range.Value = incomeConsignmentInfo.PriceDifference;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 1, row, 19]) {
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Name = "Arial";
                    range.Style.Font.Size = 11;
                }

                row++;
            }

            package.Workbook.Properties.Title = "IncomeMovementConsignment Report";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportOutcomeMovementConsignmentDocumentToXlsx(
        string path,
        IEnumerable<OutcomeConsignmentInfo> outcomeConsignmentInfos) {
        string fileName = Path.Combine(path, $"OutcomeMovementConsignmentReport_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        bool isValidRetrieveData = outcomeConsignmentInfos != null;

        if (!isValidRetrieveData) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Outcome Movement Consignment Report");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(6, 1);
            worksheet.SetColumnWidth(17, 2);
            worksheet.SetColumnWidth(20, 3);
            worksheet.SetColumnWidth(23.8571, 4);
            worksheet.SetColumnWidth(26, 5);
            worksheet.SetColumnWidth(22.4285, 6);
            worksheet.SetColumnWidth(26, 7);
            worksheet.SetColumnWidth(21.7143, 8);
            worksheet.SetColumnWidth(18.8572, 9);
            worksheet.SetColumnWidth(11.2857, 10);

            worksheet.SetRowHeight(19.697, 1);

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 1]) {
                range.Value = "№";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 2, 1, 2]) {
                range.Value = "Дата";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 3, 1, 3]) {
                range.Value = "Вид документу";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 1, 4]) {
                range.Value = "Склад";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 5, 1, 5]) {
                range.Value = "Організація";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 6, 1, 6]) {
                range.Value = "Номер документа";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 7, 1, 7]) {
                range.Value = "Клієнт";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 8, 1, 8]) {
                range.Value = "Відповідальний";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 9, 1, 9]) {
                range.Value = "Ціна продажу";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 10, 1, 10]) {
                range.Value = "К-сть";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 10]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 236, 197));
            }

            int row = 2;

            int counter = 1;

            foreach (OutcomeConsignmentInfo outcomeConsignmentInfo in outcomeConsignmentInfos) {
                worksheet.SetRowHeight(15.9091, row);

                using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                    range.Value = counter++;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                    range.Value = outcomeConsignmentInfo.FromDate.ToString("dd.MM.yyyy hh:mm");
                    range.Value = TimeZoneInfo.ConvertTimeFromUtc(outcomeConsignmentInfo.FromDate,
                        TimeZoneInfo.FindSystemTimeZoneById(CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                            ? "FLE Standard Time"
                            : "Central European Standard Time")).ToString("dd.MM.yyyy HH:mm");
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Value = outcomeConsignmentInfo.DocumentTypeName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Value = outcomeConsignmentInfo.StorageName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Value = outcomeConsignmentInfo.OrganizationName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Value = outcomeConsignmentInfo.DocumentNumber;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                    range.Value = outcomeConsignmentInfo.ClientName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                    range.Value = outcomeConsignmentInfo.ResponsibleName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                    range.Value = outcomeConsignmentInfo.Price;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                    range.Value = outcomeConsignmentInfo.Qty;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 1, row, 10]) {
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Name = "Arial";
                    range.Style.Font.Size = 11;
                }

                row++;
            }

            package.Workbook.Properties.Title = "OutcomeMovementConsignment Report";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportMovementInfoDocumentToXlsx(
        string path,
        IEnumerable<MovementConsignmentInfo> movementConsignmentInfos) {
        string fileName = Path.Combine(path, $"MovementConsignmentInfoReport_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        bool isValidRetrieveData = movementConsignmentInfos != null;

        if (!isValidRetrieveData) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Movement Consignment Info Report");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(6, 1);
            worksheet.SetColumnWidth(19, 2);
            worksheet.SetColumnWidth(23, 3);
            worksheet.SetColumnWidth(23, 4);
            worksheet.SetColumnWidth(15.7143, 5);
            worksheet.SetColumnWidth(16, 6);
            worksheet.SetColumnWidth(22.7143, 7);
            worksheet.SetColumnWidth(22.2857, 8);
            worksheet.SetColumnWidth(23.8571, 9);
            worksheet.SetColumnWidth(26, 10);
            worksheet.SetColumnWidth(10, 11);
            worksheet.SetColumnWidth(10, 12);
            worksheet.SetColumnWidth(10, 13);
            worksheet.SetColumnWidth(10, 14);
            worksheet.SetColumnWidth(22, 15);

            worksheet.SetRowHeight(32, 1);

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 1]) {
                range.Value = "№";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 2, 1, 2]) {
                range.Value = "№ прихідного інвойсу";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 3, 1, 3]) {
                range.Value = "Дата прихідного інвойсу";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 1, 4]) {
                range.Value = "Вид документу";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 5, 1, 5]) {
                range.Value = "Номер";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 6, 1, 6]) {
                range.Value = "Від якої дати";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 7, 1, 7]) {
                range.Value = "Клієнт";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 8, 1, 8]) {
                range.Value = "Склад";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 9, 1, 9]) {
                range.Value = "Організація";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 10, 1, 10]) {
                range.Value = "Відповідальний";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 11, 1, 11]) {
                range.Value = "Ціна";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 12, 1, 12]) {
                range.Value = "Ціна Бух.";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 13, 1, 13]) {
                range.Value = "Знижка";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 14, 1, 14]) {
                range.Value = "Прихід";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 15, 1, 15]) {
                range.Value = "Розхід";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 16, 1, 16]) {
                range.Value = "Коментар";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
            }

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 16]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 236, 197));
            }

            int row = 2;

            int counter = 1;

            foreach (MovementConsignmentInfo movementConsignmentInfo in movementConsignmentInfos) {
                using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                    range.Value = counter++;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                    range.Value = movementConsignmentInfo.IncomeDocumentNumber;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Value = movementConsignmentInfo.IncomeDocumentFromDate.HasValue
                        ? TimeZoneInfo.ConvertTimeFromUtc(movementConsignmentInfo.IncomeDocumentFromDate.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                                ? "FLE Standard Time"
                                : "Central European Standard Time")).ToString("dd.MM.yyyy HH:mm")
                        : "";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Value = movementConsignmentInfo.DocumentType;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Value = movementConsignmentInfo.DocumentNumber;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Value = movementConsignmentInfo.DocumentFromDate.ToString("dd.MM.yyyy hh:mm");
                    range.Value = TimeZoneInfo.ConvertTimeFromUtc(movementConsignmentInfo.DocumentFromDate,
                        TimeZoneInfo.FindSystemTimeZoneById(CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                            ? "FLE Standard Time"
                            : "Central European Standard Time")).ToString("dd.MM.yyyy HH:mm");
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                    range.Value = movementConsignmentInfo.ClientName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                    range.Value = movementConsignmentInfo.StorageName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                    range.Value = movementConsignmentInfo.OrganizationName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                    range.Value = movementConsignmentInfo.Responsible;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                    range.Value = Math.Round(movementConsignmentInfo.Price, 2, MidpointRounding.AwayFromZero);
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                    range.Value = movementConsignmentInfo.AccountingPrice;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                    range.Value = movementConsignmentInfo.Discount;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                    range.Value = movementConsignmentInfo.IncomeQty;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 15, row, 15]) {
                    range.Value = movementConsignmentInfo.OutcomeQty;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 16, row, 16]) {
                    range.Value = movementConsignmentInfo.Comment;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(204, 192, 133));
                }

                using (ExcelRange range = worksheet.Cells[row, 1, row, 16]) {
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Name = "Arial";
                    range.Style.Font.Size = 11;
                }

                row++;
            }

            package.Workbook.Properties.Title = "MovementConsignmentInfo Report";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }
}