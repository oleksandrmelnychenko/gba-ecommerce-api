using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.DocumentsManagement.Contracts;

public interface IAgreementDocManager {
    string ExportAgreementToDoc(
        string path,
        ClientAgreement clientAgreement,
        IEnumerable<DocumentMonth> months);

    string ExportWarrantyConditionsToDoc(
        string path,
        ClientAgreement clientAgreement,
        IEnumerable<DocumentMonth> months);
}