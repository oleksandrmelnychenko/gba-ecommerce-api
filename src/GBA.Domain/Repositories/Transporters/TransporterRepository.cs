using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.Repositories.Transporters.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Transporters;

public sealed class TransporterRepository : ITransporterRepository {
    private readonly IDbConnection _connection;

    public TransporterRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Transporter transporter) {
        return _connection.Query<long>(
                "INSERT INTO Transporter(Name, TransporterTypeId, CssClass,ImageUrl, Priority, Updated) " +
                "VALUES(@Name, @TransporterTypeId, @CssClass, @ImageUrl, @Priority, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                transporter
            )
            .Single();
    }

    public List<Transporter> GetAll() {
        return _connection.Query<Transporter, TransporterType, TransporterTypeTranslation, Transporter>(
                "SELECT * FROM Transporter " +
                "LEFT OUTER JOIN TransporterType " +
                "ON TransporterType.Id = Transporter.TransporterTypeId AND TransporterType.Deleted = 0 " +
                "LEFT OUTER JOIN TransporterTypeTranslation " +
                "ON TransporterTypeTranslation.TransporterTypeId = TransporterType.Id " +
                "AND TransporterTypeTranslation.CultureCode = @Culture " +
                "WHERE Transporter.Deleted = 0 " +
                "ORDER BY Transporter.Priority DESC, Transporter.Name",
                (transporter, transporterType, transporterTypeTranslation) => {
                    if (transporterType != null) {
                        transporterType.Name = transporterTypeTranslation?.Name ?? transporterType.Name;
                        transporter.TransporterType = transporterType;
                    }

                    return transporter;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();
    }

    public List<Transporter> GetAllByTransporterTypeNetId(Guid transporterTypeNetId) {
        return _connection.Query<Transporter, TransporterType, TransporterTypeTranslation, Transporter>(
                "SELECT * " +
                "FROM [Transporter] " +
                "LEFT JOIN [TransporterType] " +
                "ON [TransporterType].ID = [Transporter].TransporterTypeID " +
                "AND [TransporterType].Deleted = 0 " +
                "LEFT JOIN [TransporterTypeTranslation] " +
                "ON [TransporterTypeTranslation].TransporterTypeID = [TransporterType].ID " +
                "AND [TransporterTypeTranslation].CultureCode = @Culture " +
                "WHERE [TransporterType].NetUID = @NetId " +
                "AND [Transporter].Deleted = 0 " +
                "ORDER BY [Transporter].Priority DESC, [Transporter].Name",
                (transporter, transporterType, transporterTypeTranslation) => {
                    if (transporterTypeTranslation != null) transporterType.Name = transporterTypeTranslation.Name;

                    transporter.TransporterType = transporterType;

                    return transporter;
                },
                new { NetId = transporterTypeNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();
    }

    public List<Transporter> GetAllByTransporterTypeNetIdDeleted(Guid transporterTypeNetId) {
        return _connection.Query<Transporter, TransporterType, TransporterTypeTranslation, Transporter>(
                "SELECT * " +
                "FROM [Transporter] " +
                "LEFT JOIN [TransporterType] " +
                "ON [TransporterType].ID = [Transporter].TransporterTypeID " +
                "AND [TransporterType].Deleted = 0 " +
                "LEFT JOIN [TransporterTypeTranslation] " +
                "ON [TransporterTypeTranslation].TransporterTypeID = [TransporterType].ID " +
                "AND [TransporterTypeTranslation].CultureCode = @Culture " +
                "WHERE [TransporterType].NetUID = @NetId " +
                "ORDER BY [Transporter].Deleted ASC, [Transporter].Priority DESC, [Transporter].Name",
                (transporter, transporterType, transporterTypeTranslation) => {
                    if (transporterTypeTranslation != null) transporterType.Name = transporterTypeTranslation.Name;

                    transporter.TransporterType = transporterType;

                    return transporter;
                },
                new { NetId = transporterTypeNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();
    }

    public Transporter GetById(long id) {
        return _connection.Query<Transporter>(
                "SELECT * FROM Transporter " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public Transporter GetByNetId(Guid netId) {
        return _connection.Query<Transporter, TransporterType, Transporter>(
                "SELECT * FROM Transporter " +
                "LEFT JOIN TransporterType " +
                "ON Transporter.TransporterTypeID = TransporterType.ID AND Transporter.Deleted = 0 " +
                "WHERE Transporter.NetUID = @NetId",
                (transporter, transporterType) => {
                    if (transporterType != null) transporter.TransporterType = transporterType;

                    return transporter;
                },
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void IncreasePriority(Guid netId) {
        _connection.Execute(
            "UPDATE Transporter SET " +
            "Updated = getutcdate(), Priority = Priority + 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }

    public void DecreasePriority(Guid netId) {
        _connection.Execute(
            "UPDATE Transporter SET " +
            "Updated = getutcdate(), Priority = Priority - 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }

    public void Update(Transporter transporter) {
        _connection.Execute(
            "UPDATE Transporter SET Name = @Name, TransporterTypeID = @TransporterTypeId, CssClass = @CssClass, ImageUrl = @ImageUrl, Priority = @Priority, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            transporter
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE Transporter SET Deleted = 1 " +
            "WHERE NetUID = @NetId"
            , new { NetId = netId }
        );
    }
}