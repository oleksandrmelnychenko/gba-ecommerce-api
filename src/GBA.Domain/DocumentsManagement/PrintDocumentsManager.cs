using System;
using System.Collections.Generic;
using System.IO;
using GBA.Common.Helpers.PrintingDocuments;
using GBA.Domain.DocumentsManagement.Contracts;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public sealed class PrintDocumentsManager : BaseXlsManager, IPrintDocumentsManager {
    public (string, string) GetPrintDocument(
        string path,
        List<ColumnsDataForPrinting> columns,
        List<Dictionary<string, string>> rows) {
        string fileName = Path.Combine(path, $"PrintDocument_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Print Document");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            int row = 1;

            worksheet.SetColumnWidth(12, 1);

            using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 12;
                range.Style.Font.Name = "Arial";
                range.Value = "№";
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thick);
            }

            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++) {
                worksheet.SetColumnWidth(23, 2, columns.Count + 1);

                using ExcelRange range = worksheet.Cells[row, columnIndex + 2, row, columnIndex + 2];
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 12;
                range.Style.Font.Name = "Arial";
                range.Value = columns[columnIndex].Translate;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thick);
            }

            row++;

            for (int indexRow = 0; indexRow < rows.Count; indexRow++) {
                worksheet.SetRowHeight(30, 1, rows.Count + 1);

                using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Name = "Arial";
                    range.Value = indexRow + 1;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                int column = 2;

                foreach (ColumnsDataForPrinting columnForPrinting in columns) {
                    using (ExcelRange range = worksheet.Cells[row, column, row, column]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                        range.Style.Font.Size = 10;
                        range.Style.Font.Name = "Arial";
                        range.Value = rows[indexRow][columnForPrinting.ColumnName];
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.ReadingOrder = ExcelReadingOrder.ContextDependent;
                    }

                    column++;
                }

                row++;
            }

            package.Workbook.Properties.Title = "Print Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }
}