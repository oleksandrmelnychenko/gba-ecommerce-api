using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.ServicePayers.Contracts;

namespace GBA.Domain.Repositories.ServicePayers;

public sealed class ServicePayerRepository : IServicePayerRepository {
    private readonly IDbConnection _connection;

    public ServicePayerRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ServicePayer servicePayer) {
        return _connection.Query<long>(
                "INSERT INTO ServicePayer (FirstName, LastName, MiddleName, MobilePhone, Comment, PaymentAddress, PaymentCard, ServiceType, ClientId, Updated) " +
                "VALUES (@FirstName, @LastName, @MiddleName, @MobilePhone, @Comment, @PaymentAddress, @PaymentCard, @ServiceType, @ClientId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                servicePayer
            )
            .Single();
    }

    public void Add(IEnumerable<ServicePayer> servicePayers) {
        _connection.Execute(
            "INSERT INTO ServicePayer (FirstName, LastName, MiddleName, MobilePhone, Comment, PaymentAddress, PaymentCard, ServiceType, ClientId, Updated) " +
            "VALUES (@FirstName, @LastName, @MiddleName, @MobilePhone, @Comment, @PaymentAddress, @PaymentCard, @ServiceType, @ClientId, getutcdate())",
            servicePayers
        );
    }

    public void Update(ServicePayer servicePayer) {
        _connection.Execute(
            "UPDATE ServicePayer SET " +
            "FirstName = @FirstName, LastName = @LastName, MiddleName = @MiddleName, MobilePhone = @MobilePhone, Comment = @Comment, PaymentAddress = @PaymentAddress, PaymentCard = @PaymentCard, ServiceType = @ServiceType, ClientId = @ClientId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            servicePayer
        );
    }

    public void Update(IEnumerable<ServicePayer> servicePayers) {
        _connection.Execute(
            "UPDATE ServicePayer SET " +
            "FirstName = @FirstName, LastName = @LastName, MiddleName = @MiddleName, MobilePhone = @MobilePhone, Comment = @Comment, PaymentAddress = @PaymentAddress, PaymentCard = @PaymentCard, ServiceType = @ServiceType, ClientId = @ClientId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            servicePayers
        );
    }

    public List<ServicePayer> GetAllByClientId(long id) {
        return _connection.Query<ServicePayer>(
                "SELECT * FROM ServicePayer " +
                "WHERE ServicePayer.Deleted = 0 " +
                "AND ServicePayer.ClientID = @Id",
                new { Id = id }
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE ServicePayer SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }

    public void Remove(IEnumerable<ServicePayer> servicePayers) {
        _connection.Execute(
            "UPDATE ServicePayer SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            servicePayers
        );
    }
}