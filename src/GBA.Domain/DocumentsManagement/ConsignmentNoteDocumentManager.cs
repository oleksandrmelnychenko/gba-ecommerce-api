using System;
using System.Globalization;
using System.IO;
using System.Linq;
using GBA.Common.Extensions;
using GBA.Common.Helpers;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.ConsignmentNoteSettings;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.ReSaleModels;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public sealed class ConsignmentNoteDocumentManager : BaseXlsManager, IConsignmentNoteDocumentManager {
    public (string, string) GetPrintSaleConsignmentNoteDocument(string path, Sale sale, ConsignmentNoteSetting setting) {
        string fileName = Path.Combine(path, $"{setting.Name}_{Guid.NewGuid()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("TTN");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true, 87, eOrientation.Portrait);

            worksheet.Row(37).PageBreak = true;
            //7
            worksheet.SetColumnWidth(1, 1);
            worksheet.SetColumnWidth(1, 2);
            worksheet.SetColumnWidth(0.2857, 3);
            worksheet.SetColumnWidth(0.7143, 4);
            worksheet.SetColumnWidth(3.5714, 5);
            worksheet.SetColumnWidth(4.5714, 6);
            worksheet.SetColumnWidth(1.4286, 7);
            worksheet.SetColumnWidth(0.5714, 8);
            worksheet.SetColumnWidth(2.1429, 9);
            worksheet.SetColumnWidth(3.8572, 10);
            worksheet.SetColumnWidth(2, 11);
            worksheet.SetColumnWidth(4.1429, 12);
            worksheet.SetColumnWidth(0.7142, 13);
            worksheet.SetColumnWidth(1.2857, 14);
            worksheet.SetColumnWidth(5.5714, 15);
            worksheet.SetColumnWidth(2.4286, 16);
            worksheet.SetColumnWidth(4.2857, 17);
            worksheet.SetColumnWidth(0.4286, 18);
            worksheet.SetColumnWidth(6.4286, 19);
            worksheet.SetColumnWidth(1.2857, 20);
            worksheet.SetColumnWidth(0.4286, 21);
            worksheet.SetColumnWidth(0.4286, 22);
            worksheet.SetColumnWidth(0.2857, 23);
            worksheet.SetColumnWidth(3.8572, 24);
            worksheet.SetColumnWidth(3.7143, 25);
            worksheet.SetColumnWidth(0.2857, 26);
            worksheet.SetColumnWidth(3.619, 27);
            worksheet.SetColumnWidth(1.2382, 28);
            worksheet.SetColumnWidth(3, 29);
            worksheet.SetColumnWidth(1.1429, 30);
            worksheet.SetColumnWidth(3.4286, 31);
            worksheet.SetColumnWidth(1.2857, 32);
            worksheet.SetColumnWidth(2, 33);
            worksheet.SetColumnWidth(0.5714, 34);
            worksheet.SetColumnWidth(4.7143, 35);
            worksheet.SetColumnWidth(3.1429, 36);
            worksheet.SetColumnWidth(0.2857, 37);
            worksheet.SetColumnWidth(5, 38);
            worksheet.SetColumnWidth(0.1429, 39);
            worksheet.SetColumnWidth(0.5714, 40);
            worksheet.SetColumnWidth(4.4286, 41);
            worksheet.SetColumnWidth(2.1429, 42);
            worksheet.SetColumnWidth(1, 43);
            worksheet.SetColumnWidth(1.2857, 44);
            worksheet.SetColumnWidth(0.7143, 45);
            worksheet.SetColumnWidth(5, 46);
            worksheet.SetColumnWidth(1, 47);
            worksheet.SetColumnWidth(2, 48);
            worksheet.SetColumnWidth(0.8572, 49);
            worksheet.SetColumnWidth(4.1429, 50);
            worksheet.SetColumnWidth(1.2857, 51);
            worksheet.SetColumnWidth(3.8571, 52);
            worksheet.SetColumnWidth(1, 53);
            worksheet.SetColumnWidth(2, 54);
            worksheet.SetColumnWidth(2.2857, 55);
            worksheet.SetColumnWidth(0.5714, 56);
            worksheet.SetColumnWidth(3.1429, 57);
            worksheet.SetColumnWidth(0.2857, 58);
            worksheet.SetColumnWidth(3.0715, 59);
            worksheet.SetColumnWidth(6.0715, 60);
            worksheet.SetColumnWidth(5.2857, 61);
            worksheet.SetColumnWidth(1, 62);
            worksheet.SetColumnWidth(2, 63);
            worksheet.SetColumnWidth(3, 64);
            worksheet.SetColumnWidth(4.1429, 65);
            worksheet.SetColumnWidth(3.4286, 66);
            worksheet.SetColumnWidth(0.5714, 67);
            worksheet.SetColumnWidth(1.7143, 68);
            worksheet.SetColumnWidth(0.8572, 69);
            worksheet.SetColumnWidth(0.1, 70);
            worksheet.SetColumnWidth(9.1428, 71);

            //1.32
            worksheet.SetRowHeight(12.1212, 1);
            worksheet.SetRowHeight(12.1212, 2);
            worksheet.SetRowHeight(12.1212, 3);
            worksheet.SetRowHeight(12.1212, 4);
            worksheet.SetRowHeight(12.1212, 5);
            worksheet.SetRowHeight(15.1515, 6);
            worksheet.SetRowHeight(13.6364, 7);
            worksheet.SetRowHeight(11.3636, 8);
            worksheet.SetRowHeight(10.6061, 9);
            worksheet.SetRowHeight(10.6061, 10);
            worksheet.SetRowHeight(13.6364, 11);
            worksheet.SetRowHeight(11.3636, 12);
            worksheet.SetRowHeight(6.0606, 13);
            worksheet.SetRowHeight(12.8788, 14);
            worksheet.SetRowHeight(12.1212, 15);
            worksheet.SetRowHeight(6.0606, 16);
            worksheet.SetRowHeight(12.9546, 17);
            worksheet.SetRowHeight(12.1212, 18);
            worksheet.SetRowHeight(5.3030, 19);
            worksheet.SetRowHeight(5.3030, 19);
            worksheet.SetRowHeight(21.9697, 20);
            worksheet.SetRowHeight(12.1212, 21);
            worksheet.SetRowHeight(12.1212, 21);
            worksheet.SetRowHeight(6.8182, 22);
            worksheet.SetRowHeight(21.9697, 23);
            worksheet.SetRowHeight(11.3636, 24);
            worksheet.SetRowHeight(6.0606, 25);
            worksheet.SetRowHeight(12.8788, 26);
            worksheet.SetRowHeight(10.6060, 27);
            worksheet.SetRowHeight(7.5758, 28);
            worksheet.SetRowHeight(12.1212, 29);
            worksheet.SetRowHeight(12.1212, 30);
            worksheet.SetRowHeight(6.8182, 31);
            worksheet.SetRowHeight(12.1212, 32);
            worksheet.SetRowHeight(12.1212, 33);
            worksheet.SetRowHeight(5.3030, 34);
            worksheet.SetRowHeight(12.1212, 35);
            worksheet.SetRowHeight(12.1212, 36);
            worksheet.SetRowHeight(6.8182, 37);
            worksheet.SetRowHeight(15.1515, 38);
            worksheet.SetRowHeight(11.3636, 39);
            worksheet.SetRowHeight(46.2121, 40);
            worksheet.SetRowHeight(12.1212, 41);

            using (ExcelRange range = worksheet.Cells[1, 42, 1, 42]) {
                range.Value = "Додаток 7";
            }

            using (ExcelRange range = worksheet.Cells[2, 42, 2, 42]) {
                range.Value = "до Правил перевезень вантажів автомобільним";
            }

            using (ExcelRange range = worksheet.Cells[3, 42, 3, 42]) {
                range.Value = "транспортом в Україні";
            }

            using (ExcelRange range = worksheet.Cells[4, 42, 4, 42]) {
                range.Value = "(пункт 11.1 глави 11)";
            }

            using (ExcelRange range = worksheet.Cells[1, 42, 4, 42]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[6, 4, 6, 66]) {
                range.Merge = true;
                range.Value = "ТОВАРНО-ТРАНСПОРТНА НАКЛАДНА";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 11;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[7, 29, 7, 29]) {
                range.Value = "№";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[7, 30, 7, 33]) {
                range.Value = setting.Number;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[7, 34, 7, 43]) {
                range.Value = sale.ChangedToInvoice.Value.ToString("dd MMMM yyyy", CultureInfo.CurrentCulture) + " року";
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[9, 34, 9, 39]) {
                range.Value = "Форма N 1-ТН";
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[11, 4, 11, 4]) {
                range.Value = "Автомобіль";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[11, 9, 11, 24]) {
                range.Value = setting.BrandAndNumberCar;
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[11, 25, 11, 31]) {
                range.Value = "Причіп/напівпричіп";
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[11, 32, 11, 45]) {
                range.Value = setting.TrailerNumber;
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[11, 53, 11, 53]) {
                range.Value = "Вид перевезень";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[11, 54, 11, 66]) {
                range.Value = setting.TypeTransportation;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[14, 4, 14, 13]) {
                range.Value = "Автомобільний перевізник";
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[14, 14, 14, 14]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[14, 15, 14, 15]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[14, 16, 14, 40]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = setting.Carrier;
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[14, 42, 14, 45]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Водій";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[14, 46, 14, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = setting.Driver;
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[15, 16, 15, 40]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(найменування / П. І. Б.)";
            }

            using (ExcelRange range = worksheet.Cells[15, 46, 15, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(найменування / П. І. Б.)";
            }

            using (ExcelRange range = worksheet.Cells[17, 4, 17, 11]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Замовник";
            }

            using (ExcelRange range = worksheet.Cells[17, 12, 17, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = setting.Customer;
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[18, 12, 18, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(найменування / П. І. Б.)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[20, 4, 20, 11]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Вантажовідправник";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[20, 12, 20, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = $"{sale.ClientAgreement.Agreement.Organization.FullName}, " +
                              $"{sale.ClientAgreement.Agreement.Organization.Address}, тел.:" +
                              $"{sale.ClientAgreement.Agreement.Organization.PhoneNumber}";
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[21, 12, 21, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(повне найменування, місцезнаходження / П. І. Б., місце проживання)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[23, 4, 23, 11]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Вантажоодержувач";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[23, 12, 23, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = $"{sale.ClientAgreement.Client.FullName}, " +
                              $"{sale.ClientAgreement.Client.ActualAddress}, тел.:" +
                              $"{sale.ClientAgreement.Client.MobileNumber}";
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[24, 12, 24, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(повне найменування, місцезнаходження / П. І. Б., місце проживання)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[26, 4, 26, 11]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Пункт навантаження";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[26, 12, 26, 31]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = setting.LoadingPoint;
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[26, 32, 26, 41]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Пункт розвантаження";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[26, 42, 26, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = setting.UnloadingPoint;
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[27, 12, 27, 31]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(місцезнаходження)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[27, 42, 27, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(місцезнаходження)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[29, 4, 29, 4]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Value = "кількість місць";
            }

            using (ExcelRange range = worksheet.Cells[29, 10, 29, 20]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = Convert.ToDecimal(sale.Order.OrderItems.Count).ToText(true, true);
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[29, 21, 29, 27]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Value = ", масою брутто, т";
                range.Merge = true;
            }

            double totalWeight =
                Math.Round(
                    sale.Order.OrderItems.Sum(x => x.Product.Weight * x.Qty),
                    3,
                    MidpointRounding.AwayFromZero
                );

            decimal totalPriceWithVat =
                Math.Round(
                    sale.Order.OrderItems.Sum(x => x.Product.CurrentLocalPrice * Convert.ToDecimal(x.Qty)),
                    2,
                    MidpointRounding.AwayFromZero
                );

            decimal vatRate =
                sale.ClientAgreement.Agreement.Organization.VatRate != null
                    ? Convert.ToDecimal(sale.ClientAgreement.Agreement.Organization.VatRate.Value) / 100
                    : 0;

            decimal totalVat = Math.Round(
                totalPriceWithVat * (vatRate / (1 + vatRate)),
                2,
                MidpointRounding.AwayFromZero);

            double qtyT = totalWeight / 1000;
            double qtyKg = (qtyT - (int)qtyT) * 1000;

            using (ExcelRange range = worksheet.Cells[29, 28, 29, 36]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Value = Convert.ToDecimal(qtyT).ToText(false, true) +
                              " т. " + qtyKg + " кг";
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[29, 37, 29, 37]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Value = ", отримав водій/експедитор";
            }

            using (ExcelRange range = worksheet.Cells[29, 48, 29, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = setting.Driver;
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[30, 10, 30, 20]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(словами)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[30, 28, 30, 36]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(словами)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[30, 48, 30, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(П. І. Б., посада, підпис)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[32, 4, 32, 4]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Value = "Усього відпущено на загальну суму";
            }


            using (ExcelRange range = worksheet.Cells[32, 16, 32, 41]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = $"{totalPriceWithVat.ToCompleteText(sale.ClientAgreement.Agreement.Currency.Code, true, true, true)}";
                range.Style.WrapText = true;
                range.Style.Font.Bold = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[32, 42, 32, 49]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Value = ", у тому числі ПДВ";
                range.Merge = true;
            }

            using (ExcelRange range = worksheet.Cells[32, 50, 32, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                if (sale.IsVatSale)
                    range.Value = totalVat + " грн";
                else
                    range.Value = "";
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[35, 4, 35, 4]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Value = "Супровідні документи на вантаж";
            }

            using (ExcelRange range = worksheet.Cells[35, 16, 35, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = $"Видаткова накладна {sale.SaleNumber.Value} від {sale.Created.ToString("dd.MM.yy")}";
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[36, 4, 36, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[38, 2, 38, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "ВІДОМОСТІ ПРО ВАНТАЖ";
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[40, 4, 40, 5]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "№ з/п";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 6, 40, 19]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Найменування вантажу (номер контейнера), у разі перевезення небезпечних вантажів: " +
                              "клас небезпечних речовин, до якого віднесено вантаж";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 20, 40, 26]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Одиниця виміру";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 27, 40, 31]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Кількість місць";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 32, 40, 39]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Ціна без ПДВ за одиницю, грн";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 40, 40, 49]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Загальна сума з ПДВ, грн";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 50, 40, 55]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Вид пакування";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 56, 40, 61]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Документи з вантажем";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 62, 40, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Маса брутто, т";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 4, 41, 5]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "1";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 6, 41, 19]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "2";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 20, 41, 26]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "3";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 27, 41, 31]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "4";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 32, 41, 39]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "5";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 40, 41, 49]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "6";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 50, 41, 55]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "7";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 56, 41, 61]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "8";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 62, 41, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "9";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            int row = 42;

            int index = 1;

            foreach (OrderItem item in sale.Order.OrderItems) {
                using (ExcelRange range = worksheet.Cells[row, 4, row, 5]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = index;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 19]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = item.Product.Name;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 20, row, 26]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = item.Product.MeasureUnit.Name;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 27, row, 31]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = item.Qty;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 32, row, 39]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    if (sale.IsVatSale)
                        range.Value = decimal.Round(
                            item.Product.CurrentLocalPrice - item.Product.CurrentLocalPrice * (vatRate / (vatRate + 1)),
                            2,
                            MidpointRounding.AwayFromZero
                        );
                    else
                        range.Value = decimal.Round(
                            item.Product.CurrentLocalPrice,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 40, row, 49]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = decimal.Round(
                        item.Product.CurrentLocalPrice * Convert.ToDecimal(item.Qty),
                        2,
                        MidpointRounding.AwayFromZero
                    );
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 50, row, 55]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = "упак";
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 56, row, 61]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = "";
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 62, row, 66]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = Math.Round(item.Qty * item.Product.Weight / 1000, 3, MidpointRounding.AwayFromZero);
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                worksheet.SetRowHeight(12.1212, row);
                row++;
                index++;
            }

            using (ExcelRange range = worksheet.Cells[row, 4, row, 19]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Усього:";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 20, row, 26]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 27, row, 31]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = sale.Order.OrderItems.Sum(x => x.Qty);
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 39]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 40, row, 49]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = decimal.Round(totalPriceWithVat, 2, MidpointRounding.AwayFromZero);
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 50, row, 55]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 56, row, 61]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 62, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = Math.Round(qtyT, 3, MidpointRounding.AwayFromZero);
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 4, row, 64]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            }

            row++;
            using (ExcelRange range = worksheet.Cells[row, 5, row, 25]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Здав (відповідальна особа вантажовідправника)";
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 57]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Прийняв (відповідальна особа вантажоодержувача)";
            }

            row++;

            worksheet.SetRowHeight(15, row);

            using (ExcelRange range = worksheet.Cells[row, 5, row, 25]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Директор " + sale.ClientAgreement.Agreement.Organization.Manager;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 57]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Керівник " + sale.ClientAgreement.Client.Manager;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 5, row, 25]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "(П. І. Б., посада, підпис)";
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 57]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "(П. І. Б., посада, підпис)";
            }

            row++;
            row++;

            using (ExcelRange range = worksheet.Cells[row, 4, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Value = "ВАНТАЖНО-РОЗВАНТАЖУВАЛЬНІ ОПЕРАЦІЇ";
            }

            row++;
            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row + 2, 12]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Операція";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row + 2, 23]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Маса брутто, т";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 24, row, 54]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Час (год. хв.)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 55, row + 2, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Підпис відповідальної особи";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 24, row + 1, 32]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "прибуття";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row + 1, 44]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "вибуття";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 45, row + 1, 54]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "простою";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;
            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 12]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "10";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row, 23]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "11";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 24, row, 32]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "12";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 44]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "13";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 45, row, 54]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "14";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 55, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "15";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 12]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Навантаження";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row, 23]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 24, row, 32]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 44]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 45, row, 54]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 55, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 12]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Розвантаження";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row, 23]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 24, row, 32]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 44]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 45, row, 54]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 55, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 4, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 4, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Value = "ГАБАРИТНО-ВАГОВІ ПАРАМЕТРИ ТРАНСПОРТНОГО ЗАСОБУ";
            }

            row++;
            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row + 1, 18]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Транспортний засіб";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 19, row, 46]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Габарити, мм";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 47, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Вага, т";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 19, row, 27]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Довжина";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 36]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Ширина";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 46]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Висота";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 47, row, 59]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "без вантажу";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 60, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "загальна";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 18]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "16";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 19, row, 27]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "17";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 36]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "18";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 46]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "19";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 47, row, 59]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "20";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 60, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "21";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 18]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.CarLabel;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 19, row, 27]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.CarLength.Equals(0) ? "" : setting.CarLength.ToString();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 36]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.CarWidth.Equals(0) ? "" : setting.CarWidth.ToString();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 46]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.CarHeight.Equals(0) ? "" : setting.CarHeight.ToString();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 47, row, 59]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.CarNetWeight.Equals(0) ? "" : setting.CarNetWeight.ToString(CultureInfo.CreateSpecificCulture("de-DE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            decimal grossWeight = decimal.Round((setting.CarNetWeight * 1000 + Convert.ToDecimal(sale.TotalWeight)) / 1000, 3, MidpointRounding.AwayFromZero);

            using (ExcelRange range = worksheet.Cells[row, 60, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.CarGrossWeight.Equals(0m)
                    ? grossWeight.ToString(CultureInfo.CreateSpecificCulture("de-DE"))
                    : setting.CarGrossWeight.ToString(CultureInfo.CreateSpecificCulture("de-DE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 18]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.TrailerLabel;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 19, row, 27]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.TrailerLength.Equals(0) ? "" : setting.TrailerLength.ToString();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 36]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.TrailerWidth.Equals(0) ? "" : setting.TrailerWidth.ToString();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 46]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.TrailerHeight.Equals(0) ? "" : setting.TrailerHeight.ToString();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 47, row, 59]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.TrailerNetWeight.Equals(0) ? "" : setting.TrailerNetWeight.ToString(CultureInfo.CreateSpecificCulture("de-DE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 60, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.TrailerGrossWeight.Equals(0) ? "" : setting.TrailerGrossWeight.ToString(CultureInfo.CreateSpecificCulture("de-DE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 4, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(12.1212, new[] {
                row, row + 1, row + 2, row + 3, row + 4, row + 6, row + 7, row + 8, row + 9,
                row + 10, row + 11, row + 12, row + 13
            });

            using (ExcelRange range = worksheet.Cells[1, 1, row, 69]) {
                range.Style.Font.Name = "Arial";
                range.Style.Border.BorderAround(ExcelBorderStyle.None);
            }

            package.Workbook.Properties.Title = "TTN Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        string pdfFile = string.Empty;

        try {
            pdfFile = HelperXlsManager.ConvertXlsxToPDF(fileName);
        } catch (Exception exc) {
            string logPath = Path.Combine(NoltFolderManager.GetDataFolderPath(), "excel_error_log.txt");

            File.AppendAllText(
                logPath,
                string.Format(
                    "\r\n{0}\r\n{1}\r\n",
                    exc.Message,
                    exc.InnerException != null ? exc.InnerException?.Message : string.Empty
                )
            );
        }

        return (fileName, pdfFile);
    }

    public (string, string) GetPrintReSaleConsignmentNoteDocument(string path, UpdatedReSaleModel reSale, ConsignmentNoteSetting setting) {
        string fileName = Path.Combine(path, $"{setting.Name}_{Guid.NewGuid()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("TTN");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true, 87, eOrientation.Portrait);

            worksheet.Row(37).PageBreak = true;

            //7
            worksheet.SetColumnWidth(1, 1);
            worksheet.SetColumnWidth(1, 2);
            worksheet.SetColumnWidth(0.2857, 3);
            worksheet.SetColumnWidth(0.7143, 4);
            worksheet.SetColumnWidth(3.5714, 5);
            worksheet.SetColumnWidth(4.5714, 6);
            worksheet.SetColumnWidth(1.4286, 7);
            worksheet.SetColumnWidth(0.5714, 8);
            worksheet.SetColumnWidth(2.1429, 9);
            worksheet.SetColumnWidth(3.8572, 10);
            worksheet.SetColumnWidth(2, 11);
            worksheet.SetColumnWidth(4.1429, 12);
            worksheet.SetColumnWidth(0.7142, 13);
            worksheet.SetColumnWidth(1.2857, 14);
            worksheet.SetColumnWidth(5.5714, 15);
            worksheet.SetColumnWidth(2.4286, 16);
            worksheet.SetColumnWidth(4.2857, 17);
            worksheet.SetColumnWidth(0.4286, 18);
            worksheet.SetColumnWidth(6.4286, 19);
            worksheet.SetColumnWidth(1.2857, 20);
            worksheet.SetColumnWidth(0.4286, 21);
            worksheet.SetColumnWidth(0.4286, 22);
            worksheet.SetColumnWidth(0.2857, 23);
            worksheet.SetColumnWidth(3.8572, 24);
            worksheet.SetColumnWidth(3.7143, 25);
            worksheet.SetColumnWidth(0.2857, 26);
            worksheet.SetColumnWidth(3.619, 27);
            worksheet.SetColumnWidth(1.2382, 28);
            worksheet.SetColumnWidth(3, 29);
            worksheet.SetColumnWidth(1.1429, 30);
            worksheet.SetColumnWidth(3.4286, 31);
            worksheet.SetColumnWidth(1.2857, 32);
            worksheet.SetColumnWidth(2, 33);
            worksheet.SetColumnWidth(0.5714, 34);
            worksheet.SetColumnWidth(4.7143, 35);
            worksheet.SetColumnWidth(3.1429, 36);
            worksheet.SetColumnWidth(0.2857, 37);
            worksheet.SetColumnWidth(5, 38);
            worksheet.SetColumnWidth(0.1429, 39);
            worksheet.SetColumnWidth(0.5714, 40);
            worksheet.SetColumnWidth(4.4286, 41);
            worksheet.SetColumnWidth(2.1429, 42);
            worksheet.SetColumnWidth(1, 43);
            worksheet.SetColumnWidth(1.2857, 44);
            worksheet.SetColumnWidth(0.7143, 45);
            worksheet.SetColumnWidth(5, 46);
            worksheet.SetColumnWidth(1, 47);
            worksheet.SetColumnWidth(2, 48);
            worksheet.SetColumnWidth(0.8572, 49);
            worksheet.SetColumnWidth(4.1429, 50);
            worksheet.SetColumnWidth(1.2857, 51);
            worksheet.SetColumnWidth(3.8571, 52);
            worksheet.SetColumnWidth(1, 53);
            worksheet.SetColumnWidth(2, 54);
            worksheet.SetColumnWidth(2.2857, 55);
            worksheet.SetColumnWidth(0.5714, 56);
            worksheet.SetColumnWidth(3.1429, 57);
            worksheet.SetColumnWidth(0.2857, 58);
            worksheet.SetColumnWidth(3.0715, 59);
            worksheet.SetColumnWidth(6.0715, 60);
            worksheet.SetColumnWidth(5.2857, 61);
            worksheet.SetColumnWidth(1, 62);
            worksheet.SetColumnWidth(2, 63);
            worksheet.SetColumnWidth(3, 64);
            worksheet.SetColumnWidth(4.1429, 65);
            worksheet.SetColumnWidth(3.4286, 66);
            worksheet.SetColumnWidth(0.5714, 67);
            worksheet.SetColumnWidth(1.7143, 68);
            worksheet.SetColumnWidth(0.8572, 69);
            worksheet.SetColumnWidth(0.1, 70);
            worksheet.SetColumnWidth(9.1428, 71);

            //1.32
            worksheet.SetRowHeight(12.1212, 1);
            worksheet.SetRowHeight(12.1212, 2);
            worksheet.SetRowHeight(12.1212, 3);
            worksheet.SetRowHeight(12.1212, 4);
            worksheet.SetRowHeight(12.1212, 5);
            worksheet.SetRowHeight(15.1515, 6);
            worksheet.SetRowHeight(13.6364, 7);
            worksheet.SetRowHeight(11.3636, 8);
            worksheet.SetRowHeight(10.6061, 9);
            worksheet.SetRowHeight(10.6061, 10);
            worksheet.SetRowHeight(13.6364, 11);
            worksheet.SetRowHeight(11.3636, 12);
            worksheet.SetRowHeight(6.0606, 13);
            worksheet.SetRowHeight(12.8788, 14);
            worksheet.SetRowHeight(12.1212, 15);
            worksheet.SetRowHeight(6.0606, 16);
            worksheet.SetRowHeight(12.9546, 17);
            worksheet.SetRowHeight(12.1212, 18);
            worksheet.SetRowHeight(5.3030, 19);
            worksheet.SetRowHeight(5.3030, 19);
            worksheet.SetRowHeight(21.9697, 20);
            worksheet.SetRowHeight(12.1212, 21);
            worksheet.SetRowHeight(12.1212, 21);
            worksheet.SetRowHeight(6.8182, 22);
            worksheet.SetRowHeight(21.9697, 23);
            worksheet.SetRowHeight(11.3636, 24);
            worksheet.SetRowHeight(6.0606, 25);
            worksheet.SetRowHeight(12.8788, 26);
            worksheet.SetRowHeight(10.6060, 27);
            worksheet.SetRowHeight(7.5758, 28);
            worksheet.SetRowHeight(12.1212, 29);
            worksheet.SetRowHeight(12.1212, 30);
            worksheet.SetRowHeight(6.8182, 31);
            worksheet.SetRowHeight(12.1212, 32);
            worksheet.SetRowHeight(12.1212, 33);
            worksheet.SetRowHeight(5.3030, 34);
            worksheet.SetRowHeight(12.1212, 35);
            worksheet.SetRowHeight(12.1212, 36);
            worksheet.SetRowHeight(6.8182, 37);
            worksheet.SetRowHeight(15.1515, 38);
            worksheet.SetRowHeight(11.3636, 39);
            worksheet.SetRowHeight(46.2121, 40);
            worksheet.SetRowHeight(12.1212, 41);

            using (ExcelRange range = worksheet.Cells[1, 42, 1, 42]) {
                range.Value = "Додаток 7";
            }

            using (ExcelRange range = worksheet.Cells[2, 42, 2, 42]) {
                range.Value = "до Правил перевезень вантажів автомобільним";
            }

            using (ExcelRange range = worksheet.Cells[3, 42, 3, 42]) {
                range.Value = "транспортом в Україні";
            }

            using (ExcelRange range = worksheet.Cells[4, 42, 4, 42]) {
                range.Value = "(пункт 11.1 глави 11)";
            }

            using (ExcelRange range = worksheet.Cells[1, 42, 4, 42]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[6, 4, 6, 66]) {
                range.Merge = true;
                range.Value = "ТОВАРНО-ТРАНСПОРТНА НАКЛАДНА";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 11;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[7, 27, 7, 27]) {
                range.Value = "№";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[7, 28, 7, 31]) {
                range.Value = setting.Number;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[7, 32, 7, 41]) {
                range.Value = DateTime.Now.ToString("dd MMMM yyyy", CultureInfo.CurrentCulture) + " року";
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 10;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[9, 32, 9, 37]) {
                range.Value = "Форма N 1-ТН";
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[11, 4, 11, 4]) {
                range.Value = "Автомобіль";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[11, 9, 11, 24]) {
                range.Value = setting.BrandAndNumberCar;
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[11, 25, 11, 31]) {
                range.Value = "Причіп/напівпричіп";
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[11, 32, 11, 45]) {
                range.Value = setting.TrailerNumber;
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[11, 53, 11, 53]) {
                range.Value = "Вид перевезень";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[11, 54, 11, 66]) {
                range.Value = setting.TypeTransportation;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[14, 4, 14, 13]) {
                range.Value = "Автомобільний перевізник";
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[14, 14, 14, 14]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[14, 15, 14, 15]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[14, 16, 14, 40]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = setting.Carrier;
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[14, 42, 14, 45]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Водій";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[14, 46, 14, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = setting.Driver;
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[15, 16, 15, 40]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(найменування / П. І. Б.)";
            }

            using (ExcelRange range = worksheet.Cells[15, 46, 15, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(найменування / П. І. Б.)";
            }

            using (ExcelRange range = worksheet.Cells[17, 4, 17, 11]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Замовник";
            }

            using (ExcelRange range = worksheet.Cells[17, 12, 17, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = setting.Customer;
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[18, 12, 18, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(найменування / П. І. Б.)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[20, 4, 20, 11]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Вантажовідправник";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[20, 12, 20, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = $"{reSale.ReSale.Organization.FullName}, " +
                              $"{reSale.ReSale.Organization.Address}, тел.:" +
                              $"{reSale.ReSale.Organization.PhoneNumber}";
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[21, 12, 21, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(повне найменування, місцезнаходження / П. І. Б., місце проживання)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[23, 4, 23, 11]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Вантажоодержувач";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[23, 12, 23, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = $"{reSale.ReSale.ClientAgreement.Client.FullName}, " +
                              $"{reSale.ReSale.ClientAgreement.Client.ActualAddress}, тел.:" +
                              $"{reSale.ReSale.ClientAgreement.Client.MobileNumber}";
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[24, 12, 24, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(повне найменування, місцезнаходження / П. І. Б., місце проживання)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[26, 4, 26, 11]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Пункт навантаження";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[26, 12, 26, 31]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = setting.LoadingPoint;
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[26, 32, 26, 41]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Пункт розвантаження";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[26, 42, 26, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = setting.UnloadingPoint;
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[27, 12, 27, 31]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(місцезнаходження)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[27, 42, 27, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(місцезнаходження)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[29, 4, 29, 4]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Value = "кількість місць";
            }

            using (ExcelRange range = worksheet.Cells[29, 10, 29, 20]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = Convert.ToDecimal(reSale.ReSaleItemModels.Count).ToText(true, true);
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[29, 21, 29, 27]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Value = ", масою брутто, т";
                range.Merge = true;
            }

            double totalWeight = Math.Round(
                reSale.ReSaleItemModels.Sum(x => x.ConsignmentItem.Weight * x.QtyToReSale),
                3,
                MidpointRounding.AwayFromZero
            );

            decimal totalPriceWithVat = Math.Round(
                reSale.ReSaleItemModels.Sum(x => x.Amount),
                2,
                MidpointRounding.AwayFromZero
            );

            decimal vatRate =
                reSale.ReSale.Organization.VatRate != null
                    ? Convert.ToDecimal(reSale.ReSale.Organization.VatRate.Value) / 100
                    : 0;

            decimal totalVat = Math.Round(
                totalPriceWithVat * (vatRate / (1 + vatRate)),
                2,
                MidpointRounding.AwayFromZero);

            double qtyT = totalWeight / 1000;
            double qtyKg = (qtyT - (int)qtyT) * 1000;

            using (ExcelRange range = worksheet.Cells[29, 28, 29, 36]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Value = Convert.ToDecimal(qtyT).ToText(false, true) +
                              " т. " + qtyKg + " кг";
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[29, 37, 29, 37]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Value = ", отримав водій/експедитор";
            }

            using (ExcelRange range = worksheet.Cells[29, 48, 29, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = setting.Driver;
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[30, 10, 30, 20]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(словами)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[30, 28, 30, 36]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(словами)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[30, 48, 30, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Merge = true;
                range.Value = "(П. І. Б., посада, підпис)";
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[32, 4, 32, 4]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Value = "Усього відпущено на загальну суму";
            }

            int fullNumber = Convert.ToInt32(Math.Truncate(totalPriceWithVat));
            int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

            string endKeyWord;

            string totalAmountInString = totalPriceWithVat.ToText(true, true);

            switch (reSale.ReSale.ClientAgreement.Agreement.Currency?.Code ?? "") {
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

            string endKeyWordForVat = endKeyWord;

            totalAmountInString += $" {endKeyWord} {decimal.Round(decimal.Round(totalPriceWithVat % 1, 2) * 100, 0)} ";

            int fullNumberDecimals = Convert.ToInt32(Math.Round(totalPriceWithVat % 1, 2) * 100);
            int endNumberDecimals = Convert.ToInt32(fullNumberDecimals.ToString().Last().ToString());

            switch (reSale.ReSale.ClientAgreement.Agreement.Currency?.Code ?? "") {
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

            totalAmountInString += endKeyWord;

            using (ExcelRange range = worksheet.Cells[32, 16, 32, 41]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Value = $"{totalPriceWithVat.ToCompleteText(reSale.ReSale.ClientAgreement.Agreement.Currency.Code, true, true, true)}";
                range.Style.WrapText = true;
                range.Style.Font.Bold = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[32, 42, 32, 49]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Value = ", у тому числі ПДВ";
                range.Merge = true;
            }

            using (ExcelRange range = worksheet.Cells[32, 50, 32, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = totalVat + " " + endKeyWordForVat;
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[35, 4, 35, 4]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Value = "Супровідні документи на вантаж";
            }

            using (ExcelRange range = worksheet.Cells[35, 16, 35, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;

                string fromDate = reSale.ReSale.ChangedToInvoice.HasValue
                    ? reSale.ReSale.ChangedToInvoice.Value.ToString("dd.MM.yy")
                    : reSale.ReSale.Created.ToString("dd.MM.yy");

                range.Value =
                    $"Видаткова накладна {reSale.ReSale.SaleNumber.Value} від {fromDate}";
                range.Style.WrapText = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[36, 4, 36, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[38, 2, 38, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "ВІДОМОСТІ ПРО ВАНТАЖ";
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[40, 4, 40, 5]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "№ з/п";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 6, 40, 19]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Найменування вантажу (номер контейнера), у разі перевезення небезпечних вантажів: " +
                              "клас небезпечних речовин, до якого віднесено вантаж";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 20, 40, 26]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Одиниця виміру";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 27, 40, 31]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Кількість місць";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 32, 40, 39]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Ціна без ПДВ за одиницю, грн";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 40, 40, 49]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Загальна сума з ПДВ, грн";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 50, 40, 55]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Вид пакування";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 56, 40, 61]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Документи з вантажем";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[40, 62, 40, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Маса брутто, т";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 4, 41, 5]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "1";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 6, 41, 19]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "2";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 20, 41, 26]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "3";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 27, 41, 31]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "4";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 32, 41, 39]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "5";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 40, 41, 49]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "6";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 50, 41, 55]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "7";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 56, 41, 61]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "8";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[41, 62, 41, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "9";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            int row = 42;

            int index = 1;

            foreach (UpdatedReSaleItemModel item in reSale.ReSaleItemModels) {
                using (ExcelRange range = worksheet.Cells[row, 4, row, 5]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = index;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 19]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = item.ConsignmentItem.Product.Name;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 20, row, 26]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = item.ConsignmentItem.Product.MeasureUnit.Name;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 27, row, 31]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = item.QtyToReSale;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 32, row, 39]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = decimal.Round(
                        item.SalePrice - item.SalePrice * (vatRate / (vatRate + 1)),
                        2,
                        MidpointRounding.AwayFromZero
                    );
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 40, row, 49]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = decimal.Round(
                        item.Amount,
                        2,
                        MidpointRounding.AwayFromZero
                    );
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 50, row, 55]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = "упак";
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 56, row, 61]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = "";
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 62, row, 66]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Merge = true;
                    range.Value = Math.Round(item.QtyToReSale * item.ConsignmentItem.Weight / 1000, 3, MidpointRounding.AwayFromZero);
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                worksheet.SetRowHeight(12.1212, row);
                row++;
                index++;
            }

            using (ExcelRange range = worksheet.Cells[row, 4, row, 19]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "Усього:";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 20, row, 26]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = "";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 27, row, 31]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = reSale.ReSaleItemModels.Sum(x => x.QtyToReSale);
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 39]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 40, row, 49]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = decimal.Round(totalPriceWithVat, 2, MidpointRounding.AwayFromZero);
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 50, row, 55]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 56, row, 61]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 62, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Value = Math.Round(qtyT, 3, MidpointRounding.AwayFromZero);
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 4, row, 64]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            }

            row++;
            using (ExcelRange range = worksheet.Cells[row, 5, row, 25]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Здав (відповідальна особа вантажовідправника)";
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 57]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Прийняв (відповідальна особа вантажоодержувача)";
            }

            row++;

            worksheet.SetRowHeight(15, row);

            using (ExcelRange range = worksheet.Cells[row, 5, row, 25]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Директор " + reSale.ReSale.Organization.Manager;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 57]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Керівник " + reSale.ReSale.ClientAgreement.Client.Manager;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 5, row, 25]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "(П. І. Б., посада, підпис)";
            }

            using (ExcelRange range = worksheet.Cells[row, 34, row, 57]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 7;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "(П. І. Б., посада, підпис)";
            }

            row++;
            row++;

            using (ExcelRange range = worksheet.Cells[row, 4, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Value = "ВАНТАЖНО-РОЗВАНТАЖУВАЛЬНІ ОПЕРАЦІЇ";
            }

            row++;
            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row + 2, 12]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Операція";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row + 2, 23]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Маса брутто, т";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 24, row, 54]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Час (год. хв.)";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 55, row + 2, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Підпис відповідальної особи";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 24, row + 1, 32]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "прибуття";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row + 1, 44]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "вибуття";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 45, row + 1, 54]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "простою";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;
            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 12]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "10";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row, 23]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "11";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 24, row, 32]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "12";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 44]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "13";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 45, row, 54]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "14";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 55, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "15";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 12]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Навантаження";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row, 23]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 24, row, 32]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 44]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 45, row, 54]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 55, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 12]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Розвантаження";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row, 23]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 24, row, 32]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 44]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 45, row, 54]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 55, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 4, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 4, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Value = "ГАБАРИТНО-ВАГОВІ ПАРАМЕТРИ ТРАНСПОРТНОГО ЗАСОБУ";
            }

            row++;
            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row + 1, 18]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Транспортний засіб";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 19, row, 46]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Габарити, мм";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 47, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Вага, т";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 19, row, 27]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Довжина";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 36]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Ширина";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 46]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "Висота";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 47, row, 59]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "без вантажу";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 60, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "загальна";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 18]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "16";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 19, row, 27]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "17";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 36]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "18";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 46]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "19";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 47, row, 59]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "20";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 60, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = "21";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 18]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.CarLabel;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 19, row, 27]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.CarLength.Equals(0) ? "" : setting.CarLength.ToString();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 36]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.CarWidth.Equals(0) ? "" : setting.CarWidth.ToString();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 46]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.CarHeight.Equals(0) ? "" : setting.CarHeight.ToString();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 47, row, 59]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.CarNetWeight.Equals(0) ? "" : setting.CarNetWeight.ToString(CultureInfo.CreateSpecificCulture("de-DE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 60, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.CarGrossWeight.Equals(0) ? "" : setting.CarGrossWeight.ToString(CultureInfo.CreateSpecificCulture("de-DE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 18]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.TrailerLabel;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 19, row, 27]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.TrailerLength.Equals(0) ? "" : setting.TrailerLength.ToString();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 36]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.TrailerWidth.Equals(0) ? "" : setting.TrailerWidth.ToString();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 46]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.TrailerHeight.Equals(0) ? "" : setting.TrailerHeight.ToString();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 47, row, 59]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.TrailerNetWeight.Equals(0) ? "" : setting.TrailerNetWeight.ToString(CultureInfo.CreateSpecificCulture("de-DE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 60, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Value = setting.TrailerGrossWeight.Equals(0) ? "" : setting.TrailerGrossWeight.ToString(CultureInfo.CreateSpecificCulture("de-DE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, 4, row, 66]) {
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Merge = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(12.1212, new[] {
                row, row + 1, row + 2, row + 3, row + 4, row + 6, row + 7, row + 8, row + 9,
                row + 10, row + 11, row + 12, row + 13
            });

            using (ExcelRange range = worksheet.Cells[1, 1, row, 69]) {
                range.Style.Font.Name = "Arial";
                range.Style.Border.BorderAround(ExcelBorderStyle.None);
            }

            package.Workbook.Properties.Title = "TTN Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }
}