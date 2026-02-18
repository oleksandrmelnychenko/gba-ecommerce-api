using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using OfficeOpenXml;
using OfficeOpenXml.Style;
#if WINDOWS
using Microsoft.Office.Interop.Excel;
#endif

namespace GBA.Domain.DocumentsManagement;

public sealed class ProductPlacementStorageManager : BaseXlsManager, IProductPlacementStorageManager {
    public (string xlsxFile, string pdfFile) ExportProductPlacementStorageToXlsx(string path, IEnumerable<ProductPlacementStorage> productPlacementStorages,
        IEnumerable<DocumentMonth> months) {
        string fileName = Path.Combine(path, $"ShipmentList_{DateTime.Now.ToString("MM.yyyy")}_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("ShipmentList Document");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(1.4286, 1);
            worksheet.SetColumnWidth(14.7143, 2);
            worksheet.SetColumnWidth(50, 3);
            worksheet.SetColumnWidth(23.4286, 4);
            worksheet.SetColumnWidth(40, 5);
            worksheet.SetColumnWidth(14.7143, 6);
            worksheet.SetColumnWidth(16, 7);

            worksheet.SetRowHeight(21.2121, 1);
            worksheet.SetRowHeight(22, 2);

            using (ExcelRange range = worksheet.Cells[1, 3, 1, 3]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 2, 2, 2]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "№";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 3, 2, 3]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Назва Товару";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 2, 4]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "К-сть";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 5, 2, 5]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Місце Зберігання";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 6, 2, 6]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Код товару";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            int row = 3;
            int count = 1;
            foreach (ProductPlacementStorage productPlacementStorage in productPlacementStorages) {
                worksheet.SetRowHeight(10.7, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = count;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = productPlacementStorage.Product.Name;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }


                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = productPlacementStorage.Qty;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = productPlacementStorage.Placement;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = productPlacementStorage.VendorCode;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                row++;
                count++;
            }

            worksheet.Cells.AutoFitColumns();

