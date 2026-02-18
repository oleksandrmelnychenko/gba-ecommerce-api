using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface ISynchronizationXlsManager {
    (string xlsxFile, string pdfFile) ExportUkSupplyOrganizationToXlsx(string path, List<SupplyOrganization> supplyOrganizations, IEnumerable<DocumentMonth> months);
    (string xlsxFile, string pdfFile) ExportUkClientsToXlsx(string path, List<Client> supplyOrganizations, IEnumerable<DocumentMonth> months);
}