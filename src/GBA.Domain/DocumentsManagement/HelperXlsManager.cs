using System;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace GBA.Domain.DocumentsManagement;

public static class HelperXlsManager {
    /// <summary>
    /// Convert old *.xls formatted file to new *.xlsx format.
    /// </summary>
    public static string ConvertXlsToXlsx(string pathToFile) {
        string xlsxFile = pathToFile + "x";

        using (FileStream fs = new(pathToFile, FileMode.Open, FileAccess.Read)) {
            HSSFWorkbook hssfWorkbook = new(fs);
            XSSFWorkbook xssfWorkbook = new();

            for (int i = 0; i < hssfWorkbook.NumberOfSheets; i++) {
                ISheet hssfSheet = hssfWorkbook.GetSheetAt(i);
                ISheet xssfSheet = xssfWorkbook.CreateSheet(hssfSheet.SheetName);

                for (int rowIndex = 0; rowIndex <= hssfSheet.LastRowNum; rowIndex++) {
                    IRow hssfRow = hssfSheet.GetRow(rowIndex);
                    if (hssfRow == null) continue;

                    IRow xssfRow = xssfSheet.CreateRow(rowIndex);
                    for (int cellIndex = 0; cellIndex < hssfRow.LastCellNum; cellIndex++) {
                        ICell hssfCell = hssfRow.GetCell(cellIndex);
                        if (hssfCell == null) continue;

                        ICell xssfCell = xssfRow.CreateCell(cellIndex);
                        switch (hssfCell.CellType) {
                            case CellType.Numeric:
                                xssfCell.SetCellValue(hssfCell.NumericCellValue);
                                break;
                            case CellType.String:
                                xssfCell.SetCellValue(hssfCell.StringCellValue);
                                break;
                            case CellType.Boolean:
                                xssfCell.SetCellValue(hssfCell.BooleanCellValue);
                                break;
                            case CellType.Formula:
                                xssfCell.SetCellFormula(hssfCell.CellFormula);
                                break;
                        }
                    }
                }
            }

            using (FileStream outputStream = new(xlsxFile, FileMode.Create, FileAccess.Write)) {
                xssfWorkbook.Write(outputStream);
            }
        }

        return xlsxFile;
    }

    /// <summary>
    /// Convert *.xlsx file to PDF.
    /// Note: PDF conversion requires external tools. This method throws NotSupportedException.
    /// </summary>
    public static string ConvertXlsxToPDF(string pathToFile) {
        throw new NotSupportedException(
            "Excel to PDF conversion is not supported in .NET Core without Excel COM automation. " +
            "Consider using a third-party library like Aspose.Cells or running on Windows with Excel installed.");
    }

    /// <summary>
    /// Convert *.xlsx file to PDF with page settings.
    /// Note: PDF conversion requires external tools. This method throws NotSupportedException.
    /// </summary>
    public static string ConvertXlsxToPDFPages(string pathToFile) {
        throw new NotSupportedException(
            "Excel to PDF conversion is not supported in .NET Core without Excel COM automation. " +
            "Consider using a third-party library like Aspose.Cells or running on Windows with Excel installed.");
    }
}