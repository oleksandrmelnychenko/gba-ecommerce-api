using System.Data;

namespace GBA.Domain.Repositories.XmlDocuments.Contracts;

public interface IXmlDocumentRepositoriesFactory {
    IXmlDocumentRepository NewXmlDocumentRepository(IDbConnection connection, IDbConnection additionalConnection);
}