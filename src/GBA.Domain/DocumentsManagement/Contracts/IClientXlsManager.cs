using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers.DebtorModels;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface IClientXlsManager {
    (string xlsxFile, string pdfFile) ExportClientInDebtToXlsx(string path, ClientDebtorsModel clientInDebtors);

    (string xlsxFile, string pdfFile) ExportAllClientsToXls(string path, List<Client> clients);
}