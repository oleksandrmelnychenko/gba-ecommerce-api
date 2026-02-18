using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using GBA.Common.Extensions;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.DepreciatedOrders;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public sealed class OrderXlsManager : BaseXlsManager, IOrderXlsManager {
    public (string xlsxFile, string pdfFile) ExportDepreciatedOrderDocumentToXlsx(string path, DepreciatedOrder depreciatedOrder) {
        string fileName = Path.Combine(path, $"DepreciatedOrder_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        decimal totalAmount = 0;

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("DepreciatedOrder Document");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Setting default width to columns
            worksheet.SetColumnWidth(1.5714, 1);
            worksheet.SetColumnWidth(3, 2);
            worksheet.SetColumnWidth(3, 3);
            worksheet.SetColumnWidth(2.7143, 4);
            worksheet.SetColumnWidth(3, 5);
            worksheet.SetColumnWidth(3.8571, 6);
            worksheet.SetColumnWidth(3, 7);
            worksheet.SetColumnWidth(3, 8);
            worksheet.SetColumnWidth(2, 9);
            worksheet.SetColumnWidth(2, 10);
            worksheet.SetColumnWidth(2, 11);
            worksheet.SetColumnWidth(2, 12);
            worksheet.SetColumnWidth(2.1428, 13);
            worksheet.SetColumnWidth(2.4285, 14);
            worksheet.SetColumnWidth(2.4285, 15);
            worksheet.SetColumnWidth(2.4285, 16);
            worksheet.SetColumnWidth(2, 17);
            worksheet.SetColumnWidth(3, 18);
            worksheet.SetColumnWidth(3, 19);
            worksheet.SetColumnWidth(2.4285, 20);
            worksheet.SetColumnWidth(2.4285, 21);
            worksheet.SetColumnWidth(2.4285, 22);
            worksheet.SetColumnWidth(3, 23);
            worksheet.SetColumnWidth(3, 24);
            worksheet.SetColumnWidth(3, 25);
            worksheet.SetColumnWidth(3, 26);
            worksheet.SetColumnWidth(3, 27);
            worksheet.SetColumnWidth(3, 28);
            worksheet.SetColumnWidth(3, 29);
            worksheet.SetColumnWidth(3, 30);
            worksheet.SetColumnWidth(3, 31);
            worksheet.SetColumnWidth(3, 32);
            worksheet.SetColumnWidth(3, 33);
            worksheet.SetColumnWidth(3, 34);
            worksheet.SetColumnWidth(3, 35);
            worksheet.SetColumnWidth(3, 36);
            worksheet.SetColumnWidth(3, 37);

            //Document header

            //Setting document header height
            worksheet.SetRowHeight(10.606, 1);
            worksheet.SetRowHeight(13, 2);
            worksheet.SetRowHeight(6.0606, 3);
            worksheet.SetRowHeight(11.3636, 4);
            worksheet.SetRowHeight(12.1212, 5);
            worksheet.SetRowHeight(15.1515, 6);
            worksheet.SetRowHeight(11.3636, 7);
            worksheet.SetRowHeight(12.1212, 8);
            worksheet.SetRowHeight(11.3636, 9);
            worksheet.SetRowHeight(11.3636, 10);
            worksheet.SetRowHeight(21.2121, 11);
            worksheet.SetRowHeight(11.3636, 12);
            worksheet.SetRowHeight(12.8788, 13);
            worksheet.SetRowHeight(12.1212, 14);
            worksheet.SetRowHeight(3.7879, 15);
            worksheet.SetRowHeight(13, 16);
            worksheet.SetRowHeight(3.7878, 17);
            worksheet.SetRowHeight(11.3636, 18);
            worksheet.SetRowHeight(11.3636, 19);

            using (ExcelRange range = worksheet.Cells[2, 20, 2, 29]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Font.Name = "Arial";
                range.Value = "ЗАТВЕРДЖУЮ";
            }

            using (ExcelRange range = worksheet.Cells[5, 20, 5, 32]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.Font.Name = "Arial";
                range.Style.WrapText = true;
                range.Value = depreciatedOrder.Organization.FullName ?? depreciatedOrder.Organization.Name;
            }

            using (ExcelRange range = worksheet.Cells[6, 20, 6, 27]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[6, 28, 6, 31]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[11, 2, 11, 36]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.Font.Name = "Arial";
                range.Value =
                    $"Акт про списання товарів № {depreciatedOrder.Number} від {depreciatedOrder.FromDate.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("uk-UA"))} р.";
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[11, 37, 11, 37]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[13, 2, 13, 6]) {
                range.Merge = true;
                range.Value = "Організація: ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[13, 7, 13, 36]) {
                range.Merge = true;
                range.Value = depreciatedOrder.Organization.FullName ?? depreciatedOrder.Organization.Name;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[16, 2, 16, 6]) {
                range.Merge = true;
                range.Value = "Склад: ";
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[16, 7, 16, 36]) {
                range.Merge = true;
                range.Value = depreciatedOrder.Storage.Name;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[18, 2, 19, 3]) {
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

            using (ExcelRange range = worksheet.Cells[18, 4, 19, 6]) {
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

            using (ExcelRange range = worksheet.Cells[18, 7, 19, 24]) {
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

            using (ExcelRange range = worksheet.Cells[18, 25, 19, 29]) {
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

            using (ExcelRange range = worksheet.Cells[18, 30, 19, 32]) {
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

            using (ExcelRange range = worksheet.Cells[18, 33, 19, 36]) {
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

            using (ExcelRange range = worksheet.Cells[18, 2, 19, 36]) {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(238, 238, 238));
            }

            int row = 20;

            int counter = 1;

            foreach (DepreciatedOrderItem depreciatedOrderItem in depreciatedOrder.DepreciatedOrderItems) {
                decimal price = 0;
                decimal totalDepreciatedPriceItem = 0;

                foreach (ConsignmentItemMovement movement in depreciatedOrderItem.ConsignmentItemMovements) {
                    depreciatedOrderItem.Qty -= movement.Qty;

                    worksheet.SetRowHeight(11.3636, row);

                    price = movement.ConsignmentItem.Price;

                    decimal totalPriceItem =
                        decimal.Round(
                            Convert.ToDecimal(movement.Qty) * movement.ConsignmentItem.Price,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    totalAmount += totalPriceItem;

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
                        range.Value = depreciatedOrderItem.Product.VendorCode;
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

                    using (ExcelRange range = worksheet.Cells[row, 7, row, 24]) {
                        range.Merge = true;
                        range.Value = depreciatedOrderItem.Product.Name;
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

                    using (ExcelRange range = worksheet.Cells[row, 25, row, 27]) {
                        range.Merge = true;
                        range.Value = movement.Qty;
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

                    using (ExcelRange range = worksheet.Cells[row, 28, row, 29]) {
                        range.Merge = true;
                        range.Value = depreciatedOrderItem.Product.MeasureUnit.Name;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                        range.Merge = true;
                        range.Value = movement.ConsignmentItem.Price;
                        range.Style.Font.Name = "Arial";
                        range.Style.Font.Size = 8;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 33, row, 36]) {
                        range.Merge = true;
                        range.Value = totalPriceItem;
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

                    counter++;
                    row++;
                }

                totalDepreciatedPriceItem =
                    decimal.Round(
                        Convert.ToDecimal(depreciatedOrderItem.Qty) * price,
                        2,
                        MidpointRounding.AwayFromZero
                    );

                totalAmount += totalDepreciatedPriceItem;

                if (depreciatedOrderItem.Qty <= 0d) continue;

                worksheet.SetRowHeight(11.3636, row);

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
                    range.Value = depreciatedOrderItem.Product.VendorCode;
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

                using (ExcelRange range = worksheet.Cells[row, 7, row, 24]) {
                    range.Merge = true;
                    range.Value = depreciatedOrderItem.Product.Name;
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

                using (ExcelRange range = worksheet.Cells[row, 25, row, 27]) {
                    range.Merge = true;
                    range.Value = depreciatedOrderItem.Qty;
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

                using (ExcelRange range = worksheet.Cells[row, 28, row, 29]) {
                    range.Merge = true;
                    range.Value = depreciatedOrderItem.Product.MeasureUnit.Name;
                    range.Style.Font.Name = "Arial";
                    range.Style.Font.Size = 8;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                    range.Merge = true;
                    range.Value = price;
                    range.Style.Font.Name = "Arial";
                    range.Style.Font.Size = 8;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 33, row, 36]) {
                    range.Merge = true;
                    range.Value = totalDepreciatedPriceItem;
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

                counter++;
                row++;
            }

            using (ExcelRange range = worksheet.Cells[row - 1, 2, row - 1, 36]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            worksheet.SetRowHeight(6.0606, row);

            worksheet.SetRowHeight(12.8788, row + 1);
            worksheet.SetRowHeight(6.8182, row + 2);
            worksheet.SetRowHeight(12.8788, row + 3);
            worksheet.SetRowHeight(12.8788, row + 4);
            worksheet.SetRowHeight(6.8182, row + 5);
            worksheet.SetRowHeight(11.3636, row + 6);
            worksheet.SetRowHeight(12.8788, row + 7);
            worksheet.SetRowHeight(12.1212, row + 8);
            worksheet.SetRowHeight(12.8788, row + 9);
            worksheet.SetRowHeight(11.3636, row + 10);
            worksheet.SetRowHeight(12.8788, row + 11);
            worksheet.SetRowHeight(12.1212, row + 12);
            worksheet.SetRowHeight(12.1212, row + 13);
            worksheet.SetRowHeight(12.1212, row + 14);
            worksheet.SetRowHeight(12.1212, row + 15);
            worksheet.SetRowHeight(12.1212, row + 16);
            worksheet.SetRowHeight(12.1212, row + 17);

            using (ExcelRange range = worksheet.Cells[row + 1, 29, row + 1, 32]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Value = "Разом:";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row + 1, 33, row + 1, 36]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Value = totalAmount;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row + 3, 2, row + 3, 36]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Value = string.Format("Всього найменувань {0}, на суму {1} {2}.",
                    depreciatedOrder.DepreciatedOrderItems.Count, string.Format("{0:0.00}", totalAmount), "EUR");
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 4, 2, row + 4, 36]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Value = totalAmount.ToCompleteText("EUR", false, true, true);
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row + 5, 2, row + 5, 37]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[row + 8, 3, row + 8, 7]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Голова комісії:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 8, 9, row + 8, 16]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 8, 19, row + 8, 21]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 8, 23, row + 8, 33]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 9, 9, row + 9, 16]) {
                range.Merge = true;
                range.Value = "посада";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[row + 9, 19, row + 9, 21]) {
                range.Merge = true;
                range.Value = "підпис";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[row + 9, 23, row + 9, 33]) {
                range.Merge = true;
                range.Value = "і., по б, прізвище";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[row + 11, 2, row + 11, 7]) {
                range.Merge = true;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 9;
                range.Value = "Члени комісії:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row + 11, 9, row + 11, 16]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 11, 19, row + 11, 21]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 11, 23, row + 11, 33]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 12, 9, row + 12, 16]) {
                range.Merge = true;
                range.Value = "посада";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[row + 12, 19, row + 12, 21]) {
                range.Merge = true;
                range.Value = "підпис";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[row + 12, 23, row + 12, 33]) {
                range.Merge = true;
                range.Value = "і., по б, прізвище";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[row + 13, 9, row + 13, 16]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 13, 19, row + 13, 21]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 13, 23, row + 13, 33]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 14, 9, row + 14, 16]) {
                range.Merge = true;
                range.Value = "посада";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[row + 14, 19, row + 14, 21]) {
                range.Merge = true;
                range.Value = "підпис";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[row + 14, 23, row + 14, 33]) {
                range.Merge = true;
                range.Value = "і., по б, прізвище";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[row + 15, 9, row + 15, 16]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 15, 19, row + 15, 21]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 15, 23, row + 15, 33]) {
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 16, 9, row + 16, 16]) {
                range.Merge = true;
                range.Value = "посада";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[row + 16, 19, row + 16, 21]) {
                range.Merge = true;
                range.Value = "підпис";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[row + 16, 23, row + 16, 33]) {
                range.Merge = true;
                range.Value = "і., по б, прізвище";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Name = "Arial";
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            package.Workbook.Properties.Title = "Tax Free Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }
}