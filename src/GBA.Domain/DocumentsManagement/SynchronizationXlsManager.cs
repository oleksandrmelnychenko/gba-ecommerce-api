using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public sealed class SynchronizationXlsManager : BaseXlsManager, ISynchronizationXlsManager {
    private readonly Regex _literals = new(@"[A-zА-я]+", RegexOptions.Compiled);

    public (string xlsxFile, string pdfFile) ExportUkClientsToXlsx(string path, List<Client> clients, IEnumerable<DocumentMonth> months) {
        string fileName = Path.Combine(path, $"supplyOrganizations_{DateTime.Now.ToString("MM.yyyy")}_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("HistoryProduct Document");

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
                range.Value = "Код по регіону";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 3, 2, 3]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Повне ім'я";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 2, 4]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Телефон";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 5, 2, 5]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Поточний баланс";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 6, 2, 6]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Емаіл";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 7, 2, 7]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Не являється резедентом";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }


            int row = 3;
            int count = 1;
            foreach (Client client in clients) {
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
                    if (client.RegionCode != null)
                        range.Value = client.RegionCode.Value;
                    else
                        range.Value = "";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = client.FullName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = client.MobileNumber;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = client.TotalCurrentAmount;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = client.EmailAddress;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    if (client.IsNotResident)
                        range.Value = "+";
                    else
                        range.Value = "-";

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

    public (string xlsxFile, string pdfFile) ExportUkSupplyOrganizationToXlsx(string path, List<SupplyOrganization> supplyOrganizations, IEnumerable<DocumentMonth> months) {
        string fileName = Path.Combine(path, $"supplyOrganizations_{DateTime.Now.ToString("MM.yyyy")}_{Guid.NewGuid().ToString()}.xlsx");

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("HistoryProduct Document");

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
                range.Value = "Назва";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 3, 2, 3]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Організація";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 2, 4]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Валюта";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 5, 2, 5]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Баланс";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 6, 2, 6]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Контактна особа";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 7, 2, 7]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Телефон";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 8, 2, 8]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Адреса";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 9, 2, 9]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Не являється рицизентом";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 10, 2, 10]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Банківські реквізити";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            int row = 3;
            int count = 1;
            foreach (SupplyOrganization supplyOrganization in supplyOrganizations) {
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
                    range.Value = supplyOrganization.Name;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = string.Join(" ",
                        supplyOrganization.SupplyOrganizationAgreements
                            .Where(a => a.Organization != null)
                            .Select(a => a.Organization.Name));
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = string.Join(" ",
                        supplyOrganization.SupplyOrganizationAgreements
                            .Where(a => a.Organization != null)
                            .Select(a => a.Currency.Code));
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = supplyOrganization.TotalAgreementsCurrentEuroAmount;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = supplyOrganization.ContactPersonName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = supplyOrganization.PhoneNumber;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = supplyOrganization.Address;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    if (supplyOrganization.IsNotResident)
                        range.Value = "+";
                    else
                        range.Value = "-";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = supplyOrganization.Requisites;
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
}