using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GBA.Common.Extensions;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public sealed class TaxFreeAndSadXlsManager : BaseXlsManager, ITaxFreeAndSadXlsManager {
    public (string xlsxFile, string pdfFile) ExportTaxFreeToXlsx(string path, TaxFree taxFree, bool isFromSale = false) {
        string fileName = Path.Combine(path, $"{taxFree.Number}_{DateTime.Now.ToString("MM.yyyy")}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("TaxFree Document");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Setting default width to columns
            worksheet.SetColumnWidth(1.22, 1);
            worksheet.SetColumnWidth(4.81, 2);
            worksheet.SetColumnWidth(4.75, 3);
            worksheet.SetColumnWidth(5.11, 4);
            worksheet.SetColumnWidth(4.38, 5);
            worksheet.SetColumnWidth(4.10, 6);
            worksheet.SetColumnWidth(4.25, 7);
            worksheet.SetColumnWidth(2.90, 8);
            worksheet.SetColumnWidth(4.00, 9);
            worksheet.SetColumnWidth(1.22, 10);
            worksheet.SetColumnWidth(1.98, 11);
            worksheet.SetColumnWidth(2.11, 12);
            worksheet.SetColumnWidth(2.11, 13);
            worksheet.SetColumnWidth(4.38, 14);
            worksheet.SetColumnWidth(1.22, 15);
            worksheet.SetColumnWidth(1.22, 16);
            worksheet.SetColumnWidth(3.60, 17);
            worksheet.SetColumnWidth(3.78, 18);
            worksheet.SetColumnWidth(1.69, 19);
            worksheet.SetColumnWidth(1.69, 20);
            worksheet.SetColumnWidth(3.48, 21);
            worksheet.SetColumnWidth(5.57, 22);
            worksheet.SetColumnWidth(4.81, 23);
            worksheet.SetColumnWidth(4.00, 24);
            worksheet.SetColumnWidth(3.13, 25);
            worksheet.SetColumnWidth(4.75, 26);
            worksheet.SetColumnWidth(4.93, 27);
            worksheet.SetColumnWidth(3.03, 28);
            worksheet.SetColumnWidth(3.60, 29);
            worksheet.SetColumnWidth(3.03, 30);
            worksheet.SetColumnWidth(3.88, 31);
            worksheet.SetColumnWidth(1.56, 32);

            //Document header

            //Setting document header height
            worksheet.SetRowHeight(3.98, 1);
            worksheet.SetRowHeight(27.00, 2);
            worksheet.SetRowHeight(11.71, 3);
            worksheet.SetRowHeight(11.71, 4);
            worksheet.SetRowHeight(11.71, 5);
            worksheet.SetRowHeight(11.71, 6);
            worksheet.SetRowHeight(11.71, 7);
            worksheet.SetRowHeight(12.11, 8);
            worksheet.SetRowHeight(22.01, 9);
            worksheet.SetRowHeight(23.51, 10);
            worksheet.SetRowHeight(12.11, 11);
            worksheet.SetRowHeight(12.11, 12);
            worksheet.SetRowHeight(12.11, 13);
            worksheet.SetRowHeight(12.11, 14);
            worksheet.SetRowHeight(12.11, 15);
            worksheet.SetRowHeight(12.11, 16);
            worksheet.SetRowHeight(15.94, 17);
            worksheet.SetRowHeight(15.94, 18);

            using (ExcelRange range = worksheet.Cells[1, 2, 5, 31]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(214, 214, 214));
            }

            using (ExcelRange range = worksheet.Cells[1, 2, 5, 3]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Thick);
            }

            using (ExcelRange range = worksheet.Cells[2, 2, 2, 3]) {
                range.Merge = true;
                range.Value = "ZWROT\rVAT";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[3, 2, 3, 3]) {
                range.Merge = true;
                range.Value = "DLA PODRÓŻNYCH";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.Font.Size = 5;
            }

            using (ExcelRange range = worksheet.Cells[4, 2, 4, 3]) {
                range.Merge = true;
                range.Value = "TAX FREE";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[5, 2, 5, 3]) {
                range.Merge = true;
                range.Value = "FOR TOURISTS";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.Font.Size = 5;
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 5, 15]) {
                range.Merge = true;
                range.Value = "ZWROT VAT DLA PODRÓŻNYCH\r\"TAX FREE FOR TOURISTS\"";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[1, 16, 2, 22]) {
                range.Merge = true;
                range.Value = "Miejscowość,\rdata dokonania sprzedaży:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[4, 16, 5, 22]) {
                range.Merge = true;
                range.Value = "Numer dokumentu:";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[1, 23, 2, 27]) {
                range.Merge = true;
                range.Value =
                    string.Format("{0}, {1}", "Przemysl", DateTime.Now.ToString("dd.MM.yyyy"));
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[4, 23, 5, 27]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "{0}/{1}/{2}",
                        string.Format(
                            "{0:D5}",
                            taxFree.Number.StartsWith("TF")
                                ? Convert.ToInt32(taxFree.Number.Substring(2, 10))
                                : Convert.ToInt32(taxFree.Number)
                        ),
                        string.Format("{0:D2}", DateTime.Now.Month),
                        DateTime.Now.Year
                    );
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[1, 28, 5, 31]) {
                range.Merge = true;
                range.Value = "POLSKA";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 11;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[7, 2, 7, 5]) {
                range.Merge = true;
                range.Value = "Dane sprzedawcy:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[7, 6, 7, 14]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[7, 17, 7, 22]) {
                range.Merge = true;
                range.Value = "Dane podróżnego:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[7, 23, 7, 31]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[9, 2, 9, 3]) {
                range.Merge = true;
                range.Value = "Nazwa:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[9, 4, 9, 14]) {
                range.Merge = true;
                range.Value = "CONCORD.PL Sp z o.o.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[10, 2, 10, 3]) {
                range.Merge = true;
                range.Value = "Adres:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[10, 4, 10, 14]) {
                range.Merge = true;
                range.Value = "ul. Gen.Jakuba Jasińskiego 58\r37-700, Przemyśl";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[11, 2, 11, 3]) {
                range.Merge = true;
                range.Value = "NIP:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[11, 4, 11, 14]) {
                range.Merge = true;
                range.Value = "8133680920";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[9, 17, 9, 21]) {
                range.Merge = true;
                range.Value = "Nazwisko i imię\rpodróżnego:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            using (ExcelRange range = worksheet.Cells[9, 22, 9, 31]) {
                range.Merge = true;
                range.Value = $"{taxFree.Statham?.LastName} {taxFree.Statham?.FirstName}";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
            }

            using (ExcelRange range = worksheet.Cells[10, 17, 10, 31]) {
                range.Merge = true;
                range.Value = "Adres podróżnego:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[11, 17, 11, 17]) {
                range.Value = "Kraj:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[11, 18, 11, 22]) {
                range.Merge = true;
                range.Value = taxFree.StathamPassport?.PassportIssuedBy;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[11, 23, 11, 25]) {
                range.Merge = true;
                range.Value = "Miejscowość:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
            }

            using (ExcelRange range = worksheet.Cells[11, 26, 11, 31]) {
                range.Merge = true;
                range.Value = taxFree.StathamPassport?.City;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[12, 17, 12, 21]) {
                range.Merge = true;
                range.Value = "Ulica:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[12, 22, 12, 31]) {
                range.Merge = true;
                range.Value = taxFree.StathamPassport?.Street;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[13, 17, 13, 21]) {
                range.Merge = true;
                range.Value = "Numer domu, lokalu:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[13, 22, 13, 31]) {
                range.Merge = true;
                range.Value = taxFree.StathamPassport?.HouseNumber;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[14, 17, 14, 21]) {
                range.Merge = true;
                range.Value = "Numer paszportu:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[14, 22, 14, 31]) {
                range.Merge = true;
                range.Value = $"{taxFree.StathamPassport?.PassportSeria} {taxFree.StathamPassport?.PassportNumber}";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[15, 17, 15, 21]) {
                range.Merge = true;
                range.Value = "Kraj wydania:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[15, 22, 15, 31]) {
                range.Merge = true;
                range.Value = taxFree.StathamPassport?.PassportIssuedBy;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            //Document body

            //Table header
            using (ExcelRange range = worksheet.Cells[17, 2, 18, 2]) {
                range.Merge = true;
                range.Value = "Lp.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 3, 18, 8]) {
                range.Merge = true;
                range.Value = "Nazwa towaru";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 9, 18, 11]) {
                range.Merge = true;
                range.Value = "Jednostka miary";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 12, 18, 14]) {
                range.Merge = true;
                range.Value = "Ilość";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 15, 18, 18]) {
                range.Merge = true;
                range.Value = "Cena jednostkowa bez podatku";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 19, 18, 22]) {
                range.Merge = true;
                range.Value = "Wartość towaru bez podatku";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 23, 18, 25]) {
                range.Merge = true;
                range.Value = "Stawka podatku VAT w %";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 26, 18, 27]) {
                range.Merge = true;
                range.Value = "Kwota podatku w zł";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 28, 18, 31]) {
                range.Merge = true;
                range.Value = "Wartość towaru z podatkiem VAT";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            //Table body
            int row = 19;

            int indexer = 1;

            foreach (TaxFreeItem item in taxFree.TaxFreeItems)
                if (isFromSale) {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Merge = true;
                        range.Value = indexer;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 3, row, 8]) {
                        range.Merge = true;
                        range.Value =
                            string.Format(
                                "{0} / {1}",
                                item.TaxFreePackListOrderItem.OrderItem.Product.NamePL,
                                item.TaxFreePackListOrderItem.OrderItem.Product.VendorCode
                            );
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.WrapText = true;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 9, row, 11]) {
                        range.Merge = true;
                        range.Value =
                            item.TaxFreePackListOrderItem.OrderItem.Product.MeasureUnit.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.WrapText = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 12, row, 14]) {
                        range.Merge = true;
                        range.Value = item.Qty;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.000";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 15, row, 18]) {
                        range.Merge = true;
                        range.Value = item.UnitPricePL;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.WrapText = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 19, row, 22]) {
                        range.Merge = true;
                        range.Value = item.TotalWithoutVatPl;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Font.Bold = true;
                        range.Style.WrapText = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 23, row, 25]) {
                        range.Merge = true;
                        range.Value = taxFree.VatPercent;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.WrapText = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 26, row, 27]) {
                        range.Merge = true;
                        range.Value = item.VatAmountPl;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Font.Bold = true;
                        range.Style.WrapText = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 28, row, 31]) {
                        range.Merge = true;
                        range.Value = item.TotalWithVatPl;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Font.Bold = true;
                        range.Style.WrapText = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    worksheet.SetRowHeight(22.01, row);

                    row++;

                    indexer++;
                } else {
                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Merge = true;
                        range.Value = indexer;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 3, row, 8]) {
                        range.Merge = true;
                        range.Value =
                            string.Format(
                                "{0} / {1}",
                                item.SupplyOrderUkraineCartItem.Product.NamePL,
                                item.SupplyOrderUkraineCartItem.Product.VendorCode
                            );
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.WrapText = true;
                    }

                    using (ExcelRange range = worksheet.Cells[row, 9, row, 11]) {
                        range.Merge = true;
                        range.Value =
                            item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.WrapText = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 12, row, 14]) {
                        range.Merge = true;
                        range.Value = item.Qty;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.000";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 15, row, 18]) {
                        range.Merge = true;
                        range.Value = item.UnitPricePL; //UnitPricePL;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.WrapText = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 19, row, 22]) {
                        range.Merge = true;
                        range.Value = item.TotalWithoutVatPl;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Font.Bold = true;
                        range.Style.WrapText = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 23, row, 25]) {
                        range.Merge = true;
                        range.Value = taxFree.VatPercent;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.WrapText = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 26, row, 27]) {
                        range.Merge = true;
                        range.Value = item.VatAmountPl;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Font.Bold = true;
                        range.Style.WrapText = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 28, row, 31]) {
                        range.Merge = true;
                        range.Value = item.TotalWithVatPl;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Font.Bold = true;
                        range.Style.WrapText = true;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    worksheet.SetRowHeight(22.01, row);

                    row++;

                    indexer++;
                    //}
                }

            worksheet.SetRowHeight(11.71, row);
            worksheet.SetRowHeight(11.71, row + 1);

            using (ExcelRange range = worksheet.Cells[row, 19, row + 1, 25]) {
                range.Merge = true;
                range.Value = "Razem/total/";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 26, row + 1, 27]) {
                range.Merge = true;
                range.Value = taxFree.VatAmountPl;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row + 1, 31]) {
                range.Merge = true;
                range.Value = taxFree.TotalWithVatPl;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            row += 2;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            //Vat totals with verbal
            using (ExcelRange range = worksheet.Cells[row, 2, row, 10]) {
                range.Merge = true;
                range.Value = "Kwota podatku zapłaconego przy nabyciu towarów";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[row, 11, row, 16]) {
                range.Merge = true;
                range.Value = taxFree.VatAmountPl;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, 17, row, 18]) {
                range.Merge = true;
                range.Value = "zł.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[row, 19, row, 21]) {
                range.Merge = true;
                range.Value = "Słownie:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[row, 22, row, 31]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "{0} zł. {1} gr.",
                        taxFree.VatAmountPl.ToText(true, false),
                        (decimal.Round(taxFree.VatAmountPl % 1, 2) * 100m).ToText(false, false)
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            //Document footer
            using (ExcelRange range = worksheet.Cells[row - 1, 2, row, 5]) {
                range.Merge = true;
                range.Value = "Kategorie celne:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row - 2, 2, row, 31]) {
                range.Merge = true;
                range.Value =
                    "Dokument stanowi podstawę do ubiegania się przez podróżnych niemających stałego miejsca zamieszkania na terytorium Unii Europejskiej o zwrot podatku od towarów i usług od nabytych towarów, które w stanie nienaruszonym zostały wywiezione poza terytorium Unii Europejskiej - art. 126-130 ustawy z dnia 11 marca 2004 r. o podatku od towarów i usług (Dz.U. z 2011 r. Nr 177, poz. 1054, z późn. zm.).";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.WrapText = true;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 2, row, 13]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.MediumDashed;
            }

            using (ExcelRange range = worksheet.Cells[row, 19, row, 31]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.MediumDashed;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 2, row, 13]) {
                range.Merge = true;
                range.Value = "podpis podróżnego";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 7;
            }

            using (ExcelRange range = worksheet.Cells[row, 19, row, 31]) {
                range.Merge = true;
                range.Value = "czytelny podpis sprzedającego";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 7;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 2, row, 31]) {
                range.Merge = true;
                range.Value = "Rozliczenie podatku:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 2, row, 6]) {
                range.Merge = true;
                range.Value = "Ustalona forma zwrotu podatku:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 12]) {
                range.Merge = true;
                range.Value = "gotówka";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Italic = true;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, 14, row, 31]) {
                range.Merge = true;
                range.Value = "Zwrot podatku w kwocie ...............................zł ............gr otrzymałem(-łam).";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 19, row, 31]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.MediumDashed;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 19, row, 31]) {
                range.Merge = true;
                range.Value = "data i podpis podróżnego";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 7;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            worksheet.Cells[row, 5].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 2, row + 8, 31]) {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(214, 214, 214));
            }

            using (ExcelRange range = worksheet.Cells[row, 2, row, 4]) {
                range.Merge = true;
                range.Value = "UWAGI URZĘDOWE";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 31]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 2, row, 31]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 2, row, 31]) {
                range.Merge = true;
                range.Value = "Potwierdzam tożsamość podróżnego oraz że towary wymienione w dokumencie zostały wywiezione poza terytorium Unii Europejskiej";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 7;
                range.Style.Font.Bold = true;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 2, row, 31]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 2, row, 31]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 2, row, 31]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 2, row, 14]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.MediumDashed;
            }

            using (ExcelRange range = worksheet.Cells[row, 18, row, 31]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.MediumDashed;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 2, row, 14]) {
                range.Merge = true;
                range.Value = "podpis funkcjonariusza Służby Celno-Skarbowej oraz stempel \"Polska - Clo\"";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 7;
            }

            using (ExcelRange range = worksheet.Cells[row, 18, row, 31]) {
                range.Merge = true;
                range.Value = "stempel potwierdzający wywóz towarów poza terytorium Unii Europejskiej";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 7;
            }

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            row++;

            worksheet.SetRowHeight(11.71, row);

            using (ExcelRange range = worksheet.Cells[row, 2, row, 5]) {
                range.Merge = true;
                range.Value = "Dokument TaxFree nr:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[row, 6, row, 15]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "{0}/{1}/{2}",
                        string.Format(
                            "{0:D5}",
                            taxFree.Number.StartsWith("TF")
                                ? Convert.ToInt32(taxFree.Number.Substring(2, 10))
                                : Convert.ToInt32(taxFree.Number)
                        ),
                        string.Format("{0:D2}", DateTime.Now.Month),
                        DateTime.Now.Year
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[row, 16, row, 30]) {
                range.Merge = true;
                range.Value = "Strona ";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            using (ExcelRange range = worksheet.Cells[row, 31, row, 31]) {
                range.Merge = true;
                range.Value = "1";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
            }

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Arial";
            }

            package.Workbook.Properties.Title = "Tax Free Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            //Saving the file.
            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportTaxFreesToXlsx(string path, List<TaxFree> taxFrees) {
        string fileName = Path.Combine(path, $"{Guid.NewGuid()}_{DateTime.Now.ToString("MM.yyyy")}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            int row = 1;

            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("TaxFree Document");

            worksheet.ApplyPrinterSettings(0.5m, 0.3m, 0.1m, 0.3m, 0.3m, 0.3m, false);

            //Setting default width to columns
            worksheet.SetColumnWidth(1.22, 1);
            worksheet.SetColumnWidth(4.81, 2);
            worksheet.SetColumnWidth(4.75, 3);
            worksheet.SetColumnWidth(5.11, 4);
            worksheet.SetColumnWidth(4.38, 5);
            worksheet.SetColumnWidth(4.10, 6);
            worksheet.SetColumnWidth(4.25, 7);
            worksheet.SetColumnWidth(2.90, 8);
            worksheet.SetColumnWidth(4.00, 9);
            worksheet.SetColumnWidth(1.22, 10);
            worksheet.SetColumnWidth(1.98, 11);
            worksheet.SetColumnWidth(2.11, 12);
            worksheet.SetColumnWidth(2.11, 13);
            worksheet.SetColumnWidth(4.38, 14);
            worksheet.SetColumnWidth(1.22, 15);
            worksheet.SetColumnWidth(1.22, 16);
            worksheet.SetColumnWidth(3.60, 17);
            worksheet.SetColumnWidth(3.78, 18);
            worksheet.SetColumnWidth(1.69, 19);
            worksheet.SetColumnWidth(1.69, 20);
            worksheet.SetColumnWidth(3.48, 21);
            worksheet.SetColumnWidth(5.57, 22);
            worksheet.SetColumnWidth(4.81, 23);
            worksheet.SetColumnWidth(4.00, 24);
            worksheet.SetColumnWidth(3.13, 25);
            worksheet.SetColumnWidth(4.75, 26);
            worksheet.SetColumnWidth(4.93, 27);
            worksheet.SetColumnWidth(3.03, 28);
            worksheet.SetColumnWidth(3.60, 29);
            worksheet.SetColumnWidth(3.03, 30);
            worksheet.SetColumnWidth(3.88, 31);
            worksheet.SetColumnWidth(1.56, 32);

            foreach (TaxFree taxFree in taxFrees) {
                //Document header

                //Setting document header height
                worksheet.SetRowHeight(3.98, row);
                worksheet.SetRowHeight(27.00, row + 1);
                worksheet.SetRowHeight(11.71, row + 2);
                worksheet.SetRowHeight(11.71, row + 3);
                worksheet.SetRowHeight(11.71, row + 4);
                worksheet.SetRowHeight(11.71, row + 5);
                worksheet.SetRowHeight(11.71, row + 6);
                worksheet.SetRowHeight(12.11, row + 7);
                worksheet.SetRowHeight(22.01, row + 8);
                worksheet.SetRowHeight(23.51, row + 9);
                worksheet.SetRowHeight(12.11, row + 10);
                worksheet.SetRowHeight(12.11, row + 11);
                worksheet.SetRowHeight(12.11, row + 12);
                worksheet.SetRowHeight(12.11, row + 13);
                worksheet.SetRowHeight(12.11, row + 14);
                worksheet.SetRowHeight(12.11, row + 15);
                worksheet.SetRowHeight(15.94, row + 16);
                worksheet.SetRowHeight(15.94, row + 17);

                using (ExcelRange range = worksheet.Cells[row, 2, row + 4, 31]) {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(214, 214, 214));
                }

                using (ExcelRange range = worksheet.Cells[row, 2, row + 4, 3]) {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thick);
                }

                using (ExcelRange range = worksheet.Cells[row + 1, 2, row + 1, 3]) {
                    range.Merge = true;
                    range.Value = "ZWROT\rVAT";
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Bold = true;
                }

                using (ExcelRange range = worksheet.Cells[row + 2, 2, row + 2, 3]) {
                    range.Merge = true;
                    range.Value = "DLA PODRÓŻNYCH";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Size = 5;
                }

                using (ExcelRange range = worksheet.Cells[row + 3, 2, row + 3, 3]) {
                    range.Merge = true;
                    range.Value = "TAX FREE";
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Bold = true;
                }

                using (ExcelRange range = worksheet.Cells[row + 4, 2, row + 4, 3]) {
                    range.Merge = true;
                    range.Value = "FOR TOURISTS";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Size = 5;
                }

                using (ExcelRange range = worksheet.Cells[row, 4, row + 4, 15]) {
                    range.Merge = true;
                    range.Value = "ZWROT VAT DLA PODRÓŻNYCH\r\"TAX FREE FOR TOURISTS\"";
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 12;
                    range.Style.Font.Bold = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 16, row + 1, 22]) {
                    range.Merge = true;
                    range.Value = "Miejscowość,\rdata dokonania sprzedaży:";
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row + 3, 16, row + 4, 22]) {
                    range.Merge = true;
                    range.Value = "Numer dokumentu:";
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row, 23, row + 1, 27]) {
                    range.Merge = true;
                    range.Value =
                        string.Format("{0}, {1}", "Przemysl", DateTime.Now.ToString("dd.MM.yyyy"));
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                }

                using (ExcelRange range = worksheet.Cells[row + 3, 23, row + 4, 27]) {
                    range.Merge = true;
                    range.Value =
                        string.Format(
                            "{0}/{1}/{2}",
                            string.Format(
                                "{0:D5}",
                                taxFree.Number.StartsWith("TF")
                                    ? Convert.ToInt32(taxFree.Number.Substring(2, 10))
                                    : Convert.ToInt32(taxFree.Number)
                            ),
                            string.Format("{0:D2}", DateTime.Now.Month),
                            DateTime.Now.Year
                        );
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                }

                using (ExcelRange range = worksheet.Cells[row, 28, row + 4, 31]) {
                    range.Merge = true;
                    range.Value = "POLSKA";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 11;
                    range.Style.Font.Bold = true;
                }

                using (ExcelRange range = worksheet.Cells[row + 6, 2, row + 6, 5]) {
                    range.Merge = true;
                    range.Value = "Dane sprzedawcy:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                using (ExcelRange range = worksheet.Cells[row + 6, 6, row + 6, 14]) {
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                using (ExcelRange range = worksheet.Cells[row + 6, 17, row + 6, 22]) {
                    range.Merge = true;
                    range.Value = "Dane podróżnego:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                using (ExcelRange range = worksheet.Cells[row + 6, 23, row + 6, 31]) {
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                using (ExcelRange range = worksheet.Cells[row + 8, 2, row + 8, 3]) {
                    range.Merge = true;
                    range.Value = "Nazwa:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row + 8, 4, row + 8, 14]) {
                    range.Merge = true;
                    range.Value = "CONCORD.PL Sp z o.o.";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                }

                using (ExcelRange range = worksheet.Cells[row + 9, 2, row + 9, 3]) {
                    range.Merge = true;
                    range.Value = "Adres:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row + 9, 4, row + 9, 14]) {
                    range.Merge = true;
                    range.Value = "ul. Gen.Jakuba Jasińskiego 58\r37-700, Przemyśl";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 9;
                    range.Style.WrapText = true;
                }

                using (ExcelRange range = worksheet.Cells[row + 10, 2, row + 10, 3]) {
                    range.Merge = true;
                    range.Value = "NIP:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row + 10, 4, row + 10, 14]) {
                    range.Merge = true;
                    range.Value = "8133680920";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 9;
                    range.Style.WrapText = true;
                }

                using (ExcelRange range = worksheet.Cells[row + 8, 17, row + 8, 21]) {
                    range.Merge = true;
                    range.Value = "Nazwisko i imię\rpodróżnego:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 8;
                    range.Style.WrapText = true;
                }

                using (ExcelRange range = worksheet.Cells[row + 8, 22, row + 8, 31]) {
                    range.Merge = true;
                    range.Value = $"{taxFree.Statham?.LastName} {taxFree.Statham?.FirstName}";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    range.Style.Font.Size = 9;
                    range.Style.Font.Bold = true;
                }

                using (ExcelRange range = worksheet.Cells[row + 9, 17, row + 9, 31]) {
                    range.Merge = true;
                    range.Value = "Adres podróżnego:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row + 10, 17, row + 10, 17]) {
                    range.Value = "Kraj:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row + 10, 18, row + 10, 22]) {
                    range.Merge = true;
                    range.Value = taxFree.StathamPassport?.PassportIssuedBy;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                using (ExcelRange range = worksheet.Cells[row + 10, 23, row + 10, 25]) {
                    range.Merge = true;
                    range.Value = "Miejscowość:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                }

                using (ExcelRange range = worksheet.Cells[row + 10, 26, row + 10, 31]) {
                    range.Merge = true;
                    range.Value = taxFree.StathamPassport?.City;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                using (ExcelRange range = worksheet.Cells[row + 11, 17, row + 11, 21]) {
                    range.Merge = true;
                    range.Value = "Ulica:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row + 11, 22, row + 11, 31]) {
                    range.Merge = true;
                    range.Value = taxFree.StathamPassport?.Street;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                using (ExcelRange range = worksheet.Cells[row + 12, 17, row + 12, 21]) {
                    range.Merge = true;
                    range.Value = "Numer domu, lokalu:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row + 12, 22, row + 12, 31]) {
                    range.Merge = true;
                    range.Value = taxFree.StathamPassport?.HouseNumber;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                using (ExcelRange range = worksheet.Cells[row + 13, 17, row + 13, 21]) {
                    range.Merge = true;
                    range.Value = "Numer paszportu:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row + 13, 22, row + 13, 31]) {
                    range.Merge = true;
                    range.Value = $"{taxFree.StathamPassport?.PassportSeria} {taxFree.StathamPassport?.PassportNumber}";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                using (ExcelRange range = worksheet.Cells[row + 14, 17, row + 14, 21]) {
                    range.Merge = true;
                    range.Value = "Kraj wydania:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row + 14, 22, row + 14, 31]) {
                    range.Merge = true;
                    range.Value = taxFree.StathamPassport?.PassportIssuedBy;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 9;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                //Document body

                //Table header
                using (ExcelRange range = worksheet.Cells[row + 16, 2, row + 17, 2]) {
                    range.Merge = true;
                    range.Value = "Lp.";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row + 16, 3, row + 17, 8]) {
                    range.Merge = true;
                    range.Value = "Nazwa towaru";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row + 16, 9, row + 17, 11]) {
                    range.Merge = true;
                    range.Value = "Jednostka miary";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row + 16, 12, row + 17, 14]) {
                    range.Merge = true;
                    range.Value = "Ilość";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row + 16, 15, row + 17, 18]) {
                    range.Merge = true;
                    range.Value = "Cena jednostkowa bez podatku";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row + 16, 19, row + 17, 22]) {
                    range.Merge = true;
                    range.Value = "Wartość towaru bez podatku";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row + 16, 23, row + 17, 25]) {
                    range.Merge = true;
                    range.Value = "Stawka podatku VAT w %";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row + 16, 26, row + 17, 27]) {
                    range.Merge = true;
                    range.Value = "Kwota podatku w zł";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row + 16, 28, row + 17, 31]) {
                    range.Merge = true;
                    range.Value = "Wartość towaru z podatkiem VAT";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                row += 18;

                int indexer = 1;

                foreach (TaxFreeItem item in taxFree.TaxFreeItems)
                    if (taxFree.TaxFreePackList.IsFromSale) {
                        using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                            range.Merge = true;
                            range.Value = indexer;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 3, row, 8]) {
                            range.Merge = true;
                            range.Value =
                                string.Format(
                                    "{0} / {1}",
                                    item.TaxFreePackListOrderItem.OrderItem.Product.NamePL,
                                    item.TaxFreePackListOrderItem.OrderItem.Product.VendorCode
                                );
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.WrapText = true;
                        }

                        using (ExcelRange range = worksheet.Cells[row, 9, row, 11]) {
                            range.Merge = true;
                            range.Value =
                                item.TaxFreePackListOrderItem.OrderItem.Product.MeasureUnit.Name;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.WrapText = true;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 12, row, 14]) {
                            range.Merge = true;
                            range.Value = item.Qty;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.000";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 15, row, 18]) {
                            range.Merge = true;
                            range.Value = item.UnitPricePL; //UnitPricePL;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.WrapText = true;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 19, row, 22]) {
                            range.Merge = true;
                            range.Value = item.TotalWithoutVatPl;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Font.Bold = true;
                            range.Style.WrapText = true;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 23, row, 25]) {
                            range.Merge = true;
                            range.Value = taxFree.VatPercent;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.WrapText = true;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 26, row, 27]) {
                            range.Merge = true;
                            range.Value = item.VatAmountPl;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Font.Bold = true;
                            range.Style.WrapText = true;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 28, row, 31]) {
                            range.Merge = true;
                            range.Value = item.TotalWithVatPl;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Font.Bold = true;
                            range.Style.WrapText = true;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        worksheet.SetRowHeight(22.01, row);

                        row++;

                        indexer++;
                    } else {
                        using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                            range.Merge = true;
                            range.Value = indexer;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 3, row, 8]) {
                            range.Merge = true;
                            range.Value =
                                string.Format(
                                    "{0} / {1}",
                                    item.SupplyOrderUkraineCartItem.Product.NamePL,
                                    item.SupplyOrderUkraineCartItem.Product.VendorCode
                                );
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.WrapText = true;
                        }

                        using (ExcelRange range = worksheet.Cells[row, 9, row, 11]) {
                            range.Merge = true;
                            range.Value =
                                item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.WrapText = true;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        using (ExcelRange range = worksheet.Cells[row, 12, row, 14]) {
                            range.Merge = true;
                            range.Value = item.Qty;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.000";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 15, row, 18]) {
                            range.Merge = true;
                            range.Value = item.UnitPricePL; //UnitPricePL;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.WrapText = true;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 19, row, 22]) {
                            range.Merge = true;
                            range.Value = item.TotalWithoutVatPl;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Font.Bold = true;
                            range.Style.WrapText = true;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 23, row, 25]) {
                            range.Merge = true;
                            range.Value = taxFree.VatPercent;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 8;
                            range.Style.WrapText = true;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 26, row, 27]) {
                            range.Merge = true;
                            range.Value = item.VatAmountPl;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Font.Bold = true;
                            range.Style.WrapText = true;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        using (ExcelRange range = worksheet.Cells[row, 28, row, 31]) {
                            range.Merge = true;
                            range.Value = item.TotalWithVatPl;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Font.Size = 10;
                            range.Style.Font.Bold = true;
                            range.Style.WrapText = true;
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Numberformat.Format = "0.00";
                        }

                        worksheet.SetRowHeight(22.01, row);

                        row++;

                        indexer++;
                        //}
                    }

                worksheet.SetRowHeight(11.71, row);
                worksheet.SetRowHeight(11.71, row + 1);

                using (ExcelRange range = worksheet.Cells[row, 19, row + 1, 25]) {
                    range.Merge = true;
                    range.Value = "Razem/total/";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                using (ExcelRange range = worksheet.Cells[row, 26, row + 1, 27]) {
                    range.Merge = true;
                    range.Value = taxFree.VatAmountPl;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.00";
                }

                using (ExcelRange range = worksheet.Cells[row, 28, row + 1, 31]) {
                    range.Merge = true;
                    range.Value = taxFree.TotalWithVatPl;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Numberformat.Format = "0.00";
                }

                row += 2;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                //Vat totals with verbal
                using (ExcelRange range = worksheet.Cells[row, 2, row, 10]) {
                    range.Merge = true;
                    range.Value = "Kwota podatku zapłaconego przy nabyciu towarów";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row, 11, row, 16]) {
                    range.Merge = true;
                    range.Value = taxFree.VatAmountPl;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Numberformat.Format = "0.00";
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                using (ExcelRange range = worksheet.Cells[row, 17, row, 18]) {
                    range.Merge = true;
                    range.Value = "zł.";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row, 19, row, 21]) {
                    range.Merge = true;
                    range.Value = "Słownie:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row, 22, row, 31]) {
                    range.Merge = true;
                    range.Value =
                        string.Format(
                            "{0} zł. {1} gr.",
                            taxFree.VatAmountPl.ToText(true, false),
                            (decimal.Round(taxFree.VatAmountPl % 1, 2) * 100m).ToText(false, false)
                        );
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                //Document footer
                using (ExcelRange range = worksheet.Cells[row - 1, 2, row, 5]) {
                    range.Merge = true;
                    range.Value = "Kategorie celne:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row - 2, 2, row, 31]) {
                    range.Merge = true;
                    range.Value =
                        "Dokument stanowi podstawę do ubiegania się przez podróżnych niemających stałego miejsca zamieszkania na terytorium Unii Europejskiej o zwrot podatku od towarów i usług od nabytych towarów, które w stanie nienaruszonym zostały wywiezione poza terytorium Unii Europejskiej - art. 126-130 ustawy z dnia 11 marca 2004 r. o podatku od towarów i usług (Dz.U. z 2011 r. Nr 177, poz. 1054, z późn. zm.).";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.WrapText = true;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 13]) {
                    range.Merge = true;
                    range.Value = string.Empty;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.MediumDashed;
                }

                using (ExcelRange range = worksheet.Cells[row, 19, row, 31]) {
                    range.Merge = true;
                    range.Value = string.Empty;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.MediumDashed;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 13]) {
                    range.Merge = true;
                    range.Value = "podpis podróżnego";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 7;
                }

                using (ExcelRange range = worksheet.Cells[row, 19, row, 31]) {
                    range.Merge = true;
                    range.Value = "czytelny podpis sprzedającego";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 7;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 31]) {
                    range.Merge = true;
                    range.Value = "Rozliczenie podatku:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 6]) {
                    range.Merge = true;
                    range.Value = "Ustalona forma zwrotu podatku:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 12]) {
                    range.Merge = true;
                    range.Value = "gotówka";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Italic = true;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                using (ExcelRange range = worksheet.Cells[row, 14, row, 31]) {
                    range.Merge = true;
                    range.Value = "Zwrot podatku w kwocie ...............................zł ............gr otrzymałem(-łam).";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Bold = true;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 19, row, 31]) {
                    range.Merge = true;
                    range.Value = string.Empty;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.MediumDashed;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 19, row, 31]) {
                    range.Merge = true;
                    range.Value = "data i podpis podróżnego";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 7;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                worksheet.Cells[row, 5].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row + 8, 31]) {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(214, 214, 214));
                }

                using (ExcelRange range = worksheet.Cells[row, 2, row, 4]) {
                    range.Merge = true;
                    range.Value = "UWAGI URZĘDOWE";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 31]) {
                    range.Merge = true;
                    range.Value = string.Empty;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 31]) {
                    range.Merge = true;
                    range.Value = string.Empty;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 31]) {
                    range.Merge = true;
                    range.Value = "Potwierdzam tożsamość podróżnego oraz że towary wymienione w dokumencie zostały wywiezione poza terytorium Unii Europejskiej";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 7;
                    range.Style.Font.Bold = true;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 31]) {
                    range.Merge = true;
                    range.Value = string.Empty;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 31]) {
                    range.Merge = true;
                    range.Value = string.Empty;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 31]) {
                    range.Merge = true;
                    range.Value = string.Empty;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 14]) {
                    range.Merge = true;
                    range.Value = string.Empty;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.MediumDashed;
                }

                using (ExcelRange range = worksheet.Cells[row, 18, row, 31]) {
                    range.Merge = true;
                    range.Value = string.Empty;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.MediumDashed;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 14]) {
                    range.Merge = true;
                    range.Value = "podpis funkcjonariusza Służby Celno-Skarbowej oraz stempel \"Polska - Clo\"";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 7;
                }

                using (ExcelRange range = worksheet.Cells[row, 18, row, 31]) {
                    range.Merge = true;
                    range.Value = "stempel potwierdzający wywóz towarów poza terytorium Unii Europejskiej";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 7;
                }

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                row++;

                worksheet.SetRowHeight(11.71, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 5]) {
                    range.Merge = true;
                    range.Value = "Dokument TaxFree nr:";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 15]) {
                    range.Merge = true;
                    range.Value =
                        string.Format(
                            "{0}/{1}/{2}",
                            string.Format(
                                "{0:D5}",
                                taxFree.Number.StartsWith("TF")
                                    ? Convert.ToInt32(taxFree.Number.Substring(2, 10))
                                    : Convert.ToInt32(taxFree.Number)
                            ),
                            string.Format("{0:D2}", DateTime.Now.Month),
                            DateTime.Now.Year
                        );
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row, 16, row, 30]) {
                    range.Merge = true;
                    range.Value = "Strona ";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                using (ExcelRange range = worksheet.Cells[row, 31, row, 31]) {
                    range.Merge = true;
                    range.Value = "1";
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                }

                row++;

                worksheet.Row(row).PageBreak = true;

                row++;
            }

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Arial";
            }

            package.Workbook.Properties.Title = "Tax Free Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            //Saving the file.
            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportSadInvoiceToXlsx(string path, Sad sadPl, Sad sadUk, string userFullName, bool isFromSale = false) {
        string fileName = Path.Combine(path, $"EX_SAD_{sadPl.Number}_{DateTime.Now.ToString("dd.MM.yyyy")}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Polish");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Setting default width to columns
            worksheet.SetColumnWidth(1.11, 1);
            worksheet.SetColumnWidth(6.07, 2);
            worksheet.SetColumnWidth(7.61, 3);
            worksheet.SetColumnWidth(8.11, 4);
            worksheet.SetColumnWidth(7.71, 5);
            worksheet.SetColumnWidth(2.77, 6);
            worksheet.SetColumnWidth(3.77, 7);
            worksheet.SetColumnWidth(3.23, 8);
            worksheet.SetColumnWidth(0.00, 9);
            worksheet.SetColumnWidth(0.00, 10);
            worksheet.SetColumnWidth(1.68, 11);
            worksheet.SetColumnWidth(1.75, 12);
            worksheet.SetColumnWidth(2.11, 13);
            worksheet.SetColumnWidth(0.75, 14);
            worksheet.SetColumnWidth(1.61, 15);
            worksheet.SetColumnWidth(0.75, 16);
            worksheet.SetColumnWidth(1.22, 17);
            worksheet.SetColumnWidth(5.11, 18);
            worksheet.SetColumnWidth(0.00, 19);
            worksheet.SetColumnWidth(0.00, 20);
            worksheet.SetColumnWidth(0.11, 21);
            worksheet.SetColumnWidth(0.00, 22);
            worksheet.SetColumnWidth(3.31, 23);
            worksheet.SetColumnWidth(1.38, 24);
            worksheet.SetColumnWidth(0.11, 25);
            worksheet.SetColumnWidth(1.52, 26);
            worksheet.SetColumnWidth(6.93, 27);
            worksheet.SetColumnWidth(0.00, 28);
            worksheet.SetColumnWidth(1.45, 29);
            worksheet.SetColumnWidth(3.77, 30);
            worksheet.SetColumnWidth(2.88, 31);
            worksheet.SetColumnWidth(2.56, 32);
            worksheet.SetColumnWidth(3.88, 33);
            worksheet.SetColumnWidth(2.56, 34);
            worksheet.SetColumnWidth(3.00, 35);
            worksheet.SetColumnWidth(2.56, 36);
            worksheet.SetColumnWidth(2.56, 37);
            worksheet.SetColumnWidth(2.56, 38);
            worksheet.SetColumnWidth(2.56, 39);

            //Document header

            worksheet.SetRowHeight(16, 1);
            worksheet.SetRowHeight(12.80, new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 18 });
            worksheet.SetRowHeight(4, 15);

            using (ExcelRange range = worksheet.Cells[1, 2, 1, 21]) {
                range.Merge = true;
                range.Value = "Oryginał / Kopia";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[1, 22, 1, 29]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[1, 30, 1, 39]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "Przemysl, {0}",
                        sadPl.FromDate.ToString("dd.MM.yyyy")
                    );
                //"Przemysl, 24.10.2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[2, 2, 5, 39]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        @"FAKTURA NR EX{0}/{1}/{2}",
                        string.Format(
                            "{0:D3}",
                            sadPl.Number.StartsWith("EX")
                                ? Convert.ToInt64(sadPl.Number.Substring(2, 10))
                                : Convert.ToInt64(sadPl.Number)
                        ),
                        string.Format("{0:D2}", sadPl.FromDate.Month),
                        sadPl.FromDate.Year
                    );
                //"FAKTURA NR EX0036/10/2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[6, 2, 6, 39]) {
                range.Merge = true;

                if (isFromSale) {
                    if (sadPl.Sales.Any()) {
                        Agreement agreement = sadPl.Sales.First().ClientAgreement.Agreement;

                        range.Value =
                            string.Format(
                                "Umowa:    {0}/{1} z {2}",
                                string.Format("{0:D3}", Convert.ToInt64(agreement.Number)),
                                agreement.Created.ToString("MM/yyyy"),
                                agreement.Created.ToString("dd.MM.yyyy")
                            );
                    }
                } else {
                    if (sadPl.OrganizationClientAgreement != null)
                        range.Value =
                            string.Format(
                                "Umowa:    {0}/{1} z {2}",
                                string.Format("{0:D3}", Convert.ToInt64(sadPl.OrganizationClientAgreement.Number)),
                                sadPl.OrganizationClientAgreement.FromDate.ToString("MM/yyyy"),
                                sadPl.OrganizationClientAgreement.FromDate.ToString("dd.MM.yyyy")
                            );
                    else
                        range.Value =
                            string.Format(
                                "Umowa:    {0}/{1} z {2}",
                                string.Format("{0:D3}", 0),
                                sadPl.FromDate.ToString("MM/yyyy"),
                                sadPl.FromDate.ToString("dd.MM.yyyy")
                            );
                }

                //"Umowa:    001/10/18 z 16.10.2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[7, 2, 7, 21]) {
                range.Merge = true;
                range.Value = "Sprzedawca:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[7, 22, 7, 39]) {
                range.Merge = true;
                range.Value = "Nabywca:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 2, 15, 21]) {
                range.Merge = true;
                range.Value =
                    "CONCORD.PL Sp z o.o.,\r37-700, Przemyśl, ul. Gen.Jakuba Jasińskiego 58,\rNIP 8133680920,\rBank BPS S.A.\rIBAN: PL55193013182740072619290003\rSWIFT : POLUPLPR\rBRANCH CODE : 1930 1318";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            string completeRecipient = string.Empty;

            if (isFromSale) {
                if (sadPl.Sales.Any()) {
                    Client client = sadPl.Sales.First().ClientAgreement.Client;

                    if (!string.IsNullOrEmpty(client.FirstName) && !string.IsNullOrEmpty(client.LastName))
                        completeRecipient = $"{client.LastName} {client.FirstName} {client.MiddleName} ";
                    else
                        completeRecipient += client.FullName;

                    completeRecipient +=
                        string.IsNullOrEmpty(client.ActualAddress)
                            ? string.Empty
                            : client.ActualAddress + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.City)
                            ? string.Empty
                            : client.RegionCode.City + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.District)
                            ? string.Empty
                            : client.RegionCode.District + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.TIN)
                            ? string.Empty
                            : "\rNIP " + client.TIN;
                }
            } else {
                if (sadPl.OrganizationClient != null) {
                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.OrganizationClient.FullName)
                            ? string.Empty
                            : sadPl.OrganizationClient.FullName + "  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.OrganizationClient.Address)
                            ? string.Empty
                            : sadPl.OrganizationClient.Address + ", ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.OrganizationClient.City)
                            ? string.Empty
                            : sadPl.OrganizationClient.City + ", ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.OrganizationClient.Country)
                            ? string.Empty
                            : sadPl.OrganizationClient.Country + ", ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.OrganizationClient.NIP)
                            ? string.Empty
                            : "\rNIP " + sadPl.OrganizationClient.NIP;
                }
            }

            using (ExcelRange range = worksheet.Cells[8, 22, 15, 39]) {
                range.Merge = true;
                range.Value = completeRecipient;
                //"Prywatny przedsiębiorca Bereshvili Vadym Viktorovych  st.Chornovola,176/7, Chmielnicky, UKRAINA\rNIP 3100401117,";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            //Document body

            //Table header

            using (ExcelRange range = worksheet.Cells[16, 2, 18, 2]) {
                range.Merge = true;
                range.Value = "Lp.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 3, 18, 17]) {
                range.Merge = true;
                range.Value = "Nazwa towaru lub usługi";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 18, 18, 21]) {
                range.Merge = true;
                range.Value = "Jednostka miary";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 22, 18, 26]) {
                range.Merge = true;
                range.Value = "Ilość towaru/usługi";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 27, 18, 29]) {
                range.Merge = true;
                range.Value = "Cena za jednostke netto";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 30, 18, 32]) {
                range.Merge = true;
                range.Value = "Wartość netto";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 33, 16, 36]) {
                range.Merge = true;
                range.Value = "Podatek";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 33, 18, 34]) {
                range.Merge = true;
                range.Value = "Stawka %";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 35, 18, 36]) {
                range.Merge = true;
                range.Value = "Kwota";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 37, 18, 39]) {
                range.Merge = true;
                range.Value = "Wartość brutto EURO";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            //Table body

            int row = 19;

            //Grouped measure units
            List<MeasureUnitGrouped> units = new();

            decimal totalNetPrice = 0m;
            decimal totalVatAmount = 0m;
            decimal totalGrossPrice = 0m;
            decimal vatPercent = 0m;

            int index = 1;

            if (isFromSale)
                foreach (Sale sale in sadPl.Sales)
                foreach (OrderItem item in sale.Order.OrderItems) {
                    decimal currentNetPrice =
                        decimal.Round(
                            item.PricePerItem * Convert.ToDecimal(item.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentVatAmount =
                        decimal.Round(
                            currentNetPrice * vatPercent / 100,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentGrossPrice =
                        decimal.Round(
                            currentNetPrice + currentVatAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    totalNetPrice =
                        decimal.Round(totalNetPrice + currentNetPrice, 2, MidpointRounding.AwayFromZero);

                    totalVatAmount =
                        decimal.Round(totalVatAmount + currentVatAmount, 2, MidpointRounding.AwayFromZero);

                    totalGrossPrice =
                        decimal.Round(totalGrossPrice + currentGrossPrice, 2, MidpointRounding.AwayFromZero);

                    if (units.Any(u => u.Name.Equals(item.Product.MeasureUnit.Name)))
                        units.First(u => u.Name.Equals(item.Product.MeasureUnit.Name)).Qty += item.Qty;
                    else
                        units.Add(new MeasureUnitGrouped {
                            Name = item.Product.MeasureUnit.Name,
                            Qty = item.Qty
                        });

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Merge = true;
                        range.Value = index;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    string fullName =
                        string.Format(
                            "{0} {1}",
                            item.Product.VendorCode,
                            item.Product.NamePL
                        );

                    using (ExcelRange range = worksheet.Cells[row, 3, row, 17]) {
                        range.Merge = true;
                        range.Value = fullName;
                        range.Style.WrapText = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 18, row, 21]) {
                        range.Merge = true;
                        range.Value = item.Product.MeasureUnit.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 22, row, 26]) {
                        range.Merge = true;
                        range.Value = item.Qty;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                        range.Merge = true;
                        range.Value = item.PricePerItem;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                        range.Merge = true;
                        range.Value = currentNetPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                        range.Merge = true;
                        range.Value = $"{vatPercent}%";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                        range.Merge = true;
                        range.Value = currentVatAmount;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                        range.Merge = true;
                        range.Value = currentGrossPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    worksheet.SetRowHeight(
                        fullName.Length > 45
                            ? 27
                            : 12.80,
                        row
                    );

                    row++;

                    index++;
                }
            else
                foreach (SadItem item in sadPl.SadItems) {
                    decimal currentNetPrice =
                        decimal.Round(
                            item.SupplyOrderUkraineCartItem.UnitPrice * Convert.ToDecimal(item.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentVatAmount =
                        decimal.Round(
                            currentNetPrice * vatPercent / 100,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentGrossPrice =
                        decimal.Round(
                            currentNetPrice + currentVatAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    totalNetPrice =
                        decimal.Round(totalNetPrice + currentNetPrice, 2, MidpointRounding.AwayFromZero);

                    totalVatAmount =
                        decimal.Round(totalVatAmount + currentVatAmount, 2, MidpointRounding.AwayFromZero);

                    totalGrossPrice =
                        decimal.Round(totalGrossPrice + currentGrossPrice, 2, MidpointRounding.AwayFromZero);

                    if (units.Any(u => u.Name.Equals(item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name)))
                        units.First(u => u.Name.Equals(item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name)).Qty += item.Qty;
                    else
                        units.Add(new MeasureUnitGrouped {
                            Name = item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name,
                            Qty = item.Qty
                        });

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Merge = true;
                        range.Value = index;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    string fullName =
                        string.Format(
                            "{0} {1}",
                            item.SupplyOrderUkraineCartItem.Product.VendorCode,
                            item.SupplyOrderUkraineCartItem.Product.NamePL
                        );

                    using (ExcelRange range = worksheet.Cells[row, 3, row, 17]) {
                        range.Merge = true;
                        range.Value = fullName;
                        range.Style.WrapText = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 18, row, 21]) {
                        range.Merge = true;
                        range.Value = item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 22, row, 26]) {
                        range.Merge = true;
                        range.Value = item.Qty;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                        range.Merge = true;
                        range.Value = item.SupplyOrderUkraineCartItem.UnitPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                        range.Merge = true;
                        range.Value = currentNetPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                        range.Merge = true;
                        range.Value = $"{vatPercent}%";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                        range.Merge = true;
                        range.Value = currentVatAmount;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                        range.Merge = true;
                        range.Value = currentGrossPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    worksheet.SetRowHeight(
                        fullName.Length > 45
                            ? 27
                            : 12.80,
                        row
                    );

                    row++;

                    index++;
                }

            using (ExcelRange range = worksheet.Cells[row, 2, row + 1, 26]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                range.Merge = true;
                range.Value = "Razem";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                range.Merge = true;
                range.Value = totalNetPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                range.Merge = true;
                range.Value = "Х";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmount;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                range.Merge = true;
                range.Value = "W tym VAT";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                range.Merge = true;
                range.Value = totalNetPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                range.Merge = true;
                range.Value = $"{vatPercent}%";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmount;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            worksheet.SetRowHeight(22, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(10, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Słownie:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "{0} euro {1} centy",
                        totalGrossPrice.ToText(true, false),
                        (decimal.Round(totalGrossPrice % 1, 2) * 100m).ToText(false, false)
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Razem do zapłaty:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 21]) {
                range.Merge = true;
                range.Value = string.Format("{0} EURO", totalGrossPrice);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 22, row, 27]) {
                range.Merge = true;
                range.Value = "Data sprzedaży:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 39]) {
                range.Merge = true;
                range.Value = DateTime.Now.ToString("dd.MM.yyyy");
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Sposób zapłaty:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = "gotówka";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Podsumowania ilości dla jednostek:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            string grouped = units.Aggregate(string.Empty, (current, unit) => current + $"{unit.Qty} {unit.Name} ");

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = grouped;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Umowy i miejsce dostawy:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Ilość miejsc:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Waga netto:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = $"{sadPl.TotalNetWeight} kg";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Waga brutto:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = $"{sadPl.TotalGrossWeight} kg";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Numer samochodu:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Panstwo pochodzenia towaru:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = "Sprzedaż w eksporcie. Towary objęte są 0% stawką VAT.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 39]) {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(8, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 4, row, 11]) {
                range.Merge = true;
                range.Value = "otrzymujący fakturę";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 25, row, 31]) {
                range.Merge = true;
                range.Value = "wystawiający fakturę";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 36]) {
                range.Merge = true;
                range.Value = userFullName;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 11]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 25, row, 36]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(38.4, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 11]) {
                range.Merge = true;
                range.Value = "podpis";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 25, row, 36]) {
                range.Merge = true;
                range.Value = "podpis";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(10.01, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 39]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row - 4, 1, row, 1]) {
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row - 4, 40, row, 40]) {
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(10.01, row);

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Arial";
            }

            worksheet = package.Workbook.Worksheets.Add("Ukrainian");

            worksheet.PrinterSettings.TopMargin = 0.3;
            worksheet.PrinterSettings.BottomMargin = 0.3;
            worksheet.PrinterSettings.RightMargin = 0.1;
            worksheet.PrinterSettings.LeftMargin = 0.1;
            worksheet.PrinterSettings.HeaderMargin = 0.3;
            worksheet.PrinterSettings.FooterMargin = 0.3;
            worksheet.PrinterSettings.FitToPage = true;

            //Setting default width to columns
            worksheet.SetColumnWidth(1.11, 1);
            worksheet.SetColumnWidth(6.07, 2);
            worksheet.SetColumnWidth(7.61, 3);
            worksheet.SetColumnWidth(8.11, 4);
            worksheet.SetColumnWidth(7.71, 5);
            worksheet.SetColumnWidth(2.77, 6);
            worksheet.SetColumnWidth(3.77, 7);
            worksheet.SetColumnWidth(3.23, 8);
            worksheet.SetColumnWidth(0.00, 9);
            worksheet.SetColumnWidth(0.00, 10);
            worksheet.SetColumnWidth(1.68, 11);
            worksheet.SetColumnWidth(1.75, 12);
            worksheet.SetColumnWidth(2.11, 13);
            worksheet.SetColumnWidth(0.75, 14);
            worksheet.SetColumnWidth(1.61, 15);
            worksheet.SetColumnWidth(0.75, 16);
            worksheet.SetColumnWidth(1.22, 17);
            worksheet.SetColumnWidth(5.11, 18);
            worksheet.SetColumnWidth(0.00, 19);
            worksheet.SetColumnWidth(0.00, 20);
            worksheet.SetColumnWidth(0.11, 21);
            worksheet.SetColumnWidth(0.00, 22);
            worksheet.SetColumnWidth(3.31, 23);
            worksheet.SetColumnWidth(1.88, 24);
            worksheet.SetColumnWidth(0.11, 25);
            worksheet.SetColumnWidth(1.52, 26);
            worksheet.SetColumnWidth(6.93, 27);
            worksheet.SetColumnWidth(0.00, 28);
            worksheet.SetColumnWidth(1.45, 29);
            worksheet.SetColumnWidth(3.77, 30);
            worksheet.SetColumnWidth(2.88, 31);
            worksheet.SetColumnWidth(2.56, 32);
            worksheet.SetColumnWidth(3.88, 33);
            worksheet.SetColumnWidth(2.56, 34);
            worksheet.SetColumnWidth(3.00, 35);
            worksheet.SetColumnWidth(2.56, 36);
            worksheet.SetColumnWidth(2.56, 37);
            worksheet.SetColumnWidth(2.56, 38);
            worksheet.SetColumnWidth(2.56, 39);

            //Document header

            worksheet.SetRowHeight(16, 1);
            worksheet.SetRowHeight(12.80, new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 16 });
            worksheet.SetRowHeight(13.11, new[] { 17, 18 });
            worksheet.SetRowHeight(4, 15);

            using (ExcelRange range = worksheet.Cells[1, 2, 1, 21]) {
                range.Merge = true;
                range.Value = "Оригінал / Копія";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[1, 22, 1, 29]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[1, 30, 1, 39]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "Перемишль, {0}",
                        sadUk.FromDate.ToString("dd.MM.yyyy")
                    );
                //"Перемишль, 24.10.2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[2, 2, 5, 39]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "Рахунок - фактура NR EX{0}/{1}",
                        string.Format(
                            "{0:D3}",
                            sadUk.Number.StartsWith("EX")
                                ? Convert.ToInt64(sadUk.Number.Substring(2, 10))
                                : Convert.ToInt64(sadUk.Number)
                        ),
                        sadUk.FromDate.ToString("MM/yyyy")
                    );
                //"Рахунок - фактура NR EX0036/10/2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[6, 2, 6, 39]) {
                range.Merge = true;

                if (isFromSale) {
                    if (sadUk.Sales.Any()) {
                        Agreement agreement = sadUk.Sales.First().ClientAgreement.Agreement;

                        range.Value =
                            string.Format(
                                "Договір:    {0}/{1} z {2}",
                                string.Format("{0:D3}", Convert.ToInt64(agreement.Number)),
                                agreement.Created.ToString("MM/yyyy"),
                                agreement.Created.ToString("dd.MM.yyyy")
                            );
                    }
                } else {
                    if (sadUk.OrganizationClientAgreement != null)
                        range.Value =
                            string.Format(
                                "Umowa:    {0}/{1} z {2}",
                                string.Format("{0:D3}", Convert.ToInt64(sadUk.OrganizationClientAgreement.Number)),
                                sadUk.OrganizationClientAgreement.FromDate.ToString("MM/yyyy"),
                                sadUk.OrganizationClientAgreement.FromDate.ToString("dd.MM.yyyy")
                            );
                    else
                        range.Value =
                            string.Format(
                                "Umowa:    {0}/{1} z {2}",
                                string.Format("{0:D3}", 0),
                                sadUk.FromDate.ToString("MM/yyyy"),
                                sadUk.FromDate.ToString("dd.MM.yyyy")
                            );
                }

                //"Договір:    001/10/18 z 16.10.2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[7, 2, 7, 21]) {
                range.Merge = true;
                range.Value = "Продавець:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[7, 22, 7, 39]) {
                range.Merge = true;
                range.Value = "Покупець:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 2, 15, 21]) {
                range.Merge = true;
                range.Value =
                    "CONCORD.PL Sp z o.o.,\r37-700, Przemyśl, ul. Gen.Jakuba Jasińskiego 58,\rNIP 8133680920,\rBank BPS S.A.\rIBAN: PL55193013182740072619290003\rSWIFT : POLUPLPR\rBRANCH CODE : 1930 1318";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            completeRecipient = string.Empty;

            if (isFromSale) {
                if (sadUk.Sales.Any()) {
                    Client client = sadUk.Sales.First().ClientAgreement.Client;

                    if (!string.IsNullOrEmpty(client.FirstName) && !string.IsNullOrEmpty(client.LastName))
                        completeRecipient = $"{client.LastName} {client.FirstName} {client.MiddleName} ";
                    else
                        completeRecipient += client.FullName;

                    completeRecipient +=
                        string.IsNullOrEmpty(client.ActualAddress)
                            ? string.Empty
                            : client.ActualAddress + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.City)
                            ? string.Empty
                            : client.RegionCode.City + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.District)
                            ? string.Empty
                            : client.RegionCode.District + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.TIN)
                            ? string.Empty
                            : "\rNIP " + client.TIN;
                }
            } else {
                if (sadUk.OrganizationClient != null) {
                    completeRecipient +=
                        string.IsNullOrEmpty(sadUk.OrganizationClient.FullName)
                            ? string.Empty
                            : sadUk.OrganizationClient.FullName + "  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadUk.OrganizationClient.Address)
                            ? string.Empty
                            : sadUk.OrganizationClient.Address + ", ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadUk.OrganizationClient.City)
                            ? string.Empty
                            : sadUk.OrganizationClient.City + ", ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadUk.OrganizationClient.Country)
                            ? string.Empty
                            : sadUk.OrganizationClient.Country + ", ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadUk.OrganizationClient.NIP)
                            ? string.Empty
                            : "\rNIP " + sadUk.OrganizationClient.NIP;
                }
            }

            using (ExcelRange range = worksheet.Cells[8, 22, 15, 39]) {
                range.Merge = true;
                range.Value = completeRecipient;
                //"Prywatny przedsiębiorca Bereshvili Vadym Viktorovych  st.Chornovola,176/7, Chmielnicky, UKRAINA\rNIP 3100401117,";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            //Document body

            //Table header

            using (ExcelRange range = worksheet.Cells[16, 2, 18, 2]) {
                range.Merge = true;
                range.Value = "№ п/п";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 3, 18, 17]) {
                range.Merge = true;
                range.Value = "Назва товару або послуги";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 18, 18, 21]) {
                range.Merge = true;
                range.Value = "Од. виміру";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 22, 18, 26]) {
                range.Merge = true;
                range.Value = "к-сть товару/послуги";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 27, 18, 29]) {
                range.Merge = true;
                range.Value = "Ціна за одиницю нетто";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 30, 18, 32]) {
                range.Merge = true;
                range.Value = "Сума нетто";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 33, 16, 36]) {
                range.Merge = true;
                range.Value = "Податок";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 33, 18, 34]) {
                range.Merge = true;
                range.Value = "Ставка %";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 35, 18, 36]) {
                range.Merge = true;
                range.Value = "Сума";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 37, 18, 39]) {
                range.Merge = true;
                range.Value = "Сума брутто Євро";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            //Table body

            row = 19;

            //Grouped measure units
            units = new List<MeasureUnitGrouped>();

            totalNetPrice = 0m;
            totalVatAmount = 0m;
            totalGrossPrice = 0m;

            if (isFromSale)
                foreach (Sale sale in sadUk.Sales)
                foreach (OrderItem item in sale.Order.OrderItems) {
                    decimal currentNetPrice =
                        decimal.Round(
                            item.PricePerItem * Convert.ToDecimal(item.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentVatAmount =
                        decimal.Round(
                            currentNetPrice * vatPercent / 100,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentGrossPrice =
                        decimal.Round(
                            currentNetPrice + currentVatAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    totalNetPrice =
                        decimal.Round(totalNetPrice + currentNetPrice, 2, MidpointRounding.AwayFromZero);

                    totalVatAmount =
                        decimal.Round(totalVatAmount + currentVatAmount, 2, MidpointRounding.AwayFromZero);

                    totalGrossPrice =
                        decimal.Round(totalGrossPrice + currentGrossPrice, 2, MidpointRounding.AwayFromZero);

                    if (units.Any(u => u.Name.Equals(item.Product.MeasureUnit.Name)))
                        units.First(u => u.Name.Equals(item.Product.MeasureUnit.Name)).Qty += item.Qty;
                    else
                        units.Add(new MeasureUnitGrouped {
                            Name = item.Product.MeasureUnit.Name,
                            Qty = item.Qty
                        });

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Merge = true;
                        range.Value = index;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    string fullName =
                        string.Format(
                            "{0} {1}",
                            item.Product.VendorCode,
                            item.Product.NameUA
                        );

                    using (ExcelRange range = worksheet.Cells[row, 3, row, 17]) {
                        range.Merge = true;
                        range.Value = fullName;
                        range.Style.WrapText = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 18, row, 21]) {
                        range.Merge = true;
                        range.Value = item.Product.MeasureUnit.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 22, row, 26]) {
                        range.Merge = true;
                        range.Value = item.Qty;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                        range.Merge = true;
                        range.Value = item.PricePerItem;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                        range.Merge = true;
                        range.Value = currentNetPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                        range.Merge = true;
                        range.Value = $"{vatPercent}%";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                        range.Merge = true;
                        range.Value = currentVatAmount;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                        range.Merge = true;
                        range.Value = currentGrossPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    worksheet.SetRowHeight(
                        fullName.Length > 45
                            ? 27
                            : 12.80,
                        row
                    );

                    row++;

                    index++;
                }
            else
                foreach (SadItem item in sadUk.SadItems) {
                    decimal currentNetPrice =
                        decimal.Round(
                            item.SupplyOrderUkraineCartItem.UnitPrice * Convert.ToDecimal(item.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentVatAmount =
                        decimal.Round(
                            currentNetPrice * vatPercent / 100,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentGrossPrice =
                        decimal.Round(
                            currentNetPrice + currentVatAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    totalNetPrice =
                        decimal.Round(totalNetPrice + currentNetPrice, 2, MidpointRounding.AwayFromZero);

                    totalVatAmount =
                        decimal.Round(totalVatAmount + currentVatAmount, 2, MidpointRounding.AwayFromZero);

                    totalGrossPrice =
                        decimal.Round(totalGrossPrice + currentGrossPrice, 2, MidpointRounding.AwayFromZero);

                    if (units.Any(u => u.Name.Equals(item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name)))
                        units.First(u => u.Name.Equals(item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name)).Qty += item.Qty;
                    else
                        units.Add(new MeasureUnitGrouped {
                            Name = item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name,
                            Qty = item.Qty
                        });

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Merge = true;
                        range.Value = index;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    string fullName =
                        string.Format(
                            "{0} {1}",
                            item.SupplyOrderUkraineCartItem.Product.VendorCode,
                            item.SupplyOrderUkraineCartItem.Product.NameUA
                        );

                    using (ExcelRange range = worksheet.Cells[row, 3, row, 17]) {
                        range.Merge = true;
                        range.Value = fullName;
                        range.Style.WrapText = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 18, row, 21]) {
                        range.Merge = true;
                        range.Value = item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 22, row, 26]) {
                        range.Merge = true;
                        range.Value = item.Qty;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                        range.Merge = true;
                        range.Value = item.SupplyOrderUkraineCartItem.UnitPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                        range.Merge = true;
                        range.Value = currentNetPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                        range.Merge = true;
                        range.Value = $"{vatPercent}%";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                        range.Merge = true;
                        range.Value = currentVatAmount;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                        range.Merge = true;
                        range.Value = currentGrossPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    worksheet.SetRowHeight(
                        fullName.Length > 45
                            ? 27
                            : 12.80,
                        row
                    );

                    row++;

                    index++;
                }

            using (ExcelRange range = worksheet.Cells[row, 2, row + 1, 26]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                range.Merge = true;
                range.Value = "Разом";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                range.Merge = true;
                range.Value = totalNetPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                range.Merge = true;
                range.Value = "Х";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmount;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                range.Merge = true;
                range.Value = "В тому числі ПДВ";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                range.Merge = true;
                range.Value = totalNetPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                range.Merge = true;
                range.Value = $"{vatPercent}%";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmount;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            worksheet.SetRowHeight(22, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(10, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Словами:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            decimal cents = decimal.Round(totalGrossPrice % 1, 2) * 100m;

            int fullNumber = Convert.ToInt32(cents);

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

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "{0} євро {1} {2}",
                        totalGrossPrice.ToText(true, true),
                        cents.ToText(false, true),
                        endKeyWord
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Разом до оплати:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 21]) {
                range.Merge = true;
                range.Value = string.Format("{0} Євро", totalGrossPrice);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 22, row, 27]) {
                range.Merge = true;
                range.Value = "Дата продажу:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 39]) {
                range.Merge = true;
                range.Value = DateTime.Now.ToString("dd.MM.yyyy");
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Спосіб оплати:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = "готівка";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Підсумовування кількості для одиниць";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            grouped = string.Empty;

            foreach (MeasureUnitGrouped unit in units) grouped += $"{unit.Qty} {unit.Name} ";

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = grouped;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Умови та місце поставки:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Кількість місць:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Вага нетто:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = $"{sadUk.TotalNetWeight} кг";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Вага брутто:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = $"{sadUk.TotalGrossWeight} кг";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Номер автомобіля:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Країна походження товару:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = "Продажа в експорті. Товар підлягає 0% ставці ПДВ.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 39]) {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(8, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 4, row, 11]) {
                range.Merge = true;
                range.Value = "отримав рахунок-фактуру";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 23, row, 31]) {
                range.Merge = true;
                range.Value = "виставив рахунок-фактуру";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 36]) {
                range.Merge = true;
                range.Value = userFullName;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 11]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 23, row, 36]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(38.4, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 11]) {
                range.Merge = true;
                range.Value = "підпис";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 23, row, 36]) {
                range.Merge = true;
                range.Value = "підпис";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(10.01, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 39]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row - 4, 1, row, 1]) {
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row - 4, 40, row, 40]) {
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(10.01, row);

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Arial";
            }

            package.Workbook.Properties.Title = "Sad Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            //Saving the file.
            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportOldSadInvoiceToXlsx(string path, Sad sadPl, Sad sadUk, string userFullName, bool isFromSale = false) {
        string fileName = Path.Combine(path, $"SAD_{sadPl.Number}_{DateTime.Now.ToString("dd.MM.yyyy")}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Polish");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Setting default width to columns
            worksheet.SetColumnWidth(1.11, 1);
            worksheet.SetColumnWidth(6.07, 2);
            worksheet.SetColumnWidth(7.61, 3);
            worksheet.SetColumnWidth(8.11, 4);
            worksheet.SetColumnWidth(7.71, 5);
            worksheet.SetColumnWidth(2.77, 6);
            worksheet.SetColumnWidth(3.77, 7);
            worksheet.SetColumnWidth(3.23, 8);
            worksheet.SetColumnWidth(1.11, 9);
            worksheet.SetColumnWidth(1.11, 10);
            worksheet.SetColumnWidth(1.68, 11);
            worksheet.SetColumnWidth(2.75, 12);
            worksheet.SetColumnWidth(2.11, 13);
            worksheet.SetColumnWidth(1.75, 14);
            worksheet.SetColumnWidth(2.61, 15);
            worksheet.SetColumnWidth(1.75, 16);
            worksheet.SetColumnWidth(2.22, 17);
            worksheet.SetColumnWidth(10.11, 18);
            worksheet.SetColumnWidth(1.11, 19);
            worksheet.SetColumnWidth(1.11, 20);
            worksheet.SetColumnWidth(0.11, 21);
            worksheet.SetColumnWidth(1.11, 22);
            worksheet.SetColumnWidth(3.31, 23);
            worksheet.SetColumnWidth(2.38, 24);
            worksheet.SetColumnWidth(1.11, 25);
            worksheet.SetColumnWidth(2.52, 26);
            worksheet.SetColumnWidth(6.93, 27);
            worksheet.SetColumnWidth(2.11, 28);
            worksheet.SetColumnWidth(5.45, 29);
            worksheet.SetColumnWidth(7.77, 30);
            worksheet.SetColumnWidth(2.88, 31);
            worksheet.SetColumnWidth(2.56, 32);
            worksheet.SetColumnWidth(3.88, 33);
            worksheet.SetColumnWidth(2.56, 34);
            worksheet.SetColumnWidth(3.00, 35);
            worksheet.SetColumnWidth(2.56, 36);
            worksheet.SetColumnWidth(2.56, 37);
            worksheet.SetColumnWidth(2.56, 38);
            worksheet.SetColumnWidth(2.56, 39);

            //Document header

            worksheet.SetRowHeight(4, 1);
            worksheet.SetRowHeight(16, 2);
            worksheet.SetRowHeight(12.80, new[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 18 });
            worksheet.SetRowHeight(4, 15);

            using (ExcelRange range = worksheet.Cells[2, 2, 2, 21]) {
                range.Merge = true;
                range.Value = "Oryginał / Kopia";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[2, 22, 2, 29]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[2, 30, 2, 39]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "Przemysl, {0}",
                        sadPl.FromDate.ToString("dd.MM.yyyy")
                    );
                //"Przemysl, 24.10.2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[3, 2, 6, 39]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        @"FAKTURA NR EX{0}/{1}/{2}",
                        string.Format(
                            "{0:D3}",
                            sadPl.Number.StartsWith("EX")
                                ? Convert.ToInt64(sadPl.Number.Substring(2, 10))
                                : Convert.ToInt64(sadPl.Number)
                        ),
                        string.Format("{0:D2}", sadPl.FromDate.Month),
                        sadPl.FromDate.Year
                    );
                //"FAKTURA NR EX0036/10/2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[7, 2, 7, 21]) {
                range.Merge = true;
                range.Value = "Sprzedawca:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[7, 22, 7, 39]) {
                range.Merge = true;
                range.Value = "Nabywca:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 2, 15, 21]) {
                range.Merge = true;
                range.Value = "CONCORD.PL Sp z o.o.,\r37-700, Przemyśl, ul. Gen.Jakuba Jasińskiego 58,\rNIP 8133680920,\r12193013182740072619290001,  Bank BPS S.A.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            string completeRecipient = string.Empty;

            if (isFromSale) {
                if (sadPl.Sales.Any()) {
                    Client client = sadPl.Sales.First().ClientAgreement.Client;

                    if (!string.IsNullOrEmpty(client.FirstName) && !string.IsNullOrEmpty(client.LastName))
                        completeRecipient = $"{client.LastName} {client.FirstName} {client.MiddleName} ";
                    else
                        completeRecipient += client.FullName;

                    completeRecipient +=
                        string.IsNullOrEmpty(client.ActualAddress)
                            ? string.Empty
                            : client.ActualAddress + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.City)
                            ? string.Empty
                            : client.RegionCode.City + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.District)
                            ? string.Empty
                            : client.RegionCode.District + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.TIN)
                            ? string.Empty
                            : "\rNIP " + client.TIN;
                }
            } else {
                if (sadPl.Statham != null) {
                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.Statham.LastName)
                            ? string.Empty
                            : sadPl.Statham.LastName + "  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.Statham.FirstName)
                            ? string.Empty
                            : sadPl.Statham.FirstName + "  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadPl.Statham.MiddleName)
                            ? string.Empty
                            : sadPl.Statham.MiddleName + "  ";

                    if (sadPl.StathamPassport != null) {
                        completeRecipient +=
                            string.IsNullOrEmpty(sadPl.StathamPassport.City)
                                ? string.Empty
                                : sadPl.StathamPassport.City + "  ";

                        completeRecipient +=
                            string.IsNullOrEmpty(sadPl.StathamPassport.Street)
                                ? string.Empty
                                : sadPl.StathamPassport.Street + "  ";

                        completeRecipient +=
                            string.IsNullOrEmpty(sadPl.StathamPassport.PassportSeria)
                                ? string.Empty
                                : sadPl.StathamPassport.PassportSeria + "  ";

                        completeRecipient +=
                            string.IsNullOrEmpty(sadPl.StathamPassport.PassportNumber)
                                ? string.Empty
                                : sadPl.StathamPassport.PassportNumber + "  ";
                    }
                }
            }

            using (ExcelRange range = worksheet.Cells[8, 22, 15, 39]) {
                range.Merge = true;
                range.Value = completeRecipient;
                //"Prywatny przedsiębiorca Bereshvili Vadym Viktorovych  st.Chornovola,176/7, Chmielnicky, UKRAINA\rNIP 3100401117,";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            //Document body

            //Table header

            using (ExcelRange range = worksheet.Cells[16, 2, 18, 2]) {
                range.Merge = true;
                range.Value = "Lp.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 3, 18, 17]) {
                range.Merge = true;
                range.Value = "Nazwa towaru lub usługi";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 18, 18, 21]) {
                range.Merge = true;
                range.Value = "Jednostka miary";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 22, 18, 26]) {
                range.Merge = true;
                range.Value = "Ilość towaru/usługi";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 27, 18, 29]) {
                range.Merge = true;
                range.Value = "Cena za jednostke netto";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 30, 18, 32]) {
                range.Merge = true;
                range.Value = "Wartość netto";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 33, 16, 36]) {
                range.Merge = true;
                range.Value = "Podatek";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 33, 18, 34]) {
                range.Merge = true;
                range.Value = "Stawka %";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 35, 18, 36]) {
                range.Merge = true;
                range.Value = "Kwota";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 37, 18, 39]) {
                range.Merge = true;
                range.Value = "Wartość brutto";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            //Table body

            int row = 19;

            //Grouped measure units
            List<MeasureUnitGrouped> units = new();

            decimal totalNetPrice = 0m;
            decimal totalVatAmount = 0m;
            decimal totalGrossPrice = 0m;
            decimal vatPercent = 0m;

            int index = 1;

            if (isFromSale)
                foreach (Sale sale in sadPl.Sales)
                foreach (OrderItem item in sale.Order.OrderItems) {
                    decimal currentNetPrice =
                        decimal.Round(
                            item.PricePerItem * Convert.ToDecimal(item.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentVatAmount =
                        decimal.Round(
                            currentNetPrice * vatPercent / 100,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentGrossPrice =
                        decimal.Round(
                            currentNetPrice + currentVatAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    totalNetPrice =
                        decimal.Round(totalNetPrice + currentNetPrice, 2, MidpointRounding.AwayFromZero);

                    totalVatAmount =
                        decimal.Round(totalVatAmount + currentVatAmount, 2, MidpointRounding.AwayFromZero);

                    totalGrossPrice =
                        decimal.Round(totalGrossPrice + currentGrossPrice, 2, MidpointRounding.AwayFromZero);

                    if (units.Any(u => u.Name.Equals(item.Product.MeasureUnit.Name)))
                        units.First(u => u.Name.Equals(item.Product.MeasureUnit.Name)).Qty += item.Qty;
                    else
                        units.Add(new MeasureUnitGrouped {
                            Name = item.Product.MeasureUnit.Name,
                            Qty = item.Qty
                        });

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Merge = true;
                        range.Value = index;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    string fullName =
                        string.Format(
                            "{0} {1}",
                            item.Product.VendorCode,
                            item.Product.NamePL
                        );

                    using (ExcelRange range = worksheet.Cells[row, 3, row, 17]) {
                        range.Merge = true;
                        range.Value = fullName;
                        range.Style.WrapText = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 18, row, 21]) {
                        range.Merge = true;
                        range.Value = item.Product.MeasureUnit.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 22, row, 26]) {
                        range.Merge = true;
                        range.Value = item.Qty;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                        range.Merge = true;
                        range.Value = item.PricePerItem;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                        range.Merge = true;
                        range.Value = currentNetPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                        range.Merge = true;
                        range.Value = $"{vatPercent}%";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                        range.Merge = true;
                        range.Value = currentVatAmount;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                        range.Merge = true;
                        range.Value = currentGrossPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    worksheet.SetRowHeight(
                        fullName.Length > 45
                            ? 27
                            : 12.80,
                        row
                    );

                    row++;

                    index++;
                }
            else
                foreach (SadItem item in sadPl.SadItems) {
                    decimal currentNetPrice =
                        decimal.Round(
                            item.SupplyOrderUkraineCartItem.UnitPrice * Convert.ToDecimal(item.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentVatAmount =
                        decimal.Round(
                            currentNetPrice * vatPercent / 100,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentGrossPrice =
                        decimal.Round(
                            currentNetPrice + currentVatAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    totalNetPrice =
                        decimal.Round(totalNetPrice + currentNetPrice, 2, MidpointRounding.AwayFromZero);

                    totalVatAmount =
                        decimal.Round(totalVatAmount + currentVatAmount, 2, MidpointRounding.AwayFromZero);

                    totalGrossPrice =
                        decimal.Round(totalGrossPrice + currentGrossPrice, 2, MidpointRounding.AwayFromZero);

                    if (units.Any(u => u.Name.Equals(item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name)))
                        units.First(u => u.Name.Equals(item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name)).Qty += item.Qty;
                    else
                        units.Add(new MeasureUnitGrouped {
                            Name = item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name,
                            Qty = item.Qty
                        });

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Merge = true;
                        range.Value = index;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    string fullName =
                        string.Format(
                            "{0} {1}",
                            item.SupplyOrderUkraineCartItem.Product.VendorCode,
                            item.SupplyOrderUkraineCartItem.Product.NamePL
                        );

                    using (ExcelRange range = worksheet.Cells[row, 3, row, 17]) {
                        range.Merge = true;
                        range.Value = fullName;
                        range.Style.WrapText = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 18, row, 21]) {
                        range.Merge = true;
                        range.Value = item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 22, row, 26]) {
                        range.Merge = true;
                        range.Value = item.Qty;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                        range.Merge = true;
                        range.Value = item.SupplyOrderUkraineCartItem.UnitPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                        range.Merge = true;
                        range.Value = currentNetPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                        range.Merge = true;
                        range.Value = $"{vatPercent}%";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                        range.Merge = true;
                        range.Value = currentVatAmount;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                        range.Merge = true;
                        range.Value = currentGrossPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    worksheet.SetRowHeight(
                        fullName.Length > 45
                            ? 27
                            : 12.80,
                        row
                    );

                    row++;

                    index++;
                }

            using (ExcelRange range = worksheet.Cells[row, 2, row + 1, 26]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                range.Merge = true;
                range.Value = "Razem";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                range.Merge = true;
                range.Value = totalNetPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                range.Merge = true;
                range.Value = "Х";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmount;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                range.Merge = true;
                range.Value = "W tym VAT";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                range.Merge = true;
                range.Value = totalNetPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                range.Merge = true;
                range.Value = $"{vatPercent}%";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmount;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(10, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Słownie:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "{0} euro {1} centy",
                        totalGrossPrice.ToText(true, false),
                        (decimal.Round(totalGrossPrice % 1, 2) * 100m).ToText(false, false)
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Razem do zapłaty:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 21]) {
                range.Merge = true;
                range.Value = string.Format("{0}", totalGrossPrice);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 22, row, 27]) {
                range.Merge = true;
                range.Value = "Data sprzedaży:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 39]) {
                range.Merge = true;
                range.Value = DateTime.Now.ToString("dd.MM.yyyy");
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Sposób zapłaty:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = "gotówka";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Podsumowania ilości dla jednostek:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            string grouped = units.Aggregate(string.Empty, (current, unit) => current + $"{unit.Qty} {unit.Name} ");

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = grouped;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Umowy i miejsce dostawy:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Ilość miejsc:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Waga netto:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = $"{sadPl.TotalNetWeight} kg";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Waga brutto:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = $"{sadPl.TotalGrossWeight} kg";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Numer samochodu:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Panstwo pochodzenia towaru:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = "Sprzedaż w eksporcie. Towary objęte są 0% stawką VAT.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 39]) {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(8, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 4, row, 11]) {
                range.Merge = true;
                range.Value = "otrzymujący fakturę";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 25, row, 31]) {
                range.Merge = true;
                range.Value = "wystawiający fakturę";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 36]) {
                range.Merge = true;
                range.Value = userFullName;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 11]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 25, row, 36]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(48.4, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 11]) {
                range.Merge = true;
                range.Value = "podpis";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 25, row, 36]) {
                range.Merge = true;
                range.Value = "podpis";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(10.01, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 39]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row - 4, 1, row, 1]) {
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row - 4, 40, row, 40]) {
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(10.01, row);

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Arial";
            }

            worksheet = package.Workbook.Worksheets.Add("Ukrainian");

            worksheet.PrinterSettings.TopMargin = 0.3;
            worksheet.PrinterSettings.BottomMargin = 0.3;
            worksheet.PrinterSettings.RightMargin = 0.1;
            worksheet.PrinterSettings.LeftMargin = 0.1;
            worksheet.PrinterSettings.HeaderMargin = 0.3;
            worksheet.PrinterSettings.FooterMargin = 0.3;
            worksheet.PrinterSettings.FitToPage = true;

            //Setting default width to columns
            worksheet.SetColumnWidth(1.11, 1);
            worksheet.SetColumnWidth(6.07, 2);
            worksheet.SetColumnWidth(7.61, 3);
            worksheet.SetColumnWidth(8.11, 4);
            worksheet.SetColumnWidth(7.71, 5);
            worksheet.SetColumnWidth(2.77, 6);
            worksheet.SetColumnWidth(3.77, 7);
            worksheet.SetColumnWidth(3.23, 8);
            worksheet.SetColumnWidth(0.00, 9);
            worksheet.SetColumnWidth(0.00, 10);
            worksheet.SetColumnWidth(1.68, 11);
            worksheet.SetColumnWidth(1.75, 12);
            worksheet.SetColumnWidth(2.11, 13);
            worksheet.SetColumnWidth(0.75, 14);
            worksheet.SetColumnWidth(1.61, 15);
            worksheet.SetColumnWidth(0.75, 16);
            worksheet.SetColumnWidth(1.22, 17);
            worksheet.SetColumnWidth(5.11, 18);
            worksheet.SetColumnWidth(0.00, 19);
            worksheet.SetColumnWidth(0.00, 20);
            worksheet.SetColumnWidth(0.11, 21);
            worksheet.SetColumnWidth(0.00, 22);
            worksheet.SetColumnWidth(3.31, 23);
            worksheet.SetColumnWidth(1.88, 24);
            worksheet.SetColumnWidth(0.11, 25);
            worksheet.SetColumnWidth(1.52, 26);
            worksheet.SetColumnWidth(6.93, 27);
            worksheet.SetColumnWidth(0.00, 28);
            worksheet.SetColumnWidth(1.45, 29);
            worksheet.SetColumnWidth(3.77, 30);
            worksheet.SetColumnWidth(2.88, 31);
            worksheet.SetColumnWidth(2.56, 32);
            worksheet.SetColumnWidth(3.88, 33);
            worksheet.SetColumnWidth(2.56, 34);
            worksheet.SetColumnWidth(3.00, 35);
            worksheet.SetColumnWidth(2.56, 36);
            worksheet.SetColumnWidth(2.56, 37);
            worksheet.SetColumnWidth(2.56, 38);
            worksheet.SetColumnWidth(2.56, 39);

            //Document header

            worksheet.SetRowHeight(16, 1);
            worksheet.SetRowHeight(12.80, new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 16 });
            worksheet.SetRowHeight(13.11, new[] { 17, 18 });
            worksheet.SetRowHeight(4, 15);

            using (ExcelRange range = worksheet.Cells[1, 2, 1, 21]) {
                range.Merge = true;
                range.Value = "Оригінал / Копія";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[1, 22, 1, 29]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 12;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[1, 30, 1, 39]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "Перемишль, {0}",
                        sadUk.FromDate.ToString("dd.MM.yyyy")
                    );
                //"Перемишль, 24.10.2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[2, 2, 5, 39]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "Рахунок - фактура NR EX{0}/{1}",
                        string.Format(
                            "{0:D3}",
                            sadUk.Number.StartsWith("EX")
                                ? Convert.ToInt64(sadUk.Number.Substring(2, 10))
                                : Convert.ToInt64(sadUk.Number)
                        ),
                        sadUk.FromDate.ToString("MM/yyyy")
                    );
                //"Рахунок - фактура NR EX0036/10/2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 14;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[6, 2, 6, 39]) {
                range.Merge = true;
                if (sadUk.OrganizationClientAgreement != null)
                    range.Value =
                        string.Format(
                            "Договір:    {0}/{1} z {2}",
                            string.Format("{0:D3}", Convert.ToInt64(sadUk.OrganizationClientAgreement.Number)),
                            sadUk.OrganizationClientAgreement.FromDate.ToString("MM/yyyy"),
                            sadUk.OrganizationClientAgreement.FromDate.ToString("dd.MM.yyyy")
                        );
                else
                    range.Value =
                        string.Format(
                            "Договір:    {0}/{1} z {2}",
                            string.Format("{0:D3}", 0),
                            sadUk.FromDate.ToString("MM/yyyy"),
                            sadUk.FromDate.ToString("dd.MM.yyyy")
                        );

                //"Договір:    001/10/18 z 16.10.2018";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[7, 2, 7, 21]) {
                range.Merge = true;
                range.Value = "Продавець:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[7, 22, 7, 39]) {
                range.Merge = true;
                range.Value = "Покупець:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 2, 15, 21]) {
                range.Merge = true;
                range.Value =
                    "CONCORD.PL Sp z o.o.,\r37-700, Przemyśl, ul. Gen.Jakuba Jasińskiego 58,\rNIP 8133680920,\rBank BPS S.A.\rIBAN: PL55193013182740072619290003\rSWIFT : POLUPLPR\rBRANCH CODE : 1930 1318";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            completeRecipient = string.Empty;

            if (isFromSale) {
                if (sadUk.Sales.Any()) {
                    Client client = sadUk.Sales.First().ClientAgreement.Client;

                    if (!string.IsNullOrEmpty(client.FirstName) && !string.IsNullOrEmpty(client.LastName))
                        completeRecipient = $"{client.LastName} {client.FirstName} {client.MiddleName} ";
                    else
                        completeRecipient += client.FullName;

                    completeRecipient +=
                        string.IsNullOrEmpty(client.ActualAddress)
                            ? string.Empty
                            : client.ActualAddress + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.City)
                            ? string.Empty
                            : client.RegionCode.City + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.RegionCode.District)
                            ? string.Empty
                            : client.RegionCode.District + ",  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(client.TIN)
                            ? string.Empty
                            : "\rNIP " + client.TIN;
                }
            } else {
                //if (sadPl.OrganizationClient != null) {
                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.FullName)
                //            ? string.Empty
                //            : sadPl.OrganizationClient.FullName + "  ";

                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.Address)
                //            ? string.Empty
                //            : sadPl.OrganizationClient.Address + ", ";

                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.City)
                //            ? string.Empty
                //            : sadPl.OrganizationClient.City + ", ";

                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.Country)
                //            ? string.Empty
                //            : sadPl.OrganizationClient.Country + ", ";

                //    completeRecipient +=
                //        string.IsNullOrEmpty(sadPl.OrganizationClient.NIP)
                //            ? string.Empty
                //            : "\rNIP " + sadPl.OrganizationClient.NIP;
                //}
                if (sadUk.Statham != null) {
                    completeRecipient +=
                        string.IsNullOrEmpty(sadUk.Statham.LastName)
                            ? string.Empty
                            : sadUk.Statham.LastName + "  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadUk.Statham.FirstName)
                            ? string.Empty
                            : sadUk.Statham.FirstName + "  ";

                    completeRecipient +=
                        string.IsNullOrEmpty(sadUk.Statham.MiddleName)
                            ? string.Empty
                            : sadUk.Statham.MiddleName + "  ";

                    if (sadUk.StathamPassport != null) {
                        completeRecipient +=
                            string.IsNullOrEmpty(sadUk.StathamPassport.City)
                                ? string.Empty
                                : sadUk.StathamPassport.City + "  ";

                        completeRecipient +=
                            string.IsNullOrEmpty(sadUk.StathamPassport.Street)
                                ? string.Empty
                                : sadUk.StathamPassport.Street + "  ";

                        completeRecipient +=
                            string.IsNullOrEmpty(sadUk.StathamPassport.PassportSeria)
                                ? string.Empty
                                : sadUk.StathamPassport.PassportSeria + "  ";

                        completeRecipient +=
                            string.IsNullOrEmpty(sadUk.StathamPassport.PassportNumber)
                                ? string.Empty
                                : sadUk.StathamPassport.PassportNumber + "  ";
                    }
                }
            }

            using (ExcelRange range = worksheet.Cells[8, 22, 15, 39]) {
                range.Merge = true;
                range.Value = completeRecipient;
                //"Prywatny przedsiębiorca Bereshvili Vadym Viktorovych  st.Chornovola,176/7, Chmielnicky, UKRAINA\rNIP 3100401117,";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                range.Style.Font.Size = 10;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            //Document body

            //Table header

            using (ExcelRange range = worksheet.Cells[16, 2, 18, 2]) {
                range.Merge = true;
                range.Value = "№ п/п";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 3, 18, 17]) {
                range.Merge = true;
                range.Value = "Назва товару або послуги";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 18, 18, 21]) {
                range.Merge = true;
                range.Value = "Од. виміру";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 22, 18, 26]) {
                range.Merge = true;
                range.Value = "к-сть товару/послуги";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 27, 18, 29]) {
                range.Merge = true;
                range.Value = "Ціна за одиницю нетто";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 30, 18, 32]) {
                range.Merge = true;
                range.Value = "Сума нетто";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 33, 16, 36]) {
                range.Merge = true;
                range.Value = "Податок";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 33, 18, 34]) {
                range.Merge = true;
                range.Value = "Ставка %";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[17, 35, 18, 36]) {
                range.Merge = true;
                range.Value = "Сума";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[16, 37, 18, 39]) {
                range.Merge = true;
                range.Value = "Сума брутто Євро";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Font.Bold = true;
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            //Table body

            row = 19;

            //Grouped measure units
            units = new List<MeasureUnitGrouped>();

            totalNetPrice = 0m;
            totalVatAmount = 0m;
            totalGrossPrice = 0m;

            if (isFromSale)
                foreach (Sale sale in sadUk.Sales)
                foreach (OrderItem item in sale.Order.OrderItems) {
                    decimal currentNetPrice =
                        decimal.Round(
                            item.PricePerItem * Convert.ToDecimal(item.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentVatAmount =
                        decimal.Round(
                            currentNetPrice * vatPercent / 100,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentGrossPrice =
                        decimal.Round(
                            currentNetPrice + currentVatAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    totalNetPrice =
                        decimal.Round(totalNetPrice + currentNetPrice, 2, MidpointRounding.AwayFromZero);

                    totalVatAmount =
                        decimal.Round(totalVatAmount + currentVatAmount, 2, MidpointRounding.AwayFromZero);

                    totalGrossPrice =
                        decimal.Round(totalGrossPrice + currentGrossPrice, 2, MidpointRounding.AwayFromZero);

                    if (units.Any(u => u.Name.Equals(item.Product.MeasureUnit.Name)))
                        units.First(u => u.Name.Equals(item.Product.MeasureUnit.Name)).Qty += item.Qty;
                    else
                        units.Add(new MeasureUnitGrouped {
                            Name = item.Product.MeasureUnit.Name,
                            Qty = item.Qty
                        });

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Merge = true;
                        range.Value = index;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    string fullName =
                        string.Format(
                            "{0} {1}",
                            item.Product.VendorCode,
                            item.Product.NameUA
                        );

                    using (ExcelRange range = worksheet.Cells[row, 3, row, 17]) {
                        range.Merge = true;
                        range.Value = fullName;
                        range.Style.WrapText = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 18, row, 21]) {
                        range.Merge = true;
                        range.Value = item.Product.MeasureUnit.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 22, row, 26]) {
                        range.Merge = true;
                        range.Value = item.Qty;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                        range.Merge = true;
                        range.Value = item.PricePerItem;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                        range.Merge = true;
                        range.Value = currentNetPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                        range.Merge = true;
                        range.Value = $"{vatPercent}%";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                        range.Merge = true;
                        range.Value = currentVatAmount;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                        range.Merge = true;
                        range.Value = currentGrossPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    worksheet.SetRowHeight(
                        fullName.Length > 45
                            ? 27
                            : 12.80,
                        row
                    );

                    row++;

                    index++;
                }
            else
                foreach (SadItem item in sadUk.SadItems) {
                    decimal currentNetPrice =
                        decimal.Round(
                            item.SupplyOrderUkraineCartItem.UnitPrice * Convert.ToDecimal(item.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentVatAmount =
                        decimal.Round(
                            currentNetPrice * vatPercent / 100,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal currentGrossPrice =
                        decimal.Round(
                            currentNetPrice + currentVatAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    totalNetPrice =
                        decimal.Round(totalNetPrice + currentNetPrice, 2, MidpointRounding.AwayFromZero);

                    totalVatAmount =
                        decimal.Round(totalVatAmount + currentVatAmount, 2, MidpointRounding.AwayFromZero);

                    totalGrossPrice =
                        decimal.Round(totalGrossPrice + currentGrossPrice, 2, MidpointRounding.AwayFromZero);

                    if (units.Any(u => u.Name.Equals(item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name)))
                        units.First(u => u.Name.Equals(item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name)).Qty += item.Qty;
                    else
                        units.Add(new MeasureUnitGrouped {
                            Name = item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name,
                            Qty = item.Qty
                        });

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Merge = true;
                        range.Value = index;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    string fullName =
                        string.Format(
                            "{0} {1}",
                            item.SupplyOrderUkraineCartItem.Product.VendorCode,
                            item.SupplyOrderUkraineCartItem.Product.NameUA
                        );

                    using (ExcelRange range = worksheet.Cells[row, 3, row, 17]) {
                        range.Merge = true;
                        range.Value = fullName;
                        range.Style.WrapText = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 18, row, 21]) {
                        range.Merge = true;
                        range.Value = item.SupplyOrderUkraineCartItem.Product.MeasureUnit.Name;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 22, row, 26]) {
                        range.Merge = true;
                        range.Value = item.Qty;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                        range.Merge = true;
                        range.Value = item.SupplyOrderUkraineCartItem.UnitPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                        range.Merge = true;
                        range.Value = currentNetPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                        range.Merge = true;
                        range.Value = $"{vatPercent}%";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                        range.Merge = true;
                        range.Value = currentVatAmount;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                        range.Merge = true;
                        range.Value = currentGrossPrice;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 9;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Numberformat.Format = "0.00";
                    }

                    worksheet.SetRowHeight(
                        fullName.Length > 45
                            ? 27
                            : 12.80,
                        row
                    );

                    row++;

                    index++;
                }

            using (ExcelRange range = worksheet.Cells[row, 2, row + 1, 26]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                range.Merge = true;
                range.Value = "Разом";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                range.Merge = true;
                range.Value = totalNetPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                range.Merge = true;
                range.Value = "Х";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmount;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 27, row, 29]) {
                range.Merge = true;
                range.Value = "В тому числі ПДВ";
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 30, row, 32]) {
                range.Merge = true;
                range.Value = totalNetPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 33, row, 34]) {
                range.Merge = true;
                range.Value = $"{vatPercent}%";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 35, row, 36]) {
                range.Merge = true;
                range.Value = totalVatAmount;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            using (ExcelRange range = worksheet.Cells[row, 37, row, 39]) {
                range.Merge = true;
                range.Value = totalGrossPrice;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Numberformat.Format = "0.00";
            }

            worksheet.SetRowHeight(22, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(10, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Словами:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            decimal cents = decimal.Round(totalGrossPrice % 1, 2) * 100m;

            int fullNumber = Convert.ToInt32(cents);

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

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value =
                    string.Format(
                        "{0} євро {1} {2}",
                        totalGrossPrice.ToText(true, true),
                        cents.ToText(false, true),
                        endKeyWord
                    );
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Разом до оплати:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 21]) {
                range.Merge = true;
                range.Value = string.Format("{0} Євро", totalGrossPrice);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 22, row, 27]) {
                range.Merge = true;
                range.Value = "Дата продажу:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 28, row, 39]) {
                range.Merge = true;
                range.Value = DateTime.Now.ToString("dd.MM.yyyy");
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Спосіб оплати:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = "готівка";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Підсумовування кількості для одиниць";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            grouped = units.Aggregate(string.Empty, (current, unit) => current + $"{unit.Qty} {unit.Name} ");

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = grouped;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Умови та місце поставки:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Кількість місць:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Вага нетто:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = $"{sadUk.TotalNetWeight} кг";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Вага брутто:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = $"{sadUk.TotalGrossWeight} кг";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Номер автомобіля:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = "Країна походження товару:";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 8]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 39]) {
                range.Merge = true;
                range.Value = "Продажа в експорті. Товар підлягає 0% ставці ПДВ.";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 9;
                range.Style.Font.Bold = true;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 39]) {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(8, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 4, row, 11]) {
                range.Merge = true;
                range.Value = "отримав рахунок-фактуру";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 23, row, 31]) {
                range.Merge = true;
                range.Value = "виставив рахунок-фактуру";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 32, row, 36]) {
                range.Merge = true;
                range.Value = userFullName;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(12.80, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 11]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 23, row, 36]) {
                range.Merge = true;
                range.Value = string.Empty;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(38.4, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 3, row, 11]) {
                range.Merge = true;
                range.Value = "підпис";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row, 23, row, 36]) {
                range.Merge = true;
                range.Value = "підпис";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Size = 8;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.SetRowHeight(10.01, row);

            row++;

            using (ExcelRange range = worksheet.Cells[row, 2, row, 39]) {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row - 4, 1, row, 1]) {
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row - 4, 40, row, 40]) {
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            worksheet.SetRowHeight(10.01, row);

            //Setting default font options
            using (ExcelRange range = worksheet.Cells[1, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column]) {
                range.Style.Font.Name = "Arial";
            }

            package.Workbook.Properties.Title = "Sad Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            //Saving the file.
            package.Save();
        }

        return SaveFiles(fileName);
    }
}