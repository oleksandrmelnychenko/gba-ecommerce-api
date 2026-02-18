using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class SupplyOrganizationDocumentRepository : ISupplyOrganizationDocumentRepository {
    private readonly IDbConnection _connection;

    public SupplyOrganizationDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<SupplyOrganizationDocument> documents) {
        _connection.Execute(
            "INSERT INTO [SupplyOrganizationDocument] (SupplyOrganizationAgreementId, DocumentUrl, FileName, ContentType, GeneratedName, Updated) " +
            "VALUES (@SupplyOrganizationAgreementId, @DocumentUrl, @FileName, @ContentType, @GeneratedName, getutcdate())",
            documents
        );
    }

    public void Update(IEnumerable<SupplyOrganizationDocument> documents) {
        _connection.Execute(
            "UPDATE [SupplyOrganizationDocument] " +
            "SET SupplyOrganizationAgreementId = @SupplyOrganizationAgreementId, DocumentUrl = @DocumentUrl, FileName = @FileName, ContentType = @ContentType, " +
            "GeneratedName = @GeneratedName, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            documents
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyOrganizationDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [SupplyOrganizationDocument].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllBySupplyOrganizationAgreementId(long id) {
        _connection.Execute(
            "UPDATE [SupplyOrganizationDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [SupplyOrganizationDocument].SupplyOrganizationAgreementID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllBySupplyOrganizationAgreementIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyOrganizationDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [SupplyOrganizationDocument].SupplyOrganizationAgreementID IN @Ids",
            new { Ids = ids }
        );
    }
}