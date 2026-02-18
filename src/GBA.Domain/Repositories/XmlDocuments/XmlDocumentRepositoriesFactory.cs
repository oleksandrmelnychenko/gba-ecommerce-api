using System.Data;
using GBA.Domain.Repositories.XmlDocuments.Contracts;

namespace GBA.Domain.Repositories.XmlDocuments;

public sealed class XmlDocumentRepositoriesFactory : IXmlDocumentRepositoriesFactory {
    public IXmlDocumentRepository NewXmlDocumentRepository(IDbConnection connection, IDbConnection additionalConnection) {
        return new XmlDocumentRepository(connection, additionalConnection);
    }
}