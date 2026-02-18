using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.XmlDocumentModels;

namespace GBA.Domain.Repositories.XmlDocuments.Contracts;

public interface IXmlDocumentRepository {
    List<Sale> GetSalesXmlDocumentByDate(DateTime from, DateTime to);

    ProductIncomesModel GetProductIncomesXmlDocumentByDate(DateTime from, DateTime to);
}