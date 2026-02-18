using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.EntityHelpers.Accounting;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public sealed class AccountingXlsManager : BaseXlsManager, IAccountingXlsManager {
    public (string xlsxFile, string pdfFile) ExportAccountingCashFlowToXlsx(string path, AccountingCashFlow accountingCashFlow, User user, DateTime to) {
        bool isValidRetrieveData = accountingCashFlow?.ClientAgreement != null || accountingCashFlow?.Client != null ||
                                   accountingCashFlow?.SupplyOrganization != null || accountingCashFlow?.SupplyOrganizationAgreement != null;

        string currencyCode = string.Empty;

        string clientName = string.Empty;

        DateTime dateContract = new();

        string numberContract = string.Empty;

        string concordName = string.Empty;

        if (accountingCashFlow?.Client != null) {
            clientName = accountingCashFlow.Client.FullName;
        } else if (accountingCashFlow?.SupplyOrganization != null) {
            clientName = accountingCashFlow.SupplyOrganization.Name;
            concordName = accountingCashFlow.SupplyOrganizationAgreement?.Organization?.Name;
        }

        if (accountingCashFlow?.ClientAgreement != null) {
            numberContract = accountingCashFlow.ClientAgreement.Agreement.Number;
            dateContract = accountingCashFlow.ClientAgreement.Created;
            if (accountingCashFlow.ClientAgreement.Agreement?.Organization != null) {
                concordName = accountingCashFlow.ClientAgreement.Agreement.Organization.Name;
                currencyCode = accountingCashFlow.ClientAgreement.Agreement.Currency?.Code ?? "";
            }
        } else if (accountingCashFlow?.SupplyOrganizationAgreement != null) {
            dateContract = accountingCashFlow.SupplyOrganizationAgreement.Created;
            numberContract = accountingCashFlow.SupplyOrganizationAgreement.Name;
            currencyCode = accountingCashFlow.SupplyOrganizationAgreement.Currency?.Code ?? "";
            concordName = accountingCashFlow.SupplyOrganizationAgreement?.Organization?.FullName ?? "";
        }

        string currencyName = currencyCode == "UAH" ? "грн" : "євро";

        string responsibleName = string.Empty;

        string directorShortName = string.Empty;

        if (!string.IsNullOrEmpty(user.LastName)) {
            responsibleName += user.LastName + " ";
            directorShortName += user.LastName + " ";
        }

        if (!string.IsNullOrEmpty(user.FirstName)) {
            responsibleName += user.FirstName + " ";
            directorShortName += user.FirstName.FirstOrDefault() + ".";
        }

        if (!string.IsNullOrEmpty(user.MiddleName)) {
            responsibleName += user.MiddleName;
            directorShortName += user.MiddleName.FirstOrDefault() + ".";
        }

        decimal initialBalanceValue = accountingCashFlow?.BeforeRangeBalance ?? 0m;

        string fileName = Path.Combine(path, $"Accounting_{Guid.NewGuid()}_{DateTime.Now:MM.yyyy}.xlsx");

        if (!isValidRetrieveData) return SaveFiles(fileName);

        using (ExcelPackage package = NewExcelPackage(fileName)) {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("AccountingCashFlow Document");

            worksheet.ApplyPrinterSettings(0.3m, 0.3m, 0.1m, 0.1m, 0.3m, 0.3m, true);

            //Setting default width to columns
            worksheet.SetColumnWidth(2, 1);
            worksheet.SetColumnWidth(8.86, 2);
            worksheet.SetColumnWidth(26.57, 3);
            worksheet.SetColumnWidth(2.86, 4);
            worksheet.SetColumnWidth(9, 5);
            worksheet.SetColumnWidth(2.715, 6);
            worksheet.SetColumnWidth(8.71, 7);
            worksheet.SetColumnWidth(3, 8);
            worksheet.SetColumnWidth(1.15, 9);
            worksheet.SetColumnWidth(8.29, 10);
            worksheet.SetColumnWidth(26.57, 11);
            worksheet.SetColumnWidth(2.71, 12);
            worksheet.SetColumnWidth(9, 13);
            worksheet.SetColumnWidth(2.71, 14);
            worksheet.SetColumnWidth(9, 15);
            worksheet.SetColumnWidth(2.715, 16);

            //Document header

            //Setting document header height
            worksheet.SetRowHeight(12.12, 1);
            worksheet.SetRowHeight(18.94, 2);
            worksheet.SetRowHeight(49.24, 3);
            worksheet.SetRowHeight(18.18, 4);
            worksheet.SetRowHeight(37.88, 5);
            worksheet.SetRowHeight(6.06, 6);
            worksheet.SetRowHeight(25, 7);
            worksheet.SetRowHeight(11.36, 8);
            worksheet.SetRowHeight(11.36, 9);

            using (ExcelRange range = worksheet.Cells[2, 2, 2, 16]) {
                range.ApplyStyledValue("Акт звірки взаєморозрахунків", 14, fontName: "Tahoma", true);
            }

            using (ExcelRange range = worksheet.Cells[3, 2, 3, 16]) {
                string value =
                    string.IsNullOrEmpty(concordName)
                        ? string.Format(
                            "взаємних розрахунків станом на період: {0} р. \nіз {1}",
                            to.ToString("MMMM yyyy", new CultureInfo("uk-UA")).Substring(0, 1).ToUpper() +
                            to.ToString("MMMM yyyy", new CultureInfo("uk-UA")).Substring(1),
                            clientName
                        )
                        : string.Format(
                            "взаємних розрахунків станом на період: {0} р. \nміж {1} \nі {2} \nза договором № {3} від {4}",
                            to.ToString("MMMM yyyy", new CultureInfo("uk-UA")).Substring(0, 1).ToUpper() +
                            to.ToString("MMMM yyyy", new CultureInfo("uk-UA")).Substring(1),
                            concordName, clientName, numberContract, dateContract.ToString("dd.MM.yy")
                        );

                range.ApplyStyledValue(value, 10);
            }

            //Document body

            using (ExcelRange range = worksheet.Cells[5, 2, 5, 16]) {
                string value = string.Format("Ми, що нижче підписалися, {0} " +
                                             "{1}, з одного боку, і ________________ {2} _______________________",
                    concordName,
                    responsibleName,
                    clientName
                );

                range.ApplyStyledValue(value, 10, verticalAlignment: ExcelVerticalAlignment.Bottom, horizontalAlignment: ExcelHorizontalAlignment.General);
            }

            //Document table header

            using (ExcelRange range = worksheet.Cells[7, 2, 7, 7]) {
                string value = !string.IsNullOrEmpty(concordName) ? string.Format("За даними {0}, {1}", concordName, currencyName) : string.Empty;

                range.ApplyStyledValue(value, 10, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Top);
            }

            using (ExcelRange range = worksheet.Cells[7, 9, 7, 16]) {
                range.ApplyStyledValue($"За даними {clientName}, {currencyName}", 10,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Top);
            }

            using (ExcelRange range = worksheet.Cells[8, 2, 8, 2]) {
                range.ApplyStyledValue("Дата", 9, bold: true, borderAroundStyle: ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 3, 8, 4]) {
                range.ApplyStyledValue("Документ", 9, bold: true, borderAroundStyle: ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 5, 8, 6]) {
                range.ApplyStyledValue("Дебет", 9, bold: true, borderAroundStyle: ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 7, 8, 8]) {
                range.ApplyStyledValue("Кредит", 9, bold: true, borderAroundStyle: ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 9, 8, 10]) {
                range.ApplyStyledValue("Дата", 9, bold: true, borderAroundStyle: ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 11, 8, 12]) {
                range.ApplyStyledValue("Документ", 9, bold: true, borderAroundStyle: ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 13, 8, 14]) {
                range.ApplyStyledValue("Дебет", 9, bold: true, borderAroundStyle: ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 15, 8, 16]) {
                range.ApplyStyledValue("Кредит", 9, bold: true, borderAroundStyle: ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[8, 2, 8, 16]) {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(153, 153, 255));
            }

            //Document table body

            using (ExcelRange range = worksheet.Cells[9, 2, 9, 2]) {
                range.ApplyStyledValue("Сальдо початкове", 8, bold: true, merge: false, wrapText: false,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[9, 9, 9, 9]) {
                range.ApplyStyledValue("Сальдо початкове", 8, bold: true, merge: false, wrapText: false,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[9, 12, 9, 12]) {
                range.ApplyStyledEmptyValue(8, bold: true, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            if (initialBalanceValue >= 0) {
                using (ExcelRange range = worksheet.Cells[9, 5, 9, 6]) {
                    if (initialBalanceValue > 0)
                        range.ApplyStyledValue(initialBalanceValue, 8, numberFormat: "#,##0.00", bold: true, borderAroundStyle: ExcelBorderStyle.Thin,
                            verticalAlignment: ExcelVerticalAlignment.Center, horizontalAlignment: ExcelHorizontalAlignment.Right);
                    else
                        range.ApplyStyledEmptyValue(8, numberFormat: "#,##0.00", bold: true, borderAroundStyle: ExcelBorderStyle.Thin,
                            verticalAlignment: ExcelVerticalAlignment.Center, horizontalAlignment: ExcelHorizontalAlignment.Right);
                }

                using (ExcelRange range = worksheet.Cells[9, 15, 9, 16]) {
                    if (initialBalanceValue > 0)
                        range.ApplyStyledValue(initialBalanceValue, 8, numberFormat: "#,##0.00", bold: true, borderAroundStyle: ExcelBorderStyle.Thin,
                            verticalAlignment: ExcelVerticalAlignment.Center, horizontalAlignment: ExcelHorizontalAlignment.Right);
                    else
                        range.ApplyStyledEmptyValue(8, numberFormat: "#,##0.00", bold: true, borderAroundStyle: ExcelBorderStyle.Thin,
                            verticalAlignment: ExcelVerticalAlignment.Center, horizontalAlignment: ExcelHorizontalAlignment.Right);
                }
            } else if (initialBalanceValue < 0) {
                using (ExcelRange range = worksheet.Cells[9, 7, 9, 8]) {
                    range.ApplyStyledValue(initialBalanceValue * -1, 8, numberFormat: "#,##0.00", bold: true, borderAroundStyle: ExcelBorderStyle.Thin,
                        verticalAlignment: ExcelVerticalAlignment.Center, horizontalAlignment: ExcelHorizontalAlignment.Right);
                }

                using (ExcelRange range = worksheet.Cells[9, 13, 9, 14]) {
                    range.ApplyStyledValue(initialBalanceValue * -1, 8, numberFormat: "#,##0.00", bold: true, borderAroundStyle: ExcelBorderStyle.Thin,
                        verticalAlignment: ExcelVerticalAlignment.Center, horizontalAlignment: ExcelHorizontalAlignment.Right);
                }
            }

            int row = 10;

            foreach (AccountingCashFlowHeadItem item in accountingCashFlow.AccountingCashFlowHeadItems) {
                worksheet.SetRowHeight(11.3636, row);

                using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                    range.ApplyStyledValue(item.FromDate.ToString("dd.MM.yy"), 8, borderAroundStyle: ExcelBorderStyle.Thin, wrapText: false,
                        horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Center);
                }

                using (ExcelRange range = worksheet.Cells[row, 3, row, 4]) {
                    range.ApplyStyledValue(item.Name, 8, borderAroundStyle: ExcelBorderStyle.Thin, wrapText: false,
                        horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Center);
                }

                using (ExcelRange range = worksheet.Cells[row, 5, row, 6]) {
                    if (!item.IsCreditValue)
                        range.ApplyStyledValue(item.CurrentValue, 8, numberFormat: "#,##0.00", borderAroundStyle: ExcelBorderStyle.Thin, wrapText: false,
                            horizontalAlignment: ExcelHorizontalAlignment.Right, verticalAlignment: ExcelVerticalAlignment.Center);
                    else
                        range.ApplyStyledEmptyValue(8, numberFormat: "#,##0.00", borderAroundStyle: ExcelBorderStyle.Thin, wrapText: false,
                            horizontalAlignment: ExcelHorizontalAlignment.Right, verticalAlignment: ExcelVerticalAlignment.Center);
                }

                using (ExcelRange range = worksheet.Cells[row, 7, row, 8]) {
                    if (item.IsCreditValue)
                        range.ApplyStyledValue(item.CurrentValue, 8, numberFormat: "#,##0.00", borderAroundStyle: ExcelBorderStyle.Thin, wrapText: false,
                            horizontalAlignment: ExcelHorizontalAlignment.Right, verticalAlignment: ExcelVerticalAlignment.Center);
                    else
                        range.ApplyStyledEmptyValue(8, numberFormat: "#,##0.00", borderAroundStyle: ExcelBorderStyle.Thin, wrapText: false,
                            horizontalAlignment: ExcelHorizontalAlignment.Right, verticalAlignment: ExcelVerticalAlignment.Center);
                }

                using (ExcelRange range = worksheet.Cells[row, 9, row, 10]) {
                    range.ApplyStyledEmptyValue(8, borderAroundStyle: ExcelBorderStyle.Thin, wrapText: false,
                        horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Center);
                }

                using (ExcelRange range = worksheet.Cells[row, 11, row, 12]) {
                    range.ApplyStyledEmptyValue(8, borderAroundStyle: ExcelBorderStyle.Thin, wrapText: false,
                        horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Center);
                }

                using (ExcelRange range = worksheet.Cells[row, 13, row, 14]) {
                    range.ApplyStyledEmptyValue(8, borderAroundStyle: ExcelBorderStyle.Thin, wrapText: false,
                        horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Center);
                }

                using (ExcelRange range = worksheet.Cells[row, 15, row, 16]) {
                    range.ApplyStyledEmptyValue(8, borderAroundStyle: ExcelBorderStyle.Thin, wrapText: false,
                        horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Center);
                }

                row++;
            }

            worksheet.SetRowHeight(11.3636, row);
            worksheet.SetRowHeight(11.3636, row + 1);
            worksheet.SetRowHeight(11.3636, row + 2);
            worksheet.SetRowHeight(21.2121, row + 3);
            worksheet.SetRowHeight(31.0606, row + 4);
            worksheet.SetRowHeight(15.1515, row + 5);
            worksheet.SetRowHeight(21.2121, row + 6);
            worksheet.SetRowHeight(11.3636, row + 7);
            worksheet.SetRowHeight(11.3636, row + 8);
            worksheet.SetRowHeight(11.3636, row + 9);
            worksheet.SetRowHeight(11.3636, row + 10);
            worksheet.SetRowHeight(11.3636, row + 11);
            worksheet.SetRowHeight(11.3636, row + 12);
            worksheet.SetRowHeight(11.3636, row + 13);

            using (ExcelRange range = worksheet.Cells[row, 2, row, 2]) {
                range.ApplyStyledValue("Обороти за період", 8, wrapText: false, bold: true, merge: false,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, 5, row, 6]) {
                range.ApplyStyledValue(accountingCashFlow.AfterRangeInAmount, 8, numberFormat: "#,##0.00",
                    borderAroundStyle: ExcelBorderStyle.Thin, wrapText: false, bold: true,
                    horizontalAlignment: ExcelHorizontalAlignment.Right, verticalAlignment: ExcelVerticalAlignment.Center);
            }

            using (ExcelRange range = worksheet.Cells[row, 7, row, 8]) {
                if (accountingCashFlow.AfterRangeOutAmount != 0)
                    range.ApplyStyledValue(accountingCashFlow.AfterRangeOutAmount, 8, numberFormat: "#,##0.00",
                        borderAroundStyle: ExcelBorderStyle.Thin, wrapText: false, bold: true,
                        horizontalAlignment: ExcelHorizontalAlignment.Right, verticalAlignment: ExcelVerticalAlignment.Center);
                else
                    range.ApplyStyledEmptyValue(8, borderAroundStyle: ExcelBorderStyle.Thin, wrapText: false, bold: true,
                        horizontalAlignment: ExcelHorizontalAlignment.Right, verticalAlignment: ExcelVerticalAlignment.Center);
            }

            using (ExcelRange range = worksheet.Cells[row, 9, row, 9]) {
                range.ApplyStyledValue("Обороти за період", 8, wrapText: false, bold: true, merge: false,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, 13, row, 13]) {
                range.ApplyStyledEmptyValue(8, wrapText: false, bold: true, merge: false,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, 14, row, 14]) {
                range.ApplyStyledEmptyValue(8, wrapText: false, bold: true, merge: false,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row, 16, row, 16]) {
                range.ApplyStyledEmptyValue(8, wrapText: false, bold: true, merge: false,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 1, 2, row + 1, 2]) {
                range.ApplyStyledValue("Сальдо кінцеве", 8, wrapText: false, bold: true, merge: false,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }

            decimal totalBalance = accountingCashFlow.AfterRangeInAmount - accountingCashFlow.AfterRangeOutAmount;

            bool isTotalBalanceDebitMore = totalBalance >= 0;

            if (isTotalBalanceDebitMore) {
                using (ExcelRange range = worksheet.Cells[row + 1, 5, row + 1, 6]) {
                    range.ApplyStyledValue(totalBalance, 8, numberFormat: "#,##0.00", wrapText: false, bold: true, borderAroundStyle: ExcelBorderStyle.Thin,
                        horizontalAlignment: ExcelHorizontalAlignment.Right, verticalAlignment: ExcelVerticalAlignment.Center);
                }

                using (ExcelRange range = worksheet.Cells[row + 1, 7, row + 1, 8]) {
                    range.ApplyStyledEmptyValue(8, wrapText: false, bold: true, borderAroundStyle: ExcelBorderStyle.Thin,
                        horizontalAlignment: ExcelHorizontalAlignment.Right, verticalAlignment: ExcelVerticalAlignment.Center);
                }
            } else {
                using (ExcelRange range = worksheet.Cells[row + 1, 5, row + 1, 6]) {
                    range.ApplyStyledEmptyValue(8, wrapText: false, bold: true, borderAroundStyle: ExcelBorderStyle.Thin,
                        horizontalAlignment: ExcelHorizontalAlignment.Right, verticalAlignment: ExcelVerticalAlignment.Center);
                }

                using (ExcelRange range = worksheet.Cells[row + 1, 7, row + 1, 8]) {
                    range.ApplyStyledValue(totalBalance, 8, numberFormat: "#,##0.00", wrapText: false, bold: true, borderAroundStyle: ExcelBorderStyle.Thin,
                        horizontalAlignment: ExcelHorizontalAlignment.Right, verticalAlignment: ExcelVerticalAlignment.Center);
                }
            }

            using (ExcelRange range = worksheet.Cells[row + 1, 9, row + 1, 9]) {
                range.ApplyStyledValue("Сальдо кінцеве", 8, wrapText: false, bold: true, merge: false,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 1, 13, row + 1, 13]) {
                range.ApplyStyledEmptyValue(8, wrapText: false, bold: true, merge: true,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 1, 14, row + 1, 14]) {
                range.ApplyStyledEmptyValue(8, wrapText: false, bold: true,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 1, 16, row + 1, 16]) {
                range.ApplyStyledEmptyValue(8, wrapText: false, bold: true,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 1, 2, row + 1, 16]) {
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            using (ExcelRange range = worksheet.Cells[row + 3, 2, row + 3, 7]) {
                range.ApplyStyledValue(string.IsNullOrEmpty(concordName) ? "" : $"За даними {concordName} ", 8,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
            }

            using (ExcelRange range = worksheet.Cells[row + 4, 2, row + 4, 7]) {
                string value = isTotalBalanceDebitMore
                    ? string.Format(
                        "на {0} заборгованість на користь {1} {2} {3}",
                        to.ToString("dd.MM.yy"), concordName, string.Format("{0:0,0.##}", totalBalance), currencyName)
                    : string.Format(
                        "на {0} заборгованість на користь {1} {2} {3}",
                        to.ToString("dd.MM.yy"), clientName, string.Format("{0:0,0.##}", totalBalance), currencyName);

                range.ApplyStyledValue(value, 8, bold: true,
                    horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
            }

            using (ExcelRange range = worksheet.Cells[row + 6, 2, row + 6, 7]) {
                if (!string.IsNullOrEmpty(concordName))
                    range.ApplyStyledValue($"Від {concordName} ", 8, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                else
                    range.ApplyStyledEmptyValue(8, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
            }

            using (ExcelRange range = worksheet.Cells[row + 6, 10, row + 6, 15]) {
                range.ApplyStyledValue($"Від {clientName} ", 8, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
            }

            using (ExcelRange range = worksheet.Cells[row + 8, 2, row + 8, 7]) {
                range.ApplyStyledValue("директор", 8, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
            }

            using (ExcelRange range = worksheet.Cells[row + 8, 10, row + 8, 15]) {
                range.ApplyStyledValue("________________", 8, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
            }

            using (ExcelRange range = worksheet.Cells[row + 10, 2, row + 10, 2]) {
                range.ApplyStyledEmptyValue(8, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 10, 3, row + 10, 3]) {
                range.ApplyStyledEmptyValue(8, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 10, 4, row + 10, 7]) {
                range.ApplyStyledValue($"({directorShortName})", 8, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
            }

            using (ExcelRange range = worksheet.Cells[row + 10, 10, row + 10, 10]) {
                range.ApplyStyledEmptyValue(8, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 10, 11, row + 10, 11]) {
                range.ApplyStyledEmptyValue(8, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            using (ExcelRange range = worksheet.Cells[row + 10, 12, row + 10, 15]) {
                range.ApplyStyledValue("(_______________________)", 8, horizontalAlignment: ExcelHorizontalAlignment.Left,
                    verticalAlignment: ExcelVerticalAlignment.Bottom);
            }

            using (ExcelRange range = worksheet.Cells[row + 12, 2, row + 12, 2]) {
                range.ApplyStyledValue("М.П.", 8, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
            }

            using (ExcelRange range = worksheet.Cells[row + 12, 10, row + 12, 10]) {
                range.ApplyStyledValue("М.П.", 8, horizontalAlignment: ExcelHorizontalAlignment.Left, verticalAlignment: ExcelVerticalAlignment.Bottom);
            }

            package.Workbook.Properties.Title = "Accounting Cash Flow";
            package.Workbook.Properties.Author = "Concord CRM";
            package.Workbook.Properties.Comments = "Auto-generated xlsx by Concord CRM";

            package.Save();
        }

        return SaveFiles(fileName);
    }
}