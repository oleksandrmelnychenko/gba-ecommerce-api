using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.AuditEntities;
using GBA.Domain.Repositories.Auditing.Contracts;

namespace GBA.Domain.Repositories.Auditing;

public sealed class AuditRepository : IAuditRepository {
    private readonly IDbConnection _connection;

    public AuditRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(AuditEntity auditEntity) {
        return _connection.Query<long>(
                "INSERT INTO AuditEntity (Type, EntityName, BaseEntityNetUid, UpdatedByNetUid, UpdatedBy, Updated) " +
                "VALUES (@Type, @EntityName, @BaseEntityNetUid, @UpdatedByNetUid, @UpdatedBy, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                auditEntity
            )
            .Single();
    }

    public List<AuditEntity> GetAllByBaseEntityNetUid(Guid netId) {
        List<AuditEntity> entities = new();

        _connection.Query<AuditEntity, AuditEntityProperty, AuditEntityProperty, AuditEntity>(
            "SELECT TOP (15) * FROM AuditEntity " +
            "LEFT JOIN AuditEntityProperty AS OldProperty " +
            "ON AuditEntity.ID = OldProperty.AuditEntityID " +
            "AND OldProperty.Type = 0 " +
            "LEFT JOIN AuditEntityProperty AS NewProperty " +
            "ON AuditEntity.ID = NewProperty.AuditEntityID " +
            "AND NewProperty.Type = 1 " +
            "WHERE AuditEntity.BaseEntityNetUid = @NetId " +
            "ORDER BY AuditEntity.ID DESC",
            (entity, oldProperty, newProperty) => {
                if (entities.Any(a => a.Id.Equals(entity.Id))) {
                    AuditEntity listEntity = entities.First(a => a.Id.Equals(entity.Id));

                    if (oldProperty != null)
                        if (!listEntity.OldValues.Any(v => v.Id.Equals(oldProperty.Id)))
                            listEntity.OldValues.Add(oldProperty);

                    if (newProperty != null)
                        if (!listEntity.NewValues.Any(v => v.Id.Equals(newProperty.Id)))
                            listEntity.NewValues.Add(newProperty);
                } else {
                    if (oldProperty != null)
                        if (!entity.OldValues.Any(v => v.Id.Equals(oldProperty.Id)))
                            entity.OldValues.Add(oldProperty);

                    if (newProperty != null)
                        if (!entity.NewValues.Any(v => v.Id.Equals(newProperty.Id)))
                            entity.NewValues.Add(newProperty);

                    entities.Add(entity);
                }

                return entity;
            },
            new { NetId = netId.ToString() }
        );

        return entities;
    }

    public List<AuditEntity> GetProductChangeHistoryByNetUid(Guid netId) {
        List<AuditEntity> entities = new();

        _connection.Query<AuditEntity, AuditEntityProperty, AuditEntityProperty, string, AuditEntity>(
            "SELECT * FROM AuditEntity " +
            "LEFT JOIN (" +
            "SELECT " +
            "[AuditEntityProperty].ID " +
            ",[AuditEntityProperty].Created " +
            ",[AuditEntityProperty].Deleted " +
            ",[AuditEntityProperty].Description " +
            ",[AuditEntityProperty].AuditEntityID " +
            ",(SELECT TOP(1) [AuditEntityPropertyNameTranslation].[LocalizedName] " +
            "FROM [AuditEntityPropertyNameTranslation] " +
            "WHERE [AuditEntityPropertyNameTranslation].Name = [AuditEntityProperty].Name " +
            "AND [AuditEntityPropertyNameTranslation].CultureCode = @Culture) AS Name " +
            ",[AuditEntityProperty].NetUID " +
            ",[AuditEntityProperty].Type " +
            ",[AuditEntityProperty].Updated " +
            ",[AuditEntityProperty].Value " +
            "FROM [AuditEntityProperty] " +
            "WHERE (" +
            "[AuditEntityProperty].Name = N'UnitPrice' " +
            "OR [AuditEntityProperty].Name = N'GrossWeight' " +
            "OR [AuditEntityProperty].Name = N'NetWeight'" +
            ")" +
            ") AS OldProperty " +
            "ON AuditEntity.ID = OldProperty.AuditEntityID " +
            "AND OldProperty.Type = 0 " +
            "LEFT JOIN (" +
            "SELECT " +
            "[AuditEntityProperty].ID " +
            ",[AuditEntityProperty].Created " +
            ",[AuditEntityProperty].Deleted " +
            ",[AuditEntityProperty].Description " +
            ",[AuditEntityProperty].AuditEntityID " +
            ",(SELECT TOP(1) [AuditEntityPropertyNameTranslation].[LocalizedName] " +
            "FROM [AuditEntityPropertyNameTranslation] " +
            "WHERE [AuditEntityPropertyNameTranslation].Name = [AuditEntityProperty].Name " +
            "AND [AuditEntityPropertyNameTranslation].CultureCode = @Culture) AS Name " +
            ",[AuditEntityProperty].NetUID " +
            ",[AuditEntityProperty].Type " +
            ",[AuditEntityProperty].Updated " +
            ",[AuditEntityProperty].Value " +
            ",[AuditEntityProperty].[Name] AS [OriginalName] " +
            "FROM [AuditEntityProperty] " +
            "WHERE (" +
            "[AuditEntityProperty].Name = N'UnitPrice' " +
            "OR [AuditEntityProperty].Name = N'GrossWeight' " +
            "OR [AuditEntityProperty].Name = N'NetWeight'" +
            ")" +
            ") AS NewProperty " +
            "ON AuditEntity.ID = NewProperty.AuditEntityID " +
            "AND NewProperty.Type = 1 " +
            "WHERE AuditEntity.BaseEntityNetUid = @NetId " +
            "AND AuditEntity.EntityName = N'Product' " +
            "AND [OldProperty].[ID] IS NOT NULL " +
            "ORDER BY AuditEntity.ID DESC",
            (entity, oldProperty, newProperty, originalName) => {
                if (entities.Any(a => a.Id.Equals(entity.Id))) {
                    AuditEntity listEntity = entities.First(a => a.Id.Equals(entity.Id));

                    if (oldProperty != null) {
                        if (originalName.Contains("Weight"))
                            oldProperty.Value =
                                decimal.Round(Convert.ToDecimal(oldProperty.Value), 3, MidpointRounding.AwayFromZero).ToString();
                        else if (originalName.Contains("Price"))
                            oldProperty.Value =
                                decimal.Round(Convert.ToDecimal(oldProperty.Value), 2, MidpointRounding.AwayFromZero).ToString();

                        if (!listEntity.OldValues.Any(v => v.Id.Equals(oldProperty.Id))) listEntity.OldValues.Add(oldProperty);
                    }

                    if (newProperty != null) {
                        if (originalName.Contains("Weight"))
                            newProperty.Value =
                                decimal.Round(Convert.ToDecimal(newProperty.Value), 3, MidpointRounding.AwayFromZero).ToString();
                        else if (originalName.Contains("Price"))
                            newProperty.Value =
                                decimal.Round(Convert.ToDecimal(newProperty.Value), 2, MidpointRounding.AwayFromZero).ToString();

                        if (!listEntity.NewValues.Any(v => v.Id.Equals(newProperty.Id))) listEntity.NewValues.Add(newProperty);
                    }
                } else {
                    if (oldProperty != null) {
                        if (originalName.Contains("Weight"))
                            oldProperty.Value =
                                decimal.Round(Convert.ToDecimal(oldProperty.Value), 3, MidpointRounding.AwayFromZero).ToString();
                        else if (originalName.Contains("Price"))
                            oldProperty.Value =
                                decimal.Round(Convert.ToDecimal(oldProperty.Value), 2, MidpointRounding.AwayFromZero).ToString();

                        if (!entity.OldValues.Any(v => v.Id.Equals(oldProperty.Id))) entity.OldValues.Add(oldProperty);
                    }

                    if (newProperty != null) {
                        if (originalName.Contains("Weight"))
                            newProperty.Value =
                                decimal.Round(Convert.ToDecimal(newProperty.Value), 3, MidpointRounding.AwayFromZero).ToString();
                        else if (originalName.Contains("Price"))
                            newProperty.Value =
                                decimal.Round(Convert.ToDecimal(newProperty.Value), 2, MidpointRounding.AwayFromZero).ToString();

                        if (!entity.NewValues.Any(v => v.Id.Equals(newProperty.Id))) entity.NewValues.Add(newProperty);
                    }

                    entities.Add(entity);
                }

                return entity;
            },
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName },
            splitOn: "ID,OriginalName"
        );

        return entities;
    }

    public List<AuditEntity> GetAllByNetIdLimited(Guid netId, long limit, long offset) {
        List<AuditEntity> toReturn = new();

        _connection.Query<AuditEntity, AuditEntityProperty, AuditEntityProperty, AuditEntity>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ID DESC) AS RowNumber " +
            ", ID " +
            "FROM ( " +
            "SELECT DISTINCT [AuditEntity].ID " +
            "FROM [AuditEntity] " +
            "WHERE [AuditEntity].BaseEntityNetUid = @NetId " +
            ") [Distincts] " +
            ") " +
            "SELECT * " +
            "FROM [AuditEntity] " +
            "LEFT JOIN [AuditEntityProperty] AS [OldProperty] " +
            "ON [OldProperty].AuditEntityID = [AuditEntity].ID " +
            "AND [OldProperty].[Type] = 0 " +
            "LEFT JOIN [AuditEntityProperty] AS [NewEntity] " +
            "ON [NewEntity].AuditEntityID = [AuditEntity].ID " +
            "AND [NewEntity].[Type] = 1 " +
            "WHERE [AuditEntity].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ") ",
            (entity, oldProperty, newProperty) => {
                if (toReturn.Any(a => a.Id.Equals(entity.Id))) {
                    AuditEntity fromList = toReturn.First(a => a.Id.Equals(entity.Id));

                    if (oldProperty != null && !fromList.OldValues.Any(p => p.Id.Equals(oldProperty.Id))) fromList.OldValues.Add(oldProperty);

                    if (newProperty != null && !fromList.NewValues.Any(p => p.Id.Equals(newProperty.Id))) fromList.NewValues.Add(newProperty);
                } else {
                    if (oldProperty != null) entity.OldValues.Add(oldProperty);

                    if (newProperty != null) entity.NewValues.Add(newProperty);

                    toReturn.Add(entity);
                }

                return entity;
            },
            new { NetId = netId, Limit = limit, Offset = offset }
        );

        return toReturn;
    }

    public List<AuditEntity> GetAllByNetIdAndSpecificFieldLimited(Guid netId, long limit, long offset, string fieldName) {
        List<AuditEntity> toReturn = new();

        _connection.Query<AuditEntity, AuditEntityProperty, AuditEntityProperty, AuditEntity>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ID DESC) AS RowNumber " +
            ", ID " +
            "FROM ( " +
            "SELECT DISTINCT [AuditEntity].ID " +
            "FROM [AuditEntity] " +
            "LEFT JOIN [AuditEntityProperty] AS [Old] " +
            "ON [Old].AuditEntityID = [AuditEntity].ID " +
            "AND [Old].[Type] = 0 " +
            "LEFT JOIN [AuditEntityProperty] AS [New] " +
            "ON [New].AuditEntityID = [AuditEntity].ID " +
            "AND [New].[Type] = 1 " +
            "WHERE [AuditEntity].BaseEntityNetUid = @NetId " +
            "AND ([Old].Name = @FieldName OR [Old].Name IS NULL) " +
            "AND [New].Name = @FieldName " +
            ") [Distincts] " +
            ") " +
            "SELECT * " +
            "FROM [AuditEntity] " +
            "LEFT JOIN [AuditEntityProperty] AS [OldProperty] " +
            "ON [OldProperty].AuditEntityID = [AuditEntity].ID " +
            "AND [OldProperty].[Type] = 0 " +
            "LEFT JOIN [AuditEntityProperty] AS [NewEntity] " +
            "ON [NewEntity].AuditEntityID = [AuditEntity].ID " +
            "AND [NewEntity].[Type] = 1 " +
            "WHERE [AuditEntity].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ") " +
            "AND ([OldProperty].Name = @FieldName OR [OldProperty].Name IS NULL) " +
            "AND [NewEntity].Name = @FieldName ",
            (entity, oldProperty, newProperty) => {
                if (toReturn.Any(a => a.Id.Equals(entity.Id))) {
                    AuditEntity fromList = toReturn.First(a => a.Id.Equals(entity.Id));

                    if (oldProperty != null && !fromList.OldValues.Any(p => p.Id.Equals(oldProperty.Id))) fromList.OldValues.Add(oldProperty);

                    if (newProperty != null && !fromList.NewValues.Any(p => p.Id.Equals(newProperty.Id))) fromList.NewValues.Add(newProperty);
                } else {
                    if (oldProperty != null) entity.OldValues.Add(oldProperty);

                    if (newProperty != null) entity.NewValues.Add(newProperty);

                    toReturn.Add(entity);
                }

                return entity;
            },
            new { NetId = netId, Limit = limit, Offset = offset, FieldName = fieldName }
        );

        return toReturn;
    }
}