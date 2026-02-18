using System.Collections.Generic;
using System.Xml.Linq;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.XmlDocumentModels;

namespace GBA.Domain.XmlDocumentManagement.Contracts;

public interface IXmlManager {
    XDocument GetSalesXmlDocuments(string pathToFolder, List<Sale> sales);

    XDocument GetProductIncomeDocument(string pathToFolder, ProductIncomesModel productIncomesModel);
}