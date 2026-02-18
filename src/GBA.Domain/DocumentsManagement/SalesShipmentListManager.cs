using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Sales.Shipments;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public sealed class SalesShipmentListManager : BaseXlsManager, ISalesShipmentListManager {
    public (string xlsxFile, string pdfFile) ExportAllSalesShipmentsToXlsx(string path, IEnumerable<ShipmentList> shipmentLists, IEnumerable<DocumentMonth> months) {
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
                range.Value = "Вантажоодержувач";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 2, 4]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "К-сть місць";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 5, 2, 5]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Коментар";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 6, 2, 6]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Телефон";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            int row = 3;
            int count = 1;
            foreach (ShipmentList shipmentList in shipmentLists)
            foreach (ShipmentListItem item in shipmentList.ShipmentListItems) {
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
                    range.Value = item.Sale.DeliveryRecipient.FullName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }


                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = item.QtyPlaces;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = item.Sale.Comment;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = item.Sale.DeliveryRecipient.MobilePhone;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                row++;
                count++;
            }

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportSalesShipmentsToXlsx(string path, ShipmentList shipmentList, IEnumerable<DocumentMonth> months) {
        string fileName = Path.Combine(path, $"ShipmentList_{DateTime.Now.ToString("MM.yyyy")}_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("ShipmentList Document");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(1.4286, 1);
            worksheet.SetColumnWidth(10, 2);
            worksheet.SetColumnWidth(50, 3);
            worksheet.SetColumnWidth(10, 4);
            worksheet.SetColumnWidth(35.8572, 5);
            worksheet.SetColumnWidth(11.7143, 6);
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
                range.Value = "Вантажоодержувач";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 2, 4]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "К-сть місць";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 5, 2, 5]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Коментар";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 6, 2, 6]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Телефон";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            int row = 3;
            int count = 1;

            foreach (ShipmentListItem shipmentListItem in shipmentList.ShipmentListItems.Where(x => !x.IsChangeTransporter)) {
                using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = count;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    if (shipmentListItem.IsChangeTransporter) range.Style.Font.Strike = true;
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    if (shipmentListItem.Sale.WarehousesShipment != null)
                        range.Value = shipmentListItem.Sale.WarehousesShipment.FullName;
                    else
                        range.Value = shipmentListItem.Sale.DeliveryRecipient.FullName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    if (shipmentListItem.IsChangeTransporter) range.Style.Font.Strike = true;
                }

                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = shipmentListItem.QtyPlaces;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    if (shipmentListItem.IsChangeTransporter) range.Style.Font.Strike = true;
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    string value = string.Empty;

                    if (shipmentListItem.Sale.IsCashOnDelivery)
                        value += $" накладний платіж: {decimal.Round(shipmentListItem.Sale.CashOnDeliveryAmount, 2, MidpointRounding.AwayFromZero)}. ";


                    if (shipmentListItem.Sale.WarehousesShipment != null) {
                        if (shipmentListItem.Sale.WarehousesShipment.Number != null) value += $" ТТН: {shipmentListItem.Sale.WarehousesShipment.Number}. ";

                        if (!string.IsNullOrEmpty(shipmentListItem.Sale.WarehousesShipment.Comment)) value += $" коментар: {shipmentListItem.Sale.WarehousesShipment.Comment}. ";
                    } else {
                        if (shipmentListItem.Sale.CustomersOwnTtn != null) value += $" ТТН: {shipmentListItem.Sale.CustomersOwnTtn.Number}. ";

                        if (!string.IsNullOrEmpty(shipmentListItem.Sale.Comment)) value += $" коментар: {shipmentListItem.Sale.Comment}. ";
                    }

                    range.Merge = true;
                    range.Style.WrapText = true;

                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = value;

                    // TODO find a better way to fit long comments
                    if (value.Length > 25)
                        worksheet.Row(row).Height = 44;

                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    if (shipmentListItem.IsChangeTransporter) range.Style.Font.Strike = true;
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    if (shipmentListItem.Sale.WarehousesShipment != null)
                        range.Value = shipmentListItem.Sale.WarehousesShipment.MobilePhone;
                    else
                        range.Value = shipmentListItem.Sale.DeliveryRecipient.MobilePhone;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    if (shipmentListItem.IsChangeTransporter) range.Style.Font.Strike = true;
                }

                row++;
                count++;
            }

            package.Save();
        }

        return SaveFiles(fileName);
    }
}