using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class SaleNumberRepository : ISaleNumberRepository {
    private readonly IDbConnection _connection;

    public SaleNumberRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SaleNumber saleNumber) {
        return _connection.Query<long>(
                "INSERT INTO SaleNumber (Value, OrganizationId, Updated) VALUES(@Value, @OrganizationId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                saleNumber
            )
            .Single();
    }

    public List<SaleNumber> GetAll() {
        return _connection.Query<SaleNumber>(
                "SELECT * FROM SaleNumber WHERE Deleted = 0"
            )
            .ToList();
    }

    public SaleNumber GetById(long id) {
        return _connection.Query<SaleNumber>(
                "SELECT * FROM SaleNumber WHERE Id = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public SaleNumber GetByNetId(Guid netId) {
        return _connection.Query<SaleNumber>(
                "SELECT * FROM SaleNumber WHERE NetUid = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public SaleNumber GetLastRecordByOrganizationNetId(Guid organizationNetId) {
        return _connection.Query<SaleNumber, Organization, SaleNumber>(
                "SELECT TOP 1 * FROM SaleNumber " +
                "LEFT JOIN Organization " +
                "ON SaleNumber.OrganizationID = Organization.ID " +
                "WHERE Organization.NetUID = @OrganizationNetId " +
                "ORDER BY SaleNumber.Created DESC",
                (saleNumber, organization) => {
                    saleNumber.Organization = organization;

                    return saleNumber;
                },
                new { OrganizationNetId = organizationNetId }
            )
            .SingleOrDefault();
    }

    public SaleNumber GetLastGeneratedByOrganizationNetId(
        Guid organizationNetId,
        string valuePrefix,
        int createdYear) {
        if (organizationNetId.Equals(Guid.Empty))
            throw new ArgumentException(
                "Organization identity is required.",
                nameof(organizationNetId));
        if (string.IsNullOrWhiteSpace(valuePrefix))
            throw new ArgumentException(
                "Sale-number prefix is required.",
                nameof(valuePrefix));
        if (createdYear < 2000 || createdYear >= 9999)
            throw new ArgumentOutOfRangeException(
                nameof(createdYear),
                "Sale-number year is outside the supported range.");

        DateTime yearStart =
            new DateTime(createdYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime yearEnd = yearStart.AddYears(1);

        return _connection.Query<SaleNumber, Organization, SaleNumber>(
                "SELECT TOP 1 [SaleNumber].*, [Organization].* " +
                "FROM [Organization] " +
                "CROSS APPLY ( " +
                "SELECT TOP 1 * " +
                "FROM [SaleNumber] WITH (UPDLOCK, HOLDLOCK) " +
                "WHERE [SaleNumber].[OrganizationID] = [Organization].[ID] " +
                "AND [SaleNumber].[Deleted] = 0 " +
                "AND [SaleNumber].[Created] >= @YearStart " +
                "AND [SaleNumber].[Created] < @YearEnd " +
                "AND LEFT([SaleNumber].[Value], LEN(@ValuePrefix)) = @ValuePrefix " +
                "AND LEN([SaleNumber].[Value]) > LEN(@ValuePrefix) " +
                "AND TRY_CONVERT(bigint, SUBSTRING(" +
                "[SaleNumber].[Value], LEN(@ValuePrefix) + 1, 64)) IS NOT NULL " +
                "ORDER BY TRY_CONVERT(bigint, SUBSTRING(" +
                "[SaleNumber].[Value], LEN(@ValuePrefix) + 1, 64)) DESC, " +
                "[SaleNumber].[ID] DESC " +
                ") AS [SaleNumber] " +
                "WHERE [Organization].[NetUID] = @OrganizationNetId",
                (saleNumber, organization) => {
                    saleNumber.Organization = organization;
                    return saleNumber;
                },
                new {
                    OrganizationNetId = organizationNetId,
                    ValuePrefix = valuePrefix,
                    YearStart = yearStart,
                    YearEnd = yearEnd
                })
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE SaleNumber SET Deleted = 1 WHERE NetUid = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(SaleNumber saleNumber) {
        _connection.Execute(
            "UPDATE SaleNumber SET Value = @Value, OrganizationId = @OrganizationId, Updated = getutcdate() WHERE NetUid = @NetUid",
            saleNumber
        );
    }
}