using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers.DebtorModels;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public sealed class ClientXlsManager : BaseXlsManager, IClientXlsManager {
    public (string xlsxFile, string pdfFile) ExportClientInDebtToXlsx(string path, ClientDebtorsModel clientInDebtors) {
        string fileName = Path.Combine(path, $"{"ClientInDebt"}_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        bool isValidRetrieveData = clientInDebtors != null;

        if (!isValidRetrieveData) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("ClientInDebt Document");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            worksheet.SetColumnWidth(1.4286, 1);
            worksheet.SetColumnWidth(14.7143, 2);
            worksheet.SetColumnWidth(50, 3);
            worksheet.SetColumnWidth(23.4286, 4);
            worksheet.SetColumnWidth(14.7143, 5);
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
                range.Value = "Код регіону";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 3, 2, 3]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Контрагент";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 4, 2, 4]) {
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Основний менеджер покупця";
                range.Style.WrapText = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 5, 2, 5]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Прострочено днів";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 6, 2, 6]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Залишок боргу";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 7, 2, 7]) {
                range.Merge = true;
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Прострочений борг";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[1, 2, 2, 7]) {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(249, 242, 222));
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Font.Name = "Arial";
            }

            int row = 3;

            foreach (ClientInDebtModel clientInDebt in clientInDebtors.ClientInDebtors) {
                worksheet.SetRowHeight(10.7, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = clientInDebt.RegionCode;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 3]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = clientInDebt.ClientName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }


                using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = clientInDebt.UserName;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";

                    if (clientInDebt.MissedDays < 0) range.Style.Font.Color.SetColor(Color.Red);

                    range.Value = clientInDebt.MissedDays;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = clientInDebt.RemainderDebt;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Font.Name = "Arial";
                    range.Value = clientInDebt.OverdueDebt;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                row++;
            }

            worksheet.SetRowHeight(11.3636, row);

            using (ExcelRange range = worksheet.Cells[row, 4, row, 4]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = "Разом:";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = clientInDebtors.TotalMissedDays;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = clientInDebtors.TotalRemainderDebtorsValue;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                range.Style.WrapText = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Value = clientInDebtors.TotalOverdueDebtorsValue;
                range.Style.Numberformat.Format = "0.00";
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
            }

            using (ExcelRange range = worksheet.Cells[row, 4, row, 7]) {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(249, 242, 222));
                range.Style.Font.Size = 10;
                range.Style.Font.Bold = true;
                range.Style.Font.Name = "Arial";
            }

            package.Workbook.Properties.Title = "Client In Debt Document";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }

    public (string xlsxFile, string pdfFile) ExportAllClientsToXls(string path, List<Client> clients) {
        try {
            string fileName = Path.Combine(path, $"{"Clients"}_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

            using (ExcelPackage package = NewExcelPackage(fileName)) {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Clients Document");

                worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

                worksheet.SetColumnWidth(1.4286, 1);
                worksheet.SetColumnWidth(14.7143, 2);
                worksheet.SetColumnWidth(50, 3);
                worksheet.SetColumnWidth(23.4286, 4);
                worksheet.SetColumnWidth(14.7143, 5);
                worksheet.SetColumnWidth(14.7143, 6);
                worksheet.SetColumnWidth(16, 7);
                worksheet.SetColumnWidth(14.7143, 8);
                worksheet.SetColumnWidth(14.7143, 9);
                worksheet.SetColumnWidth(14.7143, 10);
                worksheet.SetColumnWidth(14.7143, 11);
                worksheet.SetColumnWidth(14.7143, 12);

                worksheet.SetRowHeight(21.2121, 1);
                worksheet.SetRowHeight(22, 2);

                using (ExcelRange range = worksheet.Cells[1, 3, 1, 3]) {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[1, 2, 2, 2]) {
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Value = "Код регіону";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[1, 3, 2, 3]) {
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Value = "Контрагент";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[1, 4, 2, 4]) {
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Value = "ЄДРПУО";
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[1, 5, 2, 5]) {
                    range.Merge = true;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Value = "ІПН";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[1, 6, 2, 6]) {
                    range.Merge = true;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Value = "Номер свідоцтва платника НДС";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[1, 7, 2, 7]) {
                    range.Merge = true;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Value = "Дні ";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[1, 8, 2, 8]) {
                    range.Merge = true;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Value = "Місто ";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[1, 9, 2, 9]) {
                    range.Merge = true;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Value = "Район ";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[1, 10, 2, 10]) {
                    range.Merge = true;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Value = "Телефон ";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[1, 11, 2, 11]) {
                    range.Merge = true;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Value = "Емейл ";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[1, 12, 2, 12]) {
                    range.Merge = true;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Value = "Роль ";
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                }

                using (ExcelRange range = worksheet.Cells[1, 2, 2, 12]) {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(249, 242, 222));
                    range.Style.Font.Size = 10;
                    range.Style.Font.Bold = true;
                    range.Style.Font.Name = "Arial";
                }

                int row = 3;

                foreach (Client client in clients) {
                    worksheet.SetRowHeight(10.7, row);

                    using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = client.RegionCode?.Value ?? "";
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
                        range.Value = client.USREOU;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 5, row, 5]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = client.TIN;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 6, row, 6]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = client.SROI;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 7, row, 7]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = client.OrderExpireDays;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 8, row, 8]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = client.RegionCode?.City ?? string.Empty;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = client.RegionCode?.District ?? string.Empty;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 10, row, 10]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = client.ClientNumber;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 11, row, 11]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";
                        range.Value = client.EmailAddress;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    }

                    using (ExcelRange range = worksheet.Cells[row, 12, row, 12]) {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 8;
                        range.Style.Font.Name = "Arial";

                        string roleName = string.Empty;

                        if (client.ClientInRole?.ClientTypeRole != null)
                            roleName = client.ClientInRole.ClientTypeRole.ClientTypeRoleTranslations.Any()
                                ? client.ClientInRole.ClientTypeRole.ClientTypeRoleTranslations.First().Name
                                : client.ClientInRole.ClientTypeRole.Name;

                        range.Value = roleName;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(180, 168, 144));
                    }

                    row++;
                }

                package.Workbook.Properties.Title = "Clients Document";
                package.Workbook.Properties.Author = "Concord CRM";
                package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

                package.Save();
            }

            return SaveFiles(fileName);
        } catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }
}