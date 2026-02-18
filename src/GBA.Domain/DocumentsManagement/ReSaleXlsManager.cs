using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GBA.Common.Extensions;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.EntityHelpers.ReSaleModels;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public sealed class ReSaleXlsManager : BaseXlsManager, IReSaleXlsManager {
    private readonly Regex _literals = new(@"[A-zА-я]+", RegexOptions.Compiled);

    public (string xlsxFile, string pdfFile) ExportReSaleInvoicePaymentDocumentToXlsx(string path, UpdatedReSaleModel reSale) {
        string fileName = Path.Combine(path, $"ReSale_PaymentDocument_{reSale.ReSale.SaleNumber.Value}_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("PaymentDocument");

            //Set printer settings
            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Set column's width
            worksheet.SetColumnWidth(0.55, 1);
            worksheet.SetColumnWidth(0.44, 2);
            worksheet.SetColumnWidth(2.55, 3);
            worksheet.SetColumnWidth(2.99, 4);
            worksheet.SetColumnWidth(2.66, 5);
            worksheet.SetColumnWidth(0.66, 6);
            worksheet.SetColumnWidth(1.99, 7);
            worksheet.SetColumnWidth(2.66, 8);
            worksheet.SetColumnWidth(1.99, 9);
            worksheet.SetColumnWidth(1.99, 10);
            worksheet.SetColumnWidth(1.99, 11);
            worksheet.SetColumnWidth(1.99, 12);
            worksheet.SetColumnWidth(1.99, 13);
            worksheet.SetColumnWidth(1.99, 14);
            worksheet.SetColumnWidth(1.22, 15);
            worksheet.SetColumnWidth(1.66, 16);
            worksheet.SetColumnWidth(2.66, 17);
            worksheet.SetColumnWidth(0.55, 18);
            worksheet.SetColumnWidth(2.11, 19);
            worksheet.SetColumnWidth(2.66, 20);
            worksheet.SetColumnWidth(2.66, 21);
            worksheet.SetColumnWidth(1.44, 22);
            worksheet.SetColumnWidth(1.22, 23);
            worksheet.SetColumnWidth(1.11, 24);
            worksheet.SetColumnWidth(1.55, 25);
            worksheet.SetColumnWidth(2.66, 26);
            worksheet.SetColumnWidth(1.88, 27);
            worksheet.SetColumnWidth(0.88, 28);
            worksheet.SetColumnWidth(2.66, 29);
            worksheet.SetColumnWidth(0.99, 30);
            worksheet.SetColumnWidth(1.66, 31);
            worksheet.SetColumnWidth(0.22, 32);
            worksheet.SetColumnWidth(2.66, 33);
            worksheet.SetColumnWidth(2.99, 34);
            worksheet.SetColumnWidth(1.22, 35);
            worksheet.SetColumnWidth(1.55, 36);
            worksheet.SetColumnWidth(2.99, 37);
            worksheet.SetColumnWidth(2.99, 38);
            worksheet.SetColumnWidth(2.99, 39);
            worksheet.SetColumnWidth(2.99, 40);
            worksheet.SetColumnWidth(2.55, 41);
            worksheet.SetColumnWidth(0.11, 42);
            worksheet.SetColumnWidth(2.99, 43);
            worksheet.SetColumnWidth(2.11, 44);
            worksheet.SetColumnWidth(0.66, 45);
            worksheet.SetColumnWidth(2.99, 46);
            worksheet.SetColumnWidth(2.99, 47);
            worksheet.SetColumnWidth(2.88, 48);
            worksheet.SetColumnWidth(2.55, 49);
            worksheet.SetColumnWidth(2.22, 50);
            worksheet.SetColumnWidth(0.55, 51);
            worksheet.SetColumnWidth(0.22, 52);

            worksheet.SetRowHeight(4.38, new[] { 11, 20, 25, 24 }); //5
            worksheet.SetRowHeight(6.38, new[] { 4, 6, 13 }); //8
            worksheet.SetRowHeight(11.28, new[] { 1, 2, 3, 8, 15, 17 /*28, 34, 37*/ }); //15
            worksheet.SetRowHeight(11.98, new[] { 10, 22, 23, 26, 27, 25 }); //16
            worksheet.SetRowHeight(12.78, new[] { 9, 14, 18, 21 /*31, 32, 38*/ }); //17
            worksheet.SetRowHeight(16.28, new[] { 5 }); //21
            worksheet.SetRowHeight(21.28, new[] { 16 }); //28
            worksheet.SetRowHeight(28, new[] { 12 /*, 29*/ }); //29
            worksheet.SetRowHeight(24.38, new[] { 7 }); //32
            worksheet.SetRowHeight(34.48, new[] { 19 }); //46

            using (ExcelRange range = worksheet.Cells[1, 5, 3, 47]) {
                range.Merge = true;
                range.Value =
                    "Увага! Оплата цього рахунку означає погодження з умовами поставки товарів. Повідомлення про оплату є обов'язковим, в іншому випадку не гарантується наявність товарів на складі. Товар відпускається за фактом надходження коштів на п/р Постачальника, самовивозом, за наявності довіреності та паспорта.";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 3, 5, 50]) {
                range.Merge = true;
                range.Value = "Зразок заповнення платіжного доручення";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[7, 3, 7, 6]) {
                range.Merge = true;
                range.Value = "Одержувач";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[7, 7, 7, 30]) {
                range.Merge = true;
                range.Value = reSale.ReSale.Organization.FullName;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[8, 3, 9, 6]) {
                range.Merge = true;
                range.Value = "Код";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[8, 7, 9, 15]) {
                range.Merge = true;
                range.Value = reSale.ReSale.Organization.TIN;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            using (ExcelRange range = worksheet.Cells[10, 3, 10, 10]) {
                range.Merge = true;
                range.Value = "Банк одержувача";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[10, 25, 10, 30]) {
                range.Merge = true;
                range.Value = "Код банку";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[12, 3, 12, 24]) {
                range.Merge = true;
                range.Value =
                    $"{reSale.ReSale.Organization.MainPaymentRegister?.BankName ?? string.Empty} {reSale.ReSale.Organization.MainPaymentRegister?.City ?? string.Empty}";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[12, 25, 12, 30]) {
                range.Merge = true;
                range.Value = reSale.ReSale.Organization.MainPaymentRegister?.SortCode ?? string.Empty;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[11, 25, 12, 30]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            using (ExcelRange range = worksheet.Cells[8, 33, 8, 41]) {
                range.Merge = true;
                range.Value = "КРЕДИТ рах. N";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[9, 33, 10, 41]) {
                range.Merge = true;
                range.Value = reSale.ReSale.Organization.MainPaymentRegister?.AccountNumber ?? string.Empty;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            using (ExcelRange range = worksheet.Cells[11, 33, 12, 41]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            using (ExcelRange range = worksheet.Cells[7, 42, 12, 50]) {
                range.Merge = true;
                range.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[5, 3, 13, 50]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            string dateFormated = string.Empty;

            if (reSale.ReSale.ChangedToInvoice.HasValue) {
                dateFormated += $"{reSale.ReSale.ChangedToInvoice?.Day} ";

                switch (reSale.ReSale.ChangedToInvoice?.Month) {
                    case 1:
                        dateFormated += "січня ";
                        break;
                    case 2:
                        dateFormated += "лютого ";
                        break;
                    case 3:
                        dateFormated += "березня ";
                        break;
                    case 4:
                        dateFormated += "квітня ";
                        break;
                    case 5:
                        dateFormated += "травня ";
                        break;
                    case 6:
                        dateFormated += "червня ";
                        break;
                    case 7:
                        dateFormated += "липня ";
                        break;
                    case 8:
                        dateFormated += "серпня ";
                        break;
                    case 9:
                        dateFormated += "вересня ";
                        break;
                    case 10:
                        dateFormated += "жовтня ";
                        break;
                    case 11:
                        dateFormated += "листопада ";
                        break;
                    case 12:
                        dateFormated += "грудня ";
                        break;
                }

                dateFormated += $"{reSale.ReSale.ChangedToInvoice?.Year} ";
            } else {
                dateFormated += $"{reSale.ReSale.Updated.Day} ";

                switch (reSale.ReSale.Updated.Month) {
                    case 1:
                        dateFormated += "січня ";
                        break;
                    case 2:
                        dateFormated += "лютого ";
                        break;
                    case 3:
                        dateFormated += "березня ";
                        break;
                    case 4:
                        dateFormated += "квітня ";
                        break;
                    case 5:
                        dateFormated += "травня ";
                        break;
                    case 6:
                        dateFormated += "червня ";
                        break;
                    case 7:
                        dateFormated += "липня ";
                        break;
                    case 8:
                        dateFormated += "серпня ";
                        break;
                    case 9:
                        dateFormated += "вересня ";
                        break;
                    case 10:
                        dateFormated += "жовтня ";
                        break;
                    case 11:
                        dateFormated += "листопада ";
                        break;
                    case 12:
                        dateFormated += "грудня ";
                        break;
                }

                dateFormated += $"{reSale.ReSale.Updated.Year} ";
            }

            string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

            using (ExcelRange range = worksheet.Cells[16, 2, 16, 51]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "Рахунок на оплату № {0} від {1} р.",
                        reSale.ReSale.SaleNumber.Value,
                        dateFormated
                    );
                //"Рахунок на оплату № 77 від 24 грудня 2018 р.";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[18, 2, 18, 8]) {
                range.Merge = true;
                range.Value = "Постачальник:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[18, 9, 18, 51]) {
                range.Merge = true;
                range.Value = reSale.ReSale.Organization.FullName;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[19, 10, 19, 51]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "П/р {0}, Банк {1} {2},  МФО {3}\r{4}, тел.: {5}\rкод за ДРФО {6}, ІПН {7}",
                        reSale.ReSale.Organization.MainPaymentRegister?.AccountNumber ?? string.Empty,
                        reSale.ReSale.Organization.MainPaymentRegister?.BankName ?? string.Empty,
                        reSale.ReSale.Organization.MainPaymentRegister?.City ?? string.Empty,
                        reSale.ReSale.Organization.MainPaymentRegister?.SortCode ?? string.Empty,
                        reSale.ReSale.Organization.Address,
                        reSale.ReSale.Organization.PhoneNumber,
                        reSale.ReSale.Organization.USREOU,
                        reSale.ReSale.Organization.TIN
                    );
                //"П/р 26002190000065, Банк ПАТ «УНІВЕРСАЛ БАНК» м. Київ, МФО 322001\rУкраїна, 29000, Хмельницький, Чорновола, дом № 176, кв.7, тел.: (097) 7013465,\r код за ДРФО 3100401117, ІПН 3100401117";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[21, 2, 21, 8]) {
                range.Merge = true;
                range.Value = "Покупець:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[21, 9, 21, 51]) {
                range.Merge = true;
                range.Value = string.IsNullOrEmpty(reSale.ReSale.ClientAgreement?.Client?.FullName)
                    ? reSale.ReSale.ClientAgreement?.Client?.Name
                    : reSale.ReSale.ClientAgreement?.Client?.FullName;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[22, 10, 22, 51]) {
                range.Merge = true;
                range.Value = $"{reSale.ReSale.ClientAgreement?.Client?.LegalAddress}";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[23, 10, 23, 51]) {
                range.Merge = true;
                range.Value = $"Тел.: {reSale.ReSale.ClientAgreement?.Client?.MobileNumber}";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[25, 2, 25, 7]) {
                range.Merge = true;
                range.Value = "Договір:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            using (ExcelRange range = worksheet.Cells[25, 8, 25, 51]) {
                range.Merge = true;
                string fromDateStringFormat = string.Empty;

                if (reSale.ReSale.ClientAgreement != null && reSale.ReSale.ClientAgreement.Agreement.FromDate.HasValue)
                    fromDateStringFormat = TimeZoneInfo.ConvertTimeFromUtc(
                        reSale.ReSale.ClientAgreement.Agreement.FromDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(
                            CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                                ? "FLE Standard Time"
                                : "Central European Standard Time"
                        )).ToString("dd.MM.yyyy");

                range.Value = $"№ {reSale.ReSale.ClientAgreement?.Agreement?.Number} від {fromDateStringFormat}";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            //Table header
            using (ExcelRange range = worksheet.Cells[27, 2, 28, 4]) {
                range.Merge = true;
                range.Value = "№";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEEEEE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[27, 5, 28, 12]) {
                range.Merge = true;
                range.Value = "Артикул";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEEEEE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[27, 13, 28, 36]) {
                range.Merge = true;
                range.Value = "Товари (роботи, послуги)";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEEEEE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[27, 37, 28, 40]) {
                range.Merge = true;
                range.Value = "Кількість";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEEEEE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[27, 41, 28, 46]) {
                range.Merge = true;
                range.Value = "Ціна з ПДВ";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEEEEE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[27, 47, 28, 51]) {
                range.Merge = true;
                range.Value = "Сума з ПДВ";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEEEEE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            //Table body
            int row = 29;

            int index = 1;

            foreach (UpdatedReSaleItemModel item in reSale.ReSaleItemModels) {
                using (ExcelRange range = worksheet.Cells[row, 2, row, 4]) {
                    range.Merge = true;
                    range.Value = index;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 12]) {
                    range.Merge = true;
                    range.Value = item.ConsignmentItem.Product.VendorCode;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 13, row, 36]) {
                    range.Merge = true;
                    range.Value = $"{item.ConsignmentItem.Product.Name} {item.ConsignmentItem.ProductSpecification?.SpecificationCode}";
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 37, row, 38]) {
                    range.Merge = true;
                    range.Value = item.QtyToReSale;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 39, row, 40]) {
                    range.Merge = true;
                    range.Value = item.ConsignmentItem.Product.MeasureUnit.Name;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 41, row, 46]) {
                    range.Merge = true;
                    range.Value = decimal.Round(item.SalePrice, 2, MidpointRounding.AwayFromZero);
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 47, row, 51]) {
                    range.Merge = true;
                    range.Value = decimal.Round(item.Amount, 2, MidpointRounding.AwayFromZero);
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                if (item.ConsignmentItem.Product.Name.Length > 32 || item.ConsignmentItem.Product.VendorCode.Length > 32)
                    worksheet.SetRowHeight(24, row);
                else
                    worksheet.SetRowHeight(12, row);

                row++;
                index++;
            }

            worksheet.SetRowHeight(7.11, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 41, row, 46]) {
                range.Merge = true;
                range.Value = "Разом:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            decimal totalAmount = reSale.ReSaleItemModels.Sum(x => x.Amount);

            using (ExcelRange range = worksheet.Cells[row, 47, row, 51]) {
                range.Merge = true;
                range.Value = decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero);
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Numberformat.Format = "0.00";
            }

            worksheet.SetRowHeight(12.78, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 39, row, 46]) {
                range.Merge = true;
                range.Value = "У тому числі ПДВ:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            decimal vatRate =
                reSale.ReSale.Organization.VatRate != null
                    ? Convert.ToDecimal(reSale.ReSale.Organization.VatRate.Value) / 100
                    : 0;
            decimal totalVat = decimal.Round(totalAmount * (vatRate / (vatRate + 1)), 2, MidpointRounding.AwayFromZero);

            using (ExcelRange range = worksheet.Cells[row, 47, row, 51]) {
                range.Merge = true;
                range.Value = totalVat;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Numberformat.Format = "0.00";
            }

            worksheet.SetRowHeight(12.78, row);
            row++;

            worksheet.SetRowHeight(7.11, row);
            row++;

            string vatInString = "";

            if (reSale.ReSale.ClientAgreementId.HasValue) {
                if (reSale.ReSale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")) {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        range.Value = $"Всього найменувань {reSale.ReSaleItemModels.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} EUR.";
                        range.Style.Font.Size = 8;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                    }

                    worksheet.SetRowHeight(12.75, ++row);

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                        int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                        string endKeyWord;

                        if (fullNumber > 10 && fullNumber < 20)
                            endKeyWord = "центів";
                        else
                            switch (endNumber) {
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

                        vatInString = $"У т.ч. ПДВ: {totalVat.ToText(true, true)} євро {(Math.Round(totalVat % 1, 2) * 100).ToText(false, true)} {endKeyWord}";

                        range.Value =
                            $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} євро {(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100).ToText(false, true)} {endKeyWord}\r{vatInString}";
                        range.Style.Font.Size = 9;
                        range.Style.Font.Bold = true;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                    }
                } else if (reSale.ReSale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("usd")) {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        range.Value = $"Всього найменувань {reSale.ReSaleItemModels.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} USD.";
                        range.Style.Font.Size = 8;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                    }

                    worksheet.SetRowHeight(12.75, ++row);

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                        int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                        string endKeyWord;

                        if (fullNumber > 10 && fullNumber < 20)
                            endKeyWord = "центів";
                        else
                            switch (endNumber) {
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

                        vatInString = $"У т.ч. ПДВ: {totalVat.ToText(true, true)} доларів {(Math.Round(totalVat % 1, 2) * 100).ToText(false, true)} {endKeyWord}";

                        range.Value =
                            $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} доларів {(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100).ToText(false, true)} {endKeyWord}\r{vatInString}";
                        range.Style.Font.Size = 9;
                        range.Style.Font.Bold = true;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                    }
                } else if (reSale.ReSale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("pln")) {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        range.Value = $"Всього найменувань {reSale.ReSaleItemModels.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} PLN.";
                        range.Style.Font.Size = 8;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                    }

                    worksheet.SetRowHeight(12.75, ++row);

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                        int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                        string endKeyWord;

                        if (fullNumber > 10 && fullNumber < 20)
                            endKeyWord = "грошів";
                        else
                            switch (endNumber) {
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

                        vatInString = $"У т.ч. ПДВ: {totalVat.ToText(true, true)} злотих {(Math.Round(totalVat % 1, 2) * 100).ToText(false, true)} {endKeyWord}";

                        range.Value =
                            $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} злотих {(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100).ToText(false, true)} {endKeyWord}\r{vatInString}";
                        range.Style.Font.Size = 9;
                        range.Style.Font.Bold = true;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                    }
                } else {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        range.Value = $"Всього найменувань {reSale.ReSaleItemModels.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} ГРН.";
                        range.Style.Font.Size = 8;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                    }

                    worksheet.SetRowHeight(12.75, ++row);

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                        int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                        string endKeyWord;

                        if (fullNumber > 10 && fullNumber < 20)
                            endKeyWord = "копійок";
                        else
                            switch (endNumber) {
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

                        vatInString = $"У т.ч. ПДВ: {totalVat.ToText(true, true)} гривень {(Math.Round(totalVat % 1, 2) * 100).ToText(false, true)} {endKeyWord}";

                        range.Value =
                            $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} гривень {(Math.Round(totalAmount % 1, 2) * 100).ToText(false, true, false)} {endKeyWord}\r{vatInString}";

                        range.Style.Font.Size = 9;
                        range.Style.Font.Bold = true;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                    }
                }
            } else {
                using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                    range.Value = $"Всього найменувань {reSale.ReSaleItemModels.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} ГРН.";
                    range.Style.Font.Size = 8;
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                }

                worksheet.SetRowHeight(12.75, ++row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                    int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                    int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                    string endKeyWord;

                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "копійок";
                    else
                        switch (endNumber) {
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

                    vatInString = $"У т.ч. ПДВ: {totalVat.ToText(true, true)} гривень {(Math.Round(totalVat % 1, 2) * 100).ToText(false, true)} {endKeyWord}";

                    range.Value =
                        $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} гривень {(Math.Round(totalAmount % 1, 2) * 100).ToText(false, true, false)} {endKeyWord}\r{vatInString}";

                    range.Style.Font.Size = 9;
                    range.Style.Font.Bold = true;
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.WrapText = true;
                }
            }

            worksheet.SetRowHeight(24.98, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            worksheet.SetRowHeight(7.11, row);
            row++;

            worksheet.SetRowHeight(11.28, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 29, row, 49]) {
                range.Merge = true;
                range.Value = "Виписав(ла): директор " + reSale.ReSale.Organization.Manager;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(12.78, row);

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Arial";
            }

            using (ExcelRange range = worksheet.Cells[7, 1, 13, 52]) {
                range.Style.Font.Name = "Times New Roman";
            }

            package.Workbook.Properties.Title = "77 Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            //Saving the file.
            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string excelFilePath, string pdfFilePath) ExportReSalePaymentDocumentToXlsx(string path, ReSale reSale) {
        string fileName = Path.Combine(path, $"ReSale_PaymentDocument_{reSale.SaleNumber.Value}_{Guid.NewGuid().ToString()}.xlsx");

        FileInfo newFile = new(fileName);

        if (newFile.Exists) {
            newFile.Delete();

            newFile = new FileInfo(fileName);
        }

        using (ExcelPackage package = new(newFile)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("PaymentDocument");

            //Set printer settings
            worksheet.PrinterSettings.TopMargin = 0.3;
            worksheet.PrinterSettings.BottomMargin = 0.3;
            worksheet.PrinterSettings.RightMargin = 0.1;
            worksheet.PrinterSettings.LeftMargin = 0.1;
            worksheet.PrinterSettings.HeaderMargin = 0.3;
            worksheet.PrinterSettings.FooterMargin = 0.3;
            worksheet.PrinterSettings.FitToPage = true;

            //Set column's width
            worksheet.SetColumnWidth(0.55, 1);
            worksheet.SetColumnWidth(0.44, 2);
            worksheet.SetColumnWidth(2.55, 3);
            worksheet.SetColumnWidth(2.99, 4);
            worksheet.SetColumnWidth(2.66, 5);
            worksheet.SetColumnWidth(0.66, 6);
            worksheet.SetColumnWidth(1.99, 7);
            worksheet.SetColumnWidth(2.66, 8);
            worksheet.SetColumnWidth(1.99, 9);
            worksheet.SetColumnWidth(1.99, 10);
            worksheet.SetColumnWidth(1.99, 11);
            worksheet.SetColumnWidth(1.99, 12);
            worksheet.SetColumnWidth(1.99, 13);
            worksheet.SetColumnWidth(1.99, 14);
            worksheet.SetColumnWidth(1.22, 15);
            worksheet.SetColumnWidth(1.66, 16);
            worksheet.SetColumnWidth(2.66, 17);
            worksheet.SetColumnWidth(0.55, 18);
            worksheet.SetColumnWidth(2.11, 19);
            worksheet.SetColumnWidth(2.66, 20);
            worksheet.SetColumnWidth(2.66, 21);
            worksheet.SetColumnWidth(1.44, 22);
            worksheet.SetColumnWidth(1.22, 23);
            worksheet.SetColumnWidth(1.11, 24);
            worksheet.SetColumnWidth(1.55, 25);
            worksheet.SetColumnWidth(2.66, 26);
            worksheet.SetColumnWidth(1.88, 27);
            worksheet.SetColumnWidth(0.88, 28);
            worksheet.SetColumnWidth(2.66, 29);
            worksheet.SetColumnWidth(0.99, 30);
            worksheet.SetColumnWidth(1.66, 31);
            worksheet.SetColumnWidth(0.22, 32);
            worksheet.SetColumnWidth(2.66, 33);
            worksheet.SetColumnWidth(2.99, 34);
            worksheet.SetColumnWidth(1.22, 35);
            worksheet.SetColumnWidth(1.55, 36);
            worksheet.SetColumnWidth(2.99, 37);
            worksheet.SetColumnWidth(2.99, 38);
            worksheet.SetColumnWidth(2.99, 39);
            worksheet.SetColumnWidth(2.99, 40);
            worksheet.SetColumnWidth(2.55, 41);
            worksheet.SetColumnWidth(0.11, 42);
            worksheet.SetColumnWidth(2.99, 43);
            worksheet.SetColumnWidth(2.11, 44);
            worksheet.SetColumnWidth(0.66, 45);
            worksheet.SetColumnWidth(2.99, 46);
            worksheet.SetColumnWidth(2.99, 47);
            worksheet.SetColumnWidth(2.88, 48);
            worksheet.SetColumnWidth(2.55, 49);
            worksheet.SetColumnWidth(2.22, 50);
            worksheet.SetColumnWidth(0.55, 51);
            worksheet.SetColumnWidth(0.22, 52);

            worksheet.SetRowHeight(4.38, new[] { 11, 20, 25, 24 }); //5
            worksheet.SetRowHeight(6.38, new[] { 4, 6, 13 }); //8
            worksheet.SetRowHeight(11.28, new[] { 1, 2, 3, 8, 15, 17 /*28, 34, 37*/ }); //15
            worksheet.SetRowHeight(11.98, new[] { 10, 22, 23, 26, 27, 25 }); //16
            worksheet.SetRowHeight(12.78, new[] { 9, 14, 18, 21 /*31, 32, 38*/ }); //17
            worksheet.SetRowHeight(16.28, new[] { 5 }); //21
            worksheet.SetRowHeight(21.28, new[] { 16 }); //28
            worksheet.SetRowHeight(28, new[] { 12 /*, 29*/ }); //29
            worksheet.SetRowHeight(24.38, new[] { 7 }); //32
            worksheet.SetRowHeight(34.48, new[] { 19 }); //46

            using (ExcelRange range = worksheet.Cells[1, 5, 3, 47]) {
                range.Merge = true;
                range.Value =
                    "Увага! Оплата цього рахунку означає погодження з умовами поставки товарів. Повідомлення про оплату є обов'язковим, в іншому випадку не гарантується наявність товарів на складі. Товар відпускається за фактом надходження коштів на п/р Постачальника, самовивозом, за наявності довіреності та паспорта.";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[5, 3, 5, 50]) {
                range.Merge = true;
                range.Value = "Зразок заповнення платіжного доручення";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[7, 3, 7, 6]) {
                range.Merge = true;
                range.Value = "Одержувач";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[7, 7, 7, 30]) {
                range.Merge = true;
                range.Value = reSale.Organization.FullName;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[8, 3, 9, 6]) {
                range.Merge = true;
                range.Value = "Код";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[8, 7, 9, 15]) {
                range.Merge = true;
                range.Value = reSale.Organization.TIN;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            using (ExcelRange range = worksheet.Cells[10, 3, 10, 10]) {
                range.Merge = true;
                range.Value = "Банк одержувача";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[10, 25, 10, 30]) {
                range.Merge = true;
                range.Value = "Код банку";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[12, 3, 12, 24]) {
                range.Merge = true;
                range.Value =
                    $"{reSale.Organization.MainPaymentRegister?.BankName ?? string.Empty} {reSale.Organization.MainPaymentRegister?.City ?? string.Empty}";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[12, 25, 12, 30]) {
                range.Merge = true;
                range.Value = reSale.Organization.MainPaymentRegister?.SortCode ?? string.Empty;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[11, 25, 12, 30]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            using (ExcelRange range = worksheet.Cells[8, 33, 8, 41]) {
                range.Merge = true;
                range.Value = "КРЕДИТ рах. N";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[9, 33, 10, 41]) {
                range.Merge = true;
                range.Value = reSale.Organization.MainPaymentRegister?.AccountNumber ?? string.Empty;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            using (ExcelRange range = worksheet.Cells[11, 33, 12, 41]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            using (ExcelRange range = worksheet.Cells[7, 42, 12, 50]) {
                range.Merge = true;
                range.Style.Border.Left.Style = ExcelBorderStyle.Medium;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[5, 3, 13, 50]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            string dateFormated = string.Empty;

            if (reSale.ChangedToInvoice.HasValue) {
                dateFormated += $"{reSale.ChangedToInvoice?.Day} ";

                switch (reSale.ChangedToInvoice?.Month) {
                    case 1:
                        dateFormated += "січня ";
                        break;
                    case 2:
                        dateFormated += "лютого ";
                        break;
                    case 3:
                        dateFormated += "березня ";
                        break;
                    case 4:
                        dateFormated += "квітня ";
                        break;
                    case 5:
                        dateFormated += "травня ";
                        break;
                    case 6:
                        dateFormated += "червня ";
                        break;
                    case 7:
                        dateFormated += "липня ";
                        break;
                    case 8:
                        dateFormated += "серпня ";
                        break;
                    case 9:
                        dateFormated += "вересня ";
                        break;
                    case 10:
                        dateFormated += "жовтня ";
                        break;
                    case 11:
                        dateFormated += "листопада ";
                        break;
                    case 12:
                        dateFormated += "грудня ";
                        break;
                }

                dateFormated += $"{reSale.ChangedToInvoice?.Year} ";
            } else {
                dateFormated += $"{reSale.Updated.Day} ";

                switch (reSale.Updated.Month) {
                    case 1:
                        dateFormated += "січня ";
                        break;
                    case 2:
                        dateFormated += "лютого ";
                        break;
                    case 3:
                        dateFormated += "березня ";
                        break;
                    case 4:
                        dateFormated += "квітня ";
                        break;
                    case 5:
                        dateFormated += "травня ";
                        break;
                    case 6:
                        dateFormated += "червня ";
                        break;
                    case 7:
                        dateFormated += "липня ";
                        break;
                    case 8:
                        dateFormated += "серпня ";
                        break;
                    case 9:
                        dateFormated += "вересня ";
                        break;
                    case 10:
                        dateFormated += "жовтня ";
                        break;
                    case 11:
                        dateFormated += "листопада ";
                        break;
                    case 12:
                        dateFormated += "грудня ";
                        break;
                }

                dateFormated += $"{reSale.Updated.Year} ";
            }

            string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

            using (ExcelRange range = worksheet.Cells[16, 2, 16, 51]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "Рахунок на оплату № {0} від {1} р.",
                        reSale.SaleNumber.Value,
                        dateFormated
                    );
                //"Рахунок на оплату № 77 від 24 грудня 2018 р.";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            using (ExcelRange range = worksheet.Cells[18, 2, 18, 8]) {
                range.Merge = true;
                range.Value = "Постачальник:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[18, 9, 18, 51]) {
                range.Merge = true;
                range.Value = reSale.Organization.FullName;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[19, 10, 19, 51]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "П/р {0}, Банк {1} {2},  МФО {3}\r{4}, тел.: {5}\rкод за ДРФО {6}, ІПН {7}",
                        reSale.Organization.MainPaymentRegister?.AccountNumber ?? string.Empty,
                        reSale.Organization.MainPaymentRegister?.BankName ?? string.Empty,
                        reSale.Organization.MainPaymentRegister?.City ?? string.Empty,
                        reSale.Organization.MainPaymentRegister?.SortCode ?? string.Empty,
                        reSale.Organization.Address,
                        reSale.Organization.PhoneNumber,
                        reSale.Organization.USREOU,
                        reSale.Organization.TIN
                    );
                //"П/р 26002190000065, Банк ПАТ «УНІВЕРСАЛ БАНК» м. Київ, МФО 322001\rУкраїна, 29000, Хмельницький, Чорновола, дом № 176, кв.7, тел.: (097) 7013465,\r код за ДРФО 3100401117, ІПН 3100401117";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[21, 2, 21, 8]) {
                range.Merge = true;
                range.Value = "Покупець:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.UnderLine = true;
            }

            using (ExcelRange range = worksheet.Cells[21, 9, 21, 51]) {
                range.Merge = true;
                range.Value = string.IsNullOrEmpty(reSale.ClientAgreement?.Client?.FullName)
                    ? reSale.ClientAgreement?.Client?.Name
                    : reSale.ClientAgreement?.Client?.FullName;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[22, 10, 22, 51]) {
                range.Merge = true;
                range.Value = $"{reSale.ClientAgreement?.Client?.LegalAddress}";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[23, 10, 23, 51]) {
                range.Merge = true;
                range.Value = $"Тел.: {reSale.ClientAgreement?.Client?.MobileNumber}";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[25, 2, 25, 7]) {
                range.Merge = true;
                range.Value = "Договір:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            using (ExcelRange range = worksheet.Cells[25, 8, 25, 51]) {
                range.Merge = true;
                string fromDateStringFormat = string.Empty;

                if (reSale.ClientAgreement != null && reSale.ClientAgreement.Agreement.FromDate.HasValue)
                    fromDateStringFormat = TimeZoneInfo.ConvertTimeFromUtc(
                        reSale.ClientAgreement.Agreement.FromDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(
                            CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                                ? "FLE Standard Time"
                                : "Central European Standard Time"
                        )).ToString("dd.MM.yyyy");

                range.Value = $"№ {reSale.ClientAgreement?.Agreement?.Number} від {fromDateStringFormat}";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            //Table header
            using (ExcelRange range = worksheet.Cells[27, 2, 28, 4]) {
                range.Merge = true;
                range.Value = "№";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEEEEE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[27, 5, 28, 12]) {
                range.Merge = true;
                range.Value = "Артикул";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEEEEE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[27, 13, 28, 36]) {
                range.Merge = true;
                range.Value = "Товари (роботи, послуги)";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEEEEE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[27, 37, 28, 40]) {
                range.Merge = true;
                range.Value = "Кількість";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEEEEE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[27, 41, 28, 46]) {
                range.Merge = true;
                range.Value = "Ціна з ПДВ";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEEEEE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[27, 47, 28, 51]) {
                range.Merge = true;
                range.Value = "Сума з ПДВ";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEEEEE"));
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            //Table body
            int row = 29;

            int index = 1;

            foreach (ReSaleItem item in reSale.ReSaleItems) {
                using (ExcelRange range = worksheet.Cells[row, 2, row, 4]) {
                    range.Merge = true;
                    range.Value = index;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 12]) {
                    range.Merge = true;
                    range.Value = item.Product.VendorCode;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 13, row, 36]) {
                    range.Merge = true;
                    string specificationCode = string.Empty;

                    if (item.ReSaleAvailability != null)
                        specificationCode = item.ReSaleAvailability.ConsignmentItem.ProductSpecification?.SpecificationCode ?? string.Empty;

                    range.Value = $"{item.Product.Name} {specificationCode}";
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 37, row, 38]) {
                    range.Merge = true;
                    range.Value = item.Qty;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 39, row, 40]) {
                    range.Merge = true;
                    range.Value = item.Product.MeasureUnit.Name;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 41, row, 46]) {
                    range.Merge = true;
                    range.Value = decimal.Round(item.PricePerItem, 2, MidpointRounding.AwayFromZero);
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 47, row, 51]) {
                    range.Merge = true;
                    range.Value = decimal.Round(item.TotalPrice, 2, MidpointRounding.AwayFromZero);
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                if (item.Product.Name.Length > 32 || item.Product.VendorCode.Length > 32)
                    worksheet.SetRowHeight(24, row);
                else
                    worksheet.SetRowHeight(12, row);

                row++;
                index++;
            }

            worksheet.SetRowHeight(7.11, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 41, row, 46]) {
                range.Merge = true;
                range.Value = "Разом:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            decimal totalAmount = reSale.ReSaleItems.Sum(x => x.TotalPrice);

            using (ExcelRange range = worksheet.Cells[row, 47, row, 51]) {
                range.Merge = true;
                range.Value = decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero);
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Numberformat.Format = "0.00";
            }

            worksheet.SetRowHeight(12.78, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 39, row, 46]) {
                range.Merge = true;
                range.Value = "У тому числі ПДВ:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            decimal vatRate =
                reSale.Organization.VatRate != null
                    ? Convert.ToDecimal(reSale.Organization.VatRate.Value) / 100
                    : 0;
            decimal totalVat = decimal.Round(totalAmount * (vatRate / (vatRate + 1)), 2, MidpointRounding.AwayFromZero);

            using (ExcelRange range = worksheet.Cells[row, 47, row, 51]) {
                range.Merge = true;
                range.Value = totalVat;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Numberformat.Format = "0.00";
            }

            worksheet.SetRowHeight(12.78, row);
            row++;

            worksheet.SetRowHeight(7.11, row);
            row++;

            string vatInString = "";

            if (reSale.ClientAgreementId.HasValue) {
                if (reSale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")) {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        range.Value = $"Всього найменувань {reSale.ReSaleItems.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} EUR.";
                        range.Style.Font.Size = 8;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                    }

                    worksheet.SetRowHeight(12.75, ++row);

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                        int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                        string endKeyWord;

                        if (fullNumber > 10 && fullNumber < 20)
                            endKeyWord = "центів";
                        else
                            switch (endNumber) {
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

                        vatInString = $"У т.ч. ПДВ: {totalVat.ToText(true, true)} євро {(Math.Round(totalVat % 1, 2) * 100).ToText(false, true)} {endKeyWord}";

                        range.Value =
                            $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} євро {(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100).ToText(false, true)} {endKeyWord}\r{vatInString}";
                        range.Style.Font.Size = 9;
                        range.Style.Font.Bold = true;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                    }
                } else if (reSale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("usd")) {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        range.Value = $"Всього найменувань {reSale.ReSaleItems.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} USD.";
                        range.Style.Font.Size = 8;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                    }

                    worksheet.SetRowHeight(12.75, ++row);

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                        int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                        string endKeyWord;

                        if (fullNumber > 10 && fullNumber < 20)
                            endKeyWord = "центів";
                        else
                            switch (endNumber) {
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

                        vatInString = $"У т.ч. ПДВ: {totalVat.ToText(true, true)} доларів {(Math.Round(totalVat % 1, 2) * 100).ToText(false, true)} {endKeyWord}";

                        range.Value =
                            $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} доларів {(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100).ToText(false, true)} {endKeyWord}\r{vatInString}";
                        range.Style.Font.Size = 9;
                        range.Style.Font.Bold = true;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                    }
                } else if (reSale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("pln")) {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        range.Value = $"Всього найменувань {reSale.ReSaleItems.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} PLN.";
                        range.Style.Font.Size = 8;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                    }

                    worksheet.SetRowHeight(12.75, ++row);

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                        int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                        string endKeyWord;

                        if (fullNumber > 10 && fullNumber < 20)
                            endKeyWord = "грошів";
                        else
                            switch (endNumber) {
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

                        vatInString = $"У т.ч. ПДВ: {totalVat.ToText(true, true)} злотих {(Math.Round(totalVat % 1, 2) * 100).ToText(false, true)} {endKeyWord}";

                        range.Value =
                            $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} злотих {(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100).ToText(false, true)} {endKeyWord}\r{vatInString}";
                        range.Style.Font.Size = 9;
                        range.Style.Font.Bold = true;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                    }
                } else {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        range.Value = $"Всього найменувань {reSale.ReSaleItems.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} ГРН.";
                        range.Style.Font.Size = 8;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                    }

                    worksheet.SetRowHeight(12.75, ++row);

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                        int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                        int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                        string endKeyWord;

                        if (fullNumber > 10 && fullNumber < 20)
                            endKeyWord = "копійок";
                        else
                            switch (endNumber) {
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

                        vatInString = $"У т.ч. ПДВ: {totalVat.ToText(true, true)} гривень {(Math.Round(totalVat % 1, 2) * 100).ToText(false, true)} {endKeyWord}";

                        range.Value =
                            $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} гривень {(Math.Round(totalAmount % 1, 2) * 100).ToText(false, true, false)} {endKeyWord}\r{vatInString}";

                        range.Style.Font.Size = 9;
                        range.Style.Font.Bold = true;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.WrapText = true;
                    }
                }
            } else {
                using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                    range.Value = $"Всього найменувань {reSale.ReSaleItems.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} ГРН.";
                    range.Style.Font.Size = 8;
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                }

                worksheet.SetRowHeight(12.75, ++row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                    int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                    int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                    string endKeyWord;

                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "копійок";
                    else
                        switch (endNumber) {
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

                    vatInString = $"У т.ч. ПДВ: {totalVat.ToText(true, true)} гривень {(Math.Round(totalVat % 1, 2) * 100).ToText(false, true)} {endKeyWord}";

                    range.Value =
                        $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} гривень {(Math.Round(totalAmount % 1, 2) * 100).ToText(false, true, false)} {endKeyWord}\r{vatInString}";

                    range.Style.Font.Size = 9;
                    range.Style.Font.Bold = true;
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.WrapText = true;
                }
            }

            worksheet.SetRowHeight(24.98, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 51]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            worksheet.SetRowHeight(7.11, row);
            row++;

            worksheet.SetRowHeight(11.28, row);
            row++;

            using (ExcelRange range = worksheet.Cells[row, 29, row, 49]) {
                range.Merge = true;
                range.Value = "Виписав(ла): директор " + reSale.Organization.Manager;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(12.78, row);

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Arial";
            }

            using (ExcelRange range = worksheet.Cells[7, 1, 13, 52]) {
                range.Style.Font.Name = "Times New Roman";
            }

            package.Workbook.Properties.Title = "77 Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            //Saving the file.
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

    public (string xlsxFile, string pdfFile) ExportReSaleSalesInvoiceDocumentToXlsx(string path, UpdatedReSaleModel reSale, IEnumerable<DocumentMonth> months) {
        string fileName = Path.Combine(path, $"{reSale.ReSale.SaleNumber.Value}_{DateTime.Now.ToString("MM.yyyy")}_{Guid.NewGuid().ToString()}.xlsx");

        DateTime current = DateTime.Now;

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Накладна");

            worksheet.ApplyPrinterSettings(0.5m, 0.5m, 0.1m, 0.1m, 0.25m, 0.25m, true);

            worksheet.Cells.Style.Font.Name = "Arial";
            worksheet.Cells.Style.Font.Size = 8;
            worksheet.DefaultColWidth = 9.83;
            worksheet.DefaultRowHeight = 11.25;

            //Set Rows
            worksheet.SetRowHeight(3.75, new[] { 1, 3 });
            worksheet.SetRowHeight(3.95, new[] { 15 });
            worksheet.SetRowHeight(12.75, new[] { 9, 10, 13, 6, 7, 8, 11, 12, 5 });
            worksheet.SetRowHeight(31, new[] { 2, 4, 14 });
            worksheet.SetRowHeight(22, 16);

            //Set Columns
            worksheet.SetColumnWidth(0.83, new[] { 1, 34, 37 });
            worksheet.SetColumnWidth(2.33, new[] { 3, 9, 11, 13, 15 });
            worksheet.SetColumnWidth(3, new[] { 8, 10 });
            worksheet.SetColumnWidth(2.5, new[] { 12, 67 });
            worksheet.SetColumnWidth(0.66, new[] { 16, 28, 31 });
            worksheet.SetColumnWidth(0.33, new[] { 18, 23, 58, 60, 62, 64, 66, 68, 70, 72, 74, 76, 78 });
            worksheet.SetColumnWidth(0.5, new[] { 20, 22, 43 });
            worksheet.SetColumnWidth(0.16, new[] { 24, 27, 30, 33, 36, 39, 42, 45, 48 });
            worksheet.SetColumnWidth(1, new[] { 25, 57, 35, 41, 44, 47, 50, 51 });
            worksheet.SetColumnWidth(1.5, new[] { 26, 29, 32, 38, 41, 52, 54, 576, 61 });
            worksheet.SetColumnWidth(1.33, new[] { 40, 55 });
            worksheet.SetColumnWidth(1.83, new[] { 46, 71, 77 });
            worksheet.SetColumnWidth(2.16, new[] { 49, 59 });
            worksheet.SetColumnWidth(2, new[] { 14, 17, 21, 53, 56, 63, 65 });
            worksheet.SetColumnWidth(3, new[] { 69, 73, 75 });
            worksheet.SetColumnWidth(2.42, 19);
            worksheet.SetColumnWidth(2.75, 4);
            worksheet.SetColumnWidth(3, 5);
            worksheet.SetColumnWidth(3, 6);
            worksheet.SetColumnWidth(3.5, 7);
            worksheet.SetColumnWidth(2.9, 2);

            int row = 2;
            int column = 2;

            string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

            //Document header
            using (ExcelRange range = worksheet.Cells[row, column, row, column + 75]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "Видаткова накладна № {0} від {1} р.",
                        reSale
                            .ReSale
                            .SaleNumber
                            .Value,
                        $"{current.Day} {months.FirstOrDefault(m => m.Number.Equals(current.Month))?.Name.ToLower() ?? string.Empty} {current.Year}"
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 14;
            }

            row += 2;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 4]) {
                range.Merge = true;
                range.Value = "Постачальник:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.UnderLine = true;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 6, row, column + 75]) {
                range.Merge = true;
                range.Value = reSale.ReSale.Organization.FullName;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 14;
                range.Style.WrapText = true;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, column + 6, row, column + 75]) {
                range.Merge = true;
                string val = string.Empty;
                if (reSale.ReSale.Organization.MainPaymentRegister != null) {
                    if (!string.IsNullOrEmpty(reSale.ReSale.Organization.MainPaymentRegister.AccountNumber))
                        val += $"П/р {reSale.ReSale.Organization.MainPaymentRegister.AccountNumber}, ";

                    if (!string.IsNullOrEmpty(reSale.ReSale.Organization.MainPaymentRegister.BankName))
                        val += $"у банку {reSale.ReSale.Organization.MainPaymentRegister.BankName}, ";

                    range.Value = val;
                } else {
                    range.Value = val;
                }

                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;

                if (string.IsNullOrEmpty(val))
                    worksheet.SetRowHeight(1, row);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, column + 6, row, column + 75]) {
                range.Merge = true;
                string val = string.Empty;

                if (reSale.ReSale.Organization.MainPaymentRegister != null) {
                    if (!string.IsNullOrEmpty(reSale.ReSale.Organization.MainPaymentRegister.City))
                        val += $"м. {reSale.ReSale.Organization.MainPaymentRegister.City}, ";

                    if (!string.IsNullOrEmpty(reSale.ReSale.Organization.MainPaymentRegister.SortCode))
                        val += $"МФО {reSale.ReSale.Organization.MainPaymentRegister.SortCode}, ";

                    range.Value = val;
                } else {
                    range.Value = val;
                }

                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;

                if (string.IsNullOrEmpty(val))
                    worksheet.SetRowHeight(1, row);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, column + 6, row, column + 75]) {
                range.Merge = true;
                string val = string.Empty;

                if (reSale.ReSale.Organization.MainPaymentRegister != null) {
                    if (!string.IsNullOrEmpty(reSale.ReSale.Organization.Address))
                        val += $"{reSale.ReSale.Organization.Address}, ";
                    range.Value = val;
                } else {
                    range.Value = val;
                }

                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;

                if (string.IsNullOrEmpty(val))
                    worksheet.SetRowHeight(1, row);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, column + 6, row, column + 75]) {
                range.Merge = true;
                string val = string.Empty;

                if (reSale.ReSale.Organization.MainPaymentRegister != null) {
                    if (!string.IsNullOrEmpty(reSale.ReSale.Organization.PhoneNumber))
                        val += $"тел.: {reSale.ReSale.Organization.PhoneNumber}, ";

                    if (!string.IsNullOrEmpty(reSale.ReSale.Organization.USREOU))
                        val += $"код за ЄДРПОУ {reSale.ReSale.Organization.USREOU}, ";

                    if (!string.IsNullOrEmpty(reSale.ReSale.Organization.TIN))
                        val += $"ІПН {reSale.ReSale.Organization.TIN}";

                    range.Value = val;
                } else {
                    range.Value = val;
                }

                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;

                if (string.IsNullOrEmpty(val))
                    worksheet.SetRowHeight(1, row);
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, column + 7, row, column + 75]) {
                range.Merge = true;
                string val = string.Empty;

                switch (reSale.ReSale.Organization.TypeTaxation) {
                    case TypeTaxation.SingleTax:
                        val = "Є платником єдиного податку";
                        break;
                    case TypeTaxation.SingleTaxAndVat:
                        val = "Є платником єдиного податку та ПДВ";
                        break;
                    case TypeTaxation.IncomeTax:
                        val = "Є платником податку на прибуток";
                        break;
                    case TypeTaxation.IncomeTaxAndVat:
                        val = "Є платником податку на прибуток на загальних підставах";
                        break;
                    case TypeTaxation.NotPaying:
                        val = "Не є платником податку на прибуток на загальних підставах";
                        break;
                }

                range.Value = val;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            row += 2;
            using (ExcelRange range = worksheet.Cells[row, column, row, column + 4]) {
                range.Merge = true;
                range.Value = "Покупець:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.UnderLine = true;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 6, row, column + 75]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "{0} - {1}",
                        !string.IsNullOrEmpty(reSale.ReSale.ClientAgreement.Client.FullName)
                            ? reSale.ReSale.ClientAgreement.Client.FullName
                            : reSale.ReSale.ClientAgreement.Client.Name,
                        reSale.ReSale.ClientAgreement.Client.RegionCode?.Value ?? ""
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 11;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, column + 7, row, column + 75]) {
                string clientInfo = string.Empty;

                if (!string.IsNullOrEmpty(reSale.ReSale.ClientAgreement.Client.RegionCode?.City)) clientInfo += $"{reSale.ReSale.ClientAgreement.Client.RegionCode.City}, ";

                if (!string.IsNullOrEmpty(reSale.ReSale.ClientAgreement.Client.RegionCode?.District)) clientInfo += $"{reSale.ReSale.ClientAgreement.Client.RegionCode.District}, ";

                if (!string.IsNullOrEmpty(reSale.ReSale.ClientAgreement.Client.ActualAddress)) clientInfo += $"{reSale.ReSale.ClientAgreement.Client.ActualAddress}, ";

                if (!string.IsNullOrEmpty(reSale.ReSale.ClientAgreement.Client.MobileNumber)) clientInfo += $"тел.: {reSale.ReSale.ClientAgreement.Client.MobileNumber}";

                range.Merge = true;
                range.Value = clientInfo;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;

                if (string.IsNullOrEmpty(clientInfo))
                    worksheet.SetRowHeight(1, row);

                if (clientInfo.Length > 80)
                    worksheet.SetRowHeight(24, row);
            }

            row += 2;

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 4]) {
                range.Merge = true;
                range.Value = "Договір:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.UnderLine = true;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 6, row, column + 75]) {
                range.Merge = true;
                string fromDateStringFormat = string.Empty;

                if (reSale.ReSale.ClientAgreement.Agreement.FromDate.HasValue)
                    fromDateStringFormat = TimeZoneInfo.ConvertTimeFromUtc(
                        reSale.ReSale.ClientAgreement.Agreement.FromDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(
                            CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                                ? "FLE Standard Time"
                                : "Central European Standard Time"
                        )).ToString("dd.MM.yyyy");

                range.Value = $"№ {reSale.ReSale.ClientAgreement.Agreement.Number} від {fromDateStringFormat}";

                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
            }

            row += 2;
            //Table header
            int tableStartRow = row;

            using (ExcelRange range = worksheet.Cells[row, column, row + 1, column + 1]) {
                range.Value = "№";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 2, row + 1, column + 7]) {
                range.Value = "Артикул";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 8, row + 1, column + 35]) {
                range.Value = "Товар";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 36, row + 1, column + 48]) {
                range.Value = "";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 49, row + 1, column + 60]) {
                range.Value = "Кількість";
                range.Merge = true;
                range.SetTableHeaderStyle();
            }

            using (ExcelRange range = worksheet.Cells[row, column + 61, row + 1, column + 68]) {
                range.Value = "Ціна з ПДВ";
                range.Merge = true;
                range.SetTableHeaderStyle();
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 69, row + 1, column + 76]) {
                range.Value = "Сума з ПДВ";
                range.Merge = true;
                range.SetTableHeaderStyle();
                range.Style.WrapText = true;
            }

            row += 2;

            //Table body

            int tableRowNumber = 1;

            foreach (UpdatedReSaleItemModel item in reSale.ReSaleItemModels) {
                bool specialItem =
                    item.ConsignmentItem.Product.IsForSale ||
                    item.ConsignmentItem.Product.IsForZeroSale ||
                    item.ConsignmentItem.Product.Top.ToUpper().Equals("X9")
                    || item.ConsignmentItem.Product.Top.ToUpper().Equals("Х9");

                if (specialItem) {
                    //Special OrderItem
                    using (ExcelRange range = worksheet.Cells[row, column]) {
                        range.Value = tableRowNumber;
                        range.Style.Font.Size = 9;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(206, 255, 255));
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    //Special mark * 
                    using (ExcelRange range = worksheet.Cells[row, column + 1]) {
                        range.Value = "*";
                        range.Style.Font.Size = 22;
                        range.Style.Font.Bold = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(206, 255, 255));
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 2, row, column + 7]) {
                        range.Value = item.ConsignmentItem.Product.VendorCode;
                        range.Style.Font.Size = item.ConsignmentItem.Product.VendorCode.Length > 10 ? 9 : 14;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(206, 255, 255));
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 8, row, column + 35]) {
                        range.Value = item.ConsignmentItem.Product.Name;
                        range.Merge = true;
                        range.Style.WrapText = true;
                        range.Style.Font.Size = item.ConsignmentItem.Product.Name.Length > 10 ? 9 : 11;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(206, 255, 255));
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 36, row, column + 48]) {
                        range.Value = item.ConsignmentItem.ProductSpecification?.SpecificationCode;
                        range.Style.Font.Size = 10;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(206, 255, 255));
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 49, row, column + 54]) {
                        range.Value = item.QtyToReSale;
                        range.Style.Font.Size = 16;
                        range.Style.Font.Bold = true;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(206, 255, 255));
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 55, row, column + 60]) {
                        range.Value = item.ConsignmentItem.Product.MeasureUnit.Name;
                        range.Style.Font.Size = 11;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(206, 255, 255));
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 61, row, column + 68]) {
                        //ToDo: local price
                        range.Value = item.SalePrice;
                        range.Style.Font.Size = 11;
                        range.Style.Numberformat.Format = "0.00";
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(206, 255, 255));
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 69, row, column + 76]) {
                        //ToDo: local price
                        range.Value =
                            decimal.Round(
                                item.SalePrice * Convert.ToDecimal(item.QtyToReSale),
                                2,
                                MidpointRounding.AwayFromZero
                            );
                        range.Style.Font.Size = 11;
                        range.Style.Numberformat.Format = "0.00";
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(206, 255, 255));
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    if (item.ConsignmentItem.Product.Name.Length > 18) {
                        if (item.ConsignmentItem.Product.Name.Length < 35)
                            worksheet.SetRowHeight(28, row);
                        else
                            worksheet.SetRowHeight(35, row);
                    }

                    if (item.ConsignmentItem.Product.VendorCode.Length > 15)
                        worksheet.SetRowHeight(28, row);
                    else
                        worksheet.SetRowHeight(35, row);

                    row++;
                } else {
                    //Simple  OrderItem
                    using (ExcelRange range = worksheet.Cells[row, column, row, column + 1]) {
                        range.Value = tableRowNumber;
                        range.Style.Font.Size = 9;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 2, row, column + 7]) {
                        range.Value = item.ConsignmentItem.Product.VendorCode;
                        range.Style.Font.Size = item.ConsignmentItem.Product.VendorCode.Length > 10 ? 9 : 14;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 8, row, column + 35]) {
                        range.Value = item.ConsignmentItem.Product.Name;
                        range.Merge = true;
                        range.Style.Font.Size = item.ConsignmentItem.Product.Name.Length > 10 ? 9 : 11;
                        range.Style.WrapText = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 36, row, column + 48]) {
                        range.Value = item.ConsignmentItem.ProductSpecification?.SpecificationCode;
                        range.Style.Font.Size = 10;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 49, row, column + 54]) {
                        range.Value = item.QtyToReSale;
                        range.Style.Font.Size = 16;
                        range.Style.Font.Bold = true;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 55, row, column + 60]) {
                        range.Value = item.ConsignmentItem.Product.MeasureUnit.Name;
                        range.Style.Font.Size = 11;
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 61, row, column + 68]) {
                        //ToDo: local price
                        range.Value = decimal.Round(
                            item.SalePrice,
                            2,
                            MidpointRounding.AwayFromZero
                        );
                        range.Style.Font.Size = 11;
                        range.Style.Numberformat.Format = "0.00";
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, column + 69, row, column + 76]) {
                        //ToDo: local price
                        range.Value = item.SalePrice * Convert.ToDecimal(item.QtyToReSale);
                        range.Style.Font.Size = 11;
                        range.Style.Numberformat.Format = "0.00";
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    if (item.ConsignmentItem.Product.Name.Length > 18) {
                        if (item.ConsignmentItem.Product.Name.Length < 35)
                            worksheet.SetRowHeight(28, row);
                        else
                            worksheet.SetRowHeight(35, row);
                    }

                    if (item.ConsignmentItem.Product.VendorCode.Length > 15)
                        worksheet.SetRowHeight(28, row);
                    else
                        worksheet.SetRowHeight(35, row);

                    row++;
                }

                tableRowNumber++;
            }

            int tableEndRow = row - 1;

            using (ExcelRange range = worksheet.Cells[tableStartRow, column, tableEndRow, column + 76]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
            }

            worksheet.SetRowHeight(6.95, row);
            worksheet.SetRowHeight(15, ++row);

            using (ExcelRange range = worksheet.Cells[row, column + 60, row, column + 66]) {
                range.Value = "Разом:";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            decimal totalAmount = reSale.ReSaleItemModels.Sum(o => o.Amount);

            using (ExcelRange range = worksheet.Cells[row, column + 68, row, column + 75]) {
                range.Value = decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero);
                range.Style.Font.Size = 11;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Font.Bold = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            row++;

            using (ExcelRange range = worksheet.Cells[row, column + 52, row, column + 66]) {
                range.Value = "У тому числі ПДВ:";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 68, row, column + 75]) {
                decimal vatRate =
                    reSale.ReSale.Organization.VatRate != null
                        ? Convert.ToDecimal(reSale.ReSale.Organization.VatRate.Value) / 100
                        : 0;

                range.Value = decimal.Round(totalAmount * (vatRate / (1 + vatRate)), 2, MidpointRounding.AwayFromZero);
                range.Style.Font.Size = 11;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Font.Bold = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            worksheet.SetRowHeight(6.95, ++row);
            worksheet.SetRowHeight(11.25, ++row);

            if (reSale.ReSale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")) {
                using (ExcelRange range = worksheet.Cells[row, column, row, column + 75]) {
                    range.Value = $"Всього найменувань {reSale.ReSaleItemModels.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} EUR.";
                    range.Style.Font.Size = 8;
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                }

                worksheet.SetRowHeight(12.75, ++row);

                using (ExcelRange range = worksheet.Cells[row, column, row, column + 75]) {
                    int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                    int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                    string endKeyWord;

                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "центів";
                    else
                        switch (endNumber) {
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

                    range.Value =
                        $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} євро {(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100).ToText(false, true)} {endKeyWord}";
                    range.Style.Font.Size = 9;
                    range.Style.Font.Bold = true;
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                }
            } else if (reSale.ReSale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("usd")) {
                using (ExcelRange range = worksheet.Cells[row, column, row, column + 75]) {
                    range.Value = $"Всього найменувань {reSale.ReSaleItemModels.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} USD.";
                    range.Style.Font.Size = 8;
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                }

                worksheet.SetRowHeight(12.75, ++row);

                using (ExcelRange range = worksheet.Cells[row, column, row, column + 75]) {
                    int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                    int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                    string endKeyWord;

                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "центів";
                    else
                        switch (endNumber) {
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

                    range.Value =
                        $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} доларів {(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100).ToText(false, true)} {endKeyWord}";
                    range.Style.Font.Size = 9;
                    range.Style.Font.Bold = true;
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                }
            } else if (reSale.ReSale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("pln")) {
                using (ExcelRange range = worksheet.Cells[row, column, row, column + 75]) {
                    range.Value = $"Всього найменувань {reSale.ReSaleItemModels.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} PLN.";
                    range.Style.Font.Size = 8;
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                }

                worksheet.SetRowHeight(12.75, ++row);

                using (ExcelRange range = worksheet.Cells[row, column, row, column + 75]) {
                    int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                    int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                    string endKeyWord;

                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "грошів";
                    else
                        switch (endNumber) {
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

                    range.Value =
                        $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} злотих {(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100).ToText(false, true)} {endKeyWord}";
                    range.Style.Font.Size = 9;
                    range.Style.Font.Bold = true;
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                }
            } else {
                using (ExcelRange range = worksheet.Cells[row, column, row, column + 75]) {
                    range.Value = $"Всього найменувань {reSale.ReSaleItemModels.Count}, на суму {decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero)} ГРН.";
                    range.Style.Font.Size = 8;
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                }

                worksheet.SetRowHeight(12.75, ++row);

                using (ExcelRange range = worksheet.Cells[row, column, row, column + 75]) {
                    int fullNumber = Convert.ToInt32(Math.Round(decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100);
                    int endNumber = Convert.ToInt32(fullNumber.ToString().Last().ToString());

                    string endKeyWord;

                    if (fullNumber > 10 && fullNumber < 20)
                        endKeyWord = "копійок";
                    else
                        switch (endNumber) {
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

                    // if (sale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("uah"))
                    range.Value =
                        $"{decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} гривень {(Math.Round(totalAmount % 1, 2) * 100).ToText(false, true, false)} {endKeyWord}";
                    // else
                    //     range.Value =
                    //         $"{decimal.Round(sale.TotalAmount, 2, MidpointRounding.AwayFromZero).ToText(true, true)} гривень {(Math.Round(decimal.Round(sale.TotalAmount, 2, MidpointRounding.AwayFromZero) % 1, 2) * 100).ToText(false, true)} {endKeyWord}";

                    range.Style.Font.Size = 9;
                    range.Style.Font.Bold = true;
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                }
            }

            worksheet.SetRowHeight(6.95, ++row);
            using (ExcelRange range = worksheet.Cells[row, column, row, column + 75]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }

            row++;

            worksheet.SetRowHeight(12.75, ++row);

            using (ExcelRange range = worksheet.Cells[row, column, row, column + 5]) {
                range.Value = "Відвантажив(ла):";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 7, row, column + 29]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Merge = true;
                range.Value = $" директор {reSale.ReSale.Organization.Manager}";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 36, row, column + 51]) {
                range.Value = "Отримав(ла):";
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 52, row, column + 73]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            worksheet.SetRowHeight(14.75, ++row);
            worksheet.SetRowHeight(14.75, row + 1);

            using (ExcelRange range = worksheet.Cells[row, column + 1, row, column + 29]) {
                range.Value = "* Відповідальний за здійснення господарської операції";
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.General;
            }

            using (ExcelRange range = worksheet.Cells[row + 1, column + 2, row + 1, column + 29]) {
                range.Value = "і правильність її оформлення";
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.General;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 36, row, column + 51]) {
                range.Value = "За довіреністю";
                range.Style.Font.Size = 9;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 55, row, column + 57]) {
                range.Merge = true;
                range.Value = "№";
                range.Style.Font.Size = 9;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            using (ExcelRange range = worksheet.Cells[row, column + 65, row, column + 70]) {
                range.Merge = true;
                range.Value = "від";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.Font.Size = 9;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            worksheet.SetRowHeight(6.95, ++row);
            worksheet.SetRowHeight(12.75, ++row);

            //Remark

            // if (reSale.ReSaleItems.Any(x => x.OneTimeDiscount > 0)) {
            //     worksheet.SetRowHeight(24, ++row);
            //
            //     using (ExcelRange range = worksheet.Cells[row, column, row, column + 75]) {
            //         range.Value = "* - замовлений або акційний товар поверненню не підлягає";
            //         range.Style.Font.Size = 18;
            //         range.Style.Font.Bold = true;
            //         range.Merge = true;
            //         range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            //         range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            //     }
            //
            //     row++;
            // }

            worksheet.SetRowHeight(12.75, ++row);

            using (ExcelRange range = worksheet.Cells[row, column + 1, row, column + 75]) {
                range.Value = "Вимоги до повернень";
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            row++;
            using (ExcelRange range = worksheet.Cells[row, column + 1, row, column + 75]) {
                range.Value = "1)Товар має бути запакований";
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            row++;
            using (ExcelRange range = worksheet.Cells[row, column + 1, row, column + 75]) {
                range.Value = "2)Коробка даного товару і сам товар мають бути без пошкоджень";
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            row++;
            using (ExcelRange range = worksheet.Cells[row, column + 1, row, column + 75]) {
                range.Value = "3)Повинні бути документи на повернення: від кого і причина повернення в письмовому вигляді";
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            row++;
            using (ExcelRange range = worksheet.Cells[row, column + 1, row, column + 75]) {
                range.Value = "4)Товар не буде прийматися, якщо вказане не буде виконано, і в цьому випадку товар буде відправлено назад покупцеві.";
                range.Style.Font.Size = 8;
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
            }

            package.Save();
        }

        return SaveFiles(fileName);
    }
}