            package.Save();
        }

        return SaveFilesPages(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportVerificationStockStatesStorageToXlsxTest(string path, DateTime from, DateTime to,
        IEnumerable<ProductPlacementDataHistory> productPlacementDataHistoryList, IEnumerable<DocumentMonth> months) {
        string fileName = Path.Combine(path, $"HistoryProduct_{DateTime.Now.ToString("MM.yyyy")}_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("HistoryProduct Document");


            worksheet.SetColumnWidth(5.4286, 1);
            worksheet.SetColumnWidth(14.7143, 2);
            worksheet.SetColumnWidth(40, 3);
            worksheet.SetColumnWidth(15, 4);
            worksheet.SetColumnWidth(5, 5);

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 5]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = $"Наявність проданих товарів за період з {from:dd.MM.yyyy} до {to:dd.MM.yyyy} на дату {DateTime.Now:dd.MM.yyyy}";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[2, 1, 3, 1]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "№";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[2, 2, 3, 2]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Код товару";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[2, 3, 3, 3]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Назва Товару";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[2, 4, 3, 4]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Місце зберігання";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[2, 5, 3, 5]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Кількість";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            int row = 4;
            int count = 1;

            //var groupedData = productPlacementDataHistoryList
            //    .GroupBy(p => new { p.StorageId, p.Product.Id, Placement = p.StorageNumber + "-" + p.RowNumber + "-" + p.CellNumber })
            //    .Select(g => new {
            //        ProductId = g.Key.Id,
            //        VendorCode = g.First().Product.VendorCode,
            //        ProductName = g.First().Product.NameUA,
            //        Placement = g.Key.Placement,
            //        StorageId = g.Key.StorageId,
            //        TotalQty = g.Sum(x => x.Qty),
            //        RowCount = g.Count() 
            //    })
            //    .ToList();
            var groupedData = productPlacementDataHistoryList
                .GroupBy(p => new {
                    //p.StorageId,
                    p.Product.Id,
                    p.Product.VendorCode,
                    Placement = p.StorageNumber + "-" + p.RowNumber + "-" + p.CellNumber
                })
                .Select(g => new {
                    ProductId = g.First().Product.Id,
                    g.Key.VendorCode,
                    ProductName = g.First().Product.NameUA,
                    g.Key.Placement,
                    //StorageId = g.Key.StorageId,
                    TotalQty = g.Sum(x => x.Qty),
                    RowCount = g.Count()
                })
                .ToList();
            foreach (var item in groupedData) {
                int startRow = row;
                int endRow = row + item.RowCount - 1;

                worksheet.Cells[startRow, 1, endRow, 1].Merge = true;
                worksheet.Cells[startRow, 2, endRow, 2].Merge = true;
                worksheet.Cells[startRow, 3, endRow, 3].Merge = true;
                worksheet.Cells[startRow, 4, endRow, 4].Merge = true;
                worksheet.Cells[startRow, 5, endRow, 5].Merge = true;

                worksheet.Cells[startRow, 1].Value = count;
                worksheet.Cells[startRow, 2].Value = item.VendorCode;
                worksheet.Cells[startRow, 3].Value = item.ProductName;
                worksheet.Cells[startRow, 4].Value = item.Placement;
                worksheet.Cells[startRow, 5].Value = item.TotalQty;

                for (int col = 1; col <= 5; col++)
                    using (ExcelRange range = worksheet.Cells[startRow, col, endRow, col]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    }

                row += item.RowCount;
                count++;
            }

            using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Name = "Arial";
                range.Value = "Кількість товару : " + productPlacementDataHistoryList.Count();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            package.Save();
        }

        return SaveFilesPages(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportVerificationStockStatesStorageToXlsx(string path, DateTime from, DateTime to, IEnumerable<StockStateStorage> stockStorageList,
        IEnumerable<DocumentMonth> months) {
        string fileName = Path.Combine(path, $"HistoryProduct_{DateTime.Now.ToString("MM.yyyy")}_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("HistoryProduct Document");


            worksheet.SetColumnWidth(5.4286, 1);
            worksheet.SetColumnWidth(14.7143, 2);
            worksheet.SetColumnWidth(40, 3);
            worksheet.SetColumnWidth(15, 4);
            worksheet.SetColumnWidth(5, 5);

            using (ExcelRange range = worksheet.Cells[1, 1, 1, 5]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = $"Наявність проданих товарів за період з {from:dd.MM.yyyy} до {to:dd.MM.yyyy} на дату {DateTime.Now:MM.yyyy}";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[2, 1, 3, 1]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "№";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[2, 2, 3, 2]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Код товару";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[2, 3, 3, 3]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Назва Товару";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[2, 4, 3, 4]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Місце зберігання";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[2, 5, 3, 5]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Кількість";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            int row = 4;
            int count = 1;
            foreach (StockStateStorage stockStateStorage in stockStorageList)
            foreach (ProductAvailabilityDataHistory productAvailabilityDataHistory in stockStateStorage.ProductAvailabilityDataHistory)
            foreach (ProductPlacementDataHistory productPlacementDataHistory in productAvailabilityDataHistory.ProductPlacementDataHistory) {
                using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = count;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = stockStateStorage.Product.VendorCode;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }


                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = stockStateStorage.Product.NameUA;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = productPlacementDataHistory.StorageNumber + "-" + productPlacementDataHistory.RowNumber + "-" + productPlacementDataHistory.CellNumber;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = productPlacementDataHistory.Qty;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                row++;
                count++;
            }

            //worksheet.Cells.AutoFitColumns();
            using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Name = "Arial";
                range.Value = "Кількість товару : " + stockStorageList.Count();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            package.Save();
        }

        return SaveFilesPages(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportStockStateStorageToXlsx(string path, IEnumerable<StockStateStorage> stockStorageList, IEnumerable<DocumentMonth> months) {
        string fileName = Path.Combine(path, $"HistoryProduct_{DateTime.Now.ToString("MM.yyyy")}_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("HistoryProduct Document");


            worksheet.SetColumnWidth(5.4286, 1);
            worksheet.SetColumnWidth(14.7143, 2);
            worksheet.SetColumnWidth(30, 3);
            worksheet.SetColumnWidth(5.4286, 4);
            worksheet.SetColumnWidth(5, 5);
            worksheet.SetColumnWidth(15, 6);
            worksheet.SetColumnWidth(5, 7);
            worksheet.SetColumnWidth(20, 8);

            using (ExcelRange range = worksheet.Cells[1, 1, 2, 1]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "№";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 2, 2, 2]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Код товару";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 3, 2, 3]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Назва Товару";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 2, 4]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "В рахунках";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 5, 2, 5]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Всього";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 6, 2, 6]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Місце зберігання";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 7, 2, 7]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Кількість";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 8, 2, 8]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Склад";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            int row = 3;
            int count = 1;
            int mergeBatchSize = 56;
            foreach (StockStateStorage stockStateStorage in stockStorageList) {
                int startRow = row;
                int rowCount = 0;
                double amount = 0;
                foreach (ProductAvailabilityDataHistory productAvailabilityDataHistory in stockStateStorage.ProductAvailabilityDataHistory) {
                    foreach (ProductPlacementDataHistory productPlacementDataHistory in productAvailabilityDataHistory.ProductPlacementDataHistory) {
                        amount += productPlacementDataHistory.Qty;
                        //worksheet.SetRowHeight(10.7, row);

                        using (ExcelRange range = worksheet.Cells[row, 1, row, 1]) {
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Font.Name = "Arial";
                            range.Value = count;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                        }

                        using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Font.Name = "Arial";
                            range.Value = stockStateStorage.Product.VendorCode;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                        }


                        using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                            range.Style.WrapText = true;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Font.Name = "Arial";
                            range.Value = stockStateStorage.Product.NameUA;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                        }

                        using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Font.Name = "Arial";
                            range.Value = productPlacementDataHistory.StorageNumber + "-" + productPlacementDataHistory.RowNumber + "-" + productPlacementDataHistory.CellNumber;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                        }

                        using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Font.Name = "Arial";
                            range.Value = productPlacementDataHistory.Qty;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                        }

                        using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Font.Name = "Arial";
                            range.Value = productAvailabilityDataHistory.Storage.Name;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                        }

                        row++;
                        count++;
                        rowCount++;

                        if (rowCount >= mergeBatchSize) {
                            using (ExcelRange range = worksheet.Cells[startRow, 4, startRow + rowCount - 1, 4]) {
                                range.Merge = true;
                                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                range.Style.Font.Size = 8;
                                range.Style.Font.Name = "Arial";
                                range.Value = stockStateStorage.TotalReservedUK;
                                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                            }

                            using (ExcelRange range = worksheet.Cells[startRow, 5, startRow + rowCount - 1, 5]) {
                                range.Merge = true;
                                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                range.Style.Font.Size = 8;
                                range.Style.Font.Name = "Arial";
                                range.Value = amount;
                                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                            }

                            startRow = row;
                            rowCount = 0;
                            mergeBatchSize = 58;
                            amount = 0;
                        }
                    }

                    if (rowCount > 0)
                        using (ExcelRange range = worksheet.Cells[startRow, 4, startRow + rowCount - 1, 4]) {
                            range.Merge = true;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Font.Name = "Arial";
                            range.Value = stockStateStorage.TotalReservedUK;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                        }
                    else
                        using (ExcelRange range = worksheet.Cells[startRow, 4, startRow, 4]) {
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Font.Name = "Arial";
                            range.Value = stockStateStorage.TotalReservedUK;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                        }

                    if (rowCount > 0)
                        using (ExcelRange range = worksheet.Cells[startRow, 5, startRow + rowCount - 1, 5]) {
                            range.Merge = true;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Font.Name = "Arial";
                            range.Value = amount;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                        }
                    else
                        using (ExcelRange range = worksheet.Cells[startRow, 5, startRow, 5]) {
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Font.Name = "Arial";
                            range.Value = amount;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                        }
                }
            }

            //worksheet.Cells.AutoFitColumns();
            using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Name = "Arial";
                range.Value = "Кількість товару : " + stockStorageList.Count();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            package.Save();
        }

        return SaveFilesPages(fileName);
    }
}