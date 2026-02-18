using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Organizations;

public sealed class OrganizationTranslationRepository : IOrganizationTranslationRepository {
    private readonly IDbConnection _connection;

    public OrganizationTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(OrganizationTranslation organizationTranslation) {
        return _connection.Query<long>(
                "INSERT INTO OrganizationTranslation (Name, OrganizationId, CultureCode, Updated) " +
                "VALUES (@Name, @OrganizationId, @CultureCode, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                organizationTranslation
            )
            .Single();
    }

    public void Add(IEnumerable<OrganizationTranslation> organizationTranslations) {
        _connection.Execute(
            "INSERT INTO OrganizationTranslation (Name, OrganizationId, CultureCode, Updated) " +
            "VALUES (@Name, @OrganizationId, @CultureCode, getutcdate())",
            organizationTranslations
        );
    }

    public void Update(OrganizationTranslation organizationTranslation) {
        _connection.Execute(
            "UPDATE OrganizationTranslation SET " +
            "Name = @Name, OrganizationId = @OrganizationId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            organizationTranslation
        );
    }

    public void Update(IEnumerable<OrganizationTranslation> organizationTranslations) {
        _connection.Execute(
            "UPDATE OrganizationTranslation SET " +
            "Name = @Name, OrganizationId = @OrganizationId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            organizationTranslations
        );
    }

    public OrganizationTranslation GetById(long id) {
        return _connection.Query<OrganizationTranslation>(
                "SELECT * FROM OrganizationTranslation " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public OrganizationTranslation GetByNetId(Guid netId) {
        return _connection.Query<OrganizationTranslation>(
                "SELECT * FROM OrganizationTranslation " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public List<OrganizationTranslation> GetAll() {
        return _connection.Query<OrganizationTranslation>(
                "SELECT * FROM OrganizationTranslation " +
                "WHERE Deleted = 0"
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE OrganizationTranslation SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }
}