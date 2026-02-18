using System;
using GBA.Domain.Entities;
using GBA.Domain.EntityHelpers.Accounting;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface IAccountingXlsManager {
    (string xlsxFile, string pdfFile) ExportAccountingCashFlowToXlsx(string path, AccountingCashFlow accountingCashFlow, User user, DateTime to);
}