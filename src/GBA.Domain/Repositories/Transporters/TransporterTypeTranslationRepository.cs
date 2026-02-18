using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.Transporters.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Transporters;

public sealed class TransporterTypeTranslationRepository : ITransporterTypeTranslationRepository {
    private readonly IDbConnection _connection;

    public TransporterTypeTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(TransporterTypeTranslation transporterTypeTranslation) {
        return _connection.Query<long>(
                "INSERT INTO TransporterTypeTranslation (Name, CultureCode, TransporterTypeId, Updated) " +
                "VALUES(@Name, @CultureCode, @TransporterTypeId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                transporterTypeTranslation
            )
            .Single();
    }


    public void Add(IEnumerable<TransporterTypeTranslation> transporterTypeTranslations) {
        _connection.Execute(
            "INSERT INTO TransporterTypeTranslation (Name, CultureCode, TransporterTypeId, Updated) " +
            "VALUES(@Name, @CultureCode, @TransporterTypeId, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            transporterTypeTranslations
        );
    }

    public List<TransporterTypeTranslation> GetAll() {
        return _connection.Query<TransporterTypeTranslation>(
                "SELECT * FROM TransporterTypeTranslation WHERE Deleted = 0"
            )
            .ToList();
    }

    public TransporterTypeTranslation GetById(long id) {
        return _connection.Query<TransporterTypeTranslation>(
                "SELECT * FROM TransporterTypeTranslation " +
                "WHERE Id = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public TransporterTypeTranslation GetByNetId(Guid netId) {
        return _connection.Query<TransporterTypeTranslation>(
                "SELECT * FROM TransporterTypeTranslation " +
                "WHERE NetUid = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE TransporterTypeTranslation SET Deleted = 0 WHERE NetUid = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(TransporterTypeTranslation organizationTranslation) {
        _connection.Execute(
            "UPDATE TransporterTypeTranslation " +
            "SET Name = @Name, CultureCode = @CultureCode, TransporterTypeId = @TransporterTypeId, Updated = getutcdate() " +
            "WHERE NetUid = @NetUid",
            organizationTranslation
        );
    }

    public void Update(IEnumerable<TransporterTypeTranslation> transporterTypeTranslations) {
        _connection.Execute(
            "UPDATE TransporterTypeTranslation " +
            "SET Name = @Name, CultureCode = @CultureCode, TransporterTypeId = @TransporterTypeId, Updated = getutcdate() " +
            "WHERE NetUid = @NetUid",
            transporterTypeTranslations
        );
    }
}