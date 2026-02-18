using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Repositories.Consumables.Contracts;

namespace GBA.Domain.Repositories.Consumables;

public sealed class CompanyCarRoadListRepository : ICompanyCarRoadListRepository {
    private readonly IDbConnection _connection;

    public CompanyCarRoadListRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(CompanyCarRoadList companyCarRoadList) {
        return _connection.Query<long>(
                "INSERT INTO [CompanyCarRoadList] " +
                "(Comment, FuelAmount, Mileage, TotalKilometers, InCityKilometers, OutsideCityKilometers, MixedModeKilometers, CompanyCarId, OutcomePaymentOrderId, ResponsibleId, " +
                "CreatedById, Updated) " +
                "VALUES (@Comment, @FuelAmount, @Mileage, @TotalKilometers, @InCityKilometers, @OutsideCityKilometers, @MixedModeKilometers, @CompanyCarId, @OutcomePaymentOrderId," +
                " @ResponsibleId, @CreatedById, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                companyCarRoadList
            )
            .Single();
    }

    public void Update(CompanyCarRoadList companyCarRoadList) {
        _connection.Execute(
            "UPDATE [CompanyCarRoadList] " +
            "SET Comment = @Comment, FuelAmount = @FuelAmount, Mileage = @Mileage, TotalKilometers = @TotalKilometers, InCityKilometers = @InCityKilometers, " +
            "OutsideCityKilometers = @OutsideCityKilometers, MixedModeKilometers = @MixedModeKilometers, CompanyCarId = @CompanyCarId, OutcomePaymentOrderId = @OutcomePaymentOrderId, " +
            "ResponsibleId = @ResponsibleId, UpdatedById = @UpdatedById, Updated = GETUTCDATE() " +
            "WHERE [CompanyCarRoadList].ID = @Id",
            companyCarRoadList
        );
    }

    public CompanyCarRoadList GetById(long id) {
        CompanyCarRoadList toReturn = null;

        Type[] types = {
            typeof(CompanyCarRoadList),
            typeof(CompanyCar),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(OutcomePaymentOrder),
            typeof(CompanyCarRoadListDriver),
            typeof(User)
        };

        Func<object[], CompanyCarRoadList> mapper = objects => {
            CompanyCarRoadList roadList = (CompanyCarRoadList)objects[0];
            CompanyCar companyCar = (CompanyCar)objects[1];
            User responsible = (User)objects[2];
            User createdBy = (User)objects[3];
            User updatedBy = (User)objects[4];
            User carCreatedBy = (User)objects[5];
            User carUpdatedBy = (User)objects[6];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[7];
            CompanyCarRoadListDriver companyCarRoadListDriver = (CompanyCarRoadListDriver)objects[8];
            User driver = (User)objects[9];

            if (toReturn == null) {
                if (companyCarRoadListDriver != null) {
                    companyCarRoadListDriver.User = driver;

                    roadList.CompanyCarRoadListDrivers.Add(companyCarRoadListDriver);
                }

                companyCar.CreatedBy = carCreatedBy;
                companyCar.UpdatedBy = carUpdatedBy;

                roadList.CompanyCar = companyCar;
                roadList.Responsible = responsible;
                roadList.CreatedBy = createdBy;
                roadList.UpdatedBy = updatedBy;
                roadList.OutcomePaymentOrder = outcomePaymentOrder;

                toReturn = roadList;
            } else {
                if (companyCarRoadListDriver != null) {
                    companyCarRoadListDriver.User = driver;

                    toReturn.CompanyCarRoadListDrivers.Add(companyCarRoadListDriver);
                }
            }

            return roadList;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [CompanyCarRoadList] " +
            "LEFT JOIN [CompanyCar] " +
            "ON [CompanyCar].ID = [CompanyCarRoadList].CompanyCarID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [CompanyCarRoadList].ResponsibleID " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [CompanyCarRoadList].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [CompanyCarRoadList].UpdatedByID " +
            "LEFT JOIN [User] AS [CarCreatedBy] " +
            "ON [CarCreatedBy].ID = [CompanyCar].CreatedByID " +
            "LEFT JOIN [User] AS [CarUpdatedBy] " +
            "ON [CarUpdatedBy].ID = [CompanyCar].UpdatedByID " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [CompanyCarRoadList].OutcomePaymentOrderID " +
            "LEFT JOIN [CompanyCarRoadListDriver] " +
            "ON [CompanyCarRoadListDriver].CompanyCarRoadListID = [CompanyCarRoadList].ID " +
            "AND [CompanyCarRoadListDriver].Deleted = 0 " +
            "LEFT JOIN [User] AS [Driver] " +
            "ON [Driver].ID = [CompanyCarRoadListDriver].UserID " +
            "WHERE [CompanyCarRoadList].ID = @Id",
            types,
            mapper,
            new { Id = id }
        );

        return toReturn;
    }

    public CompanyCarRoadList GetByNetId(Guid netId) {
        CompanyCarRoadList toReturn = null;

        Type[] types = {
            typeof(CompanyCarRoadList),
            typeof(CompanyCar),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(OutcomePaymentOrder),
            typeof(CompanyCarRoadListDriver),
            typeof(User)
        };

        Func<object[], CompanyCarRoadList> mapper = objects => {
            CompanyCarRoadList roadList = (CompanyCarRoadList)objects[0];
            CompanyCar companyCar = (CompanyCar)objects[1];
            User responsible = (User)objects[2];
            User createdBy = (User)objects[3];
            User updatedBy = (User)objects[4];
            User carCreatedBy = (User)objects[5];
            User carUpdatedBy = (User)objects[6];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[7];
            CompanyCarRoadListDriver companyCarRoadListDriver = (CompanyCarRoadListDriver)objects[8];
            User driver = (User)objects[9];

            if (toReturn == null) {
                if (companyCarRoadListDriver != null) {
                    companyCarRoadListDriver.User = driver;

                    roadList.CompanyCarRoadListDrivers.Add(companyCarRoadListDriver);
                }

                companyCar.CreatedBy = carCreatedBy;
                companyCar.UpdatedBy = carUpdatedBy;

                roadList.CompanyCar = companyCar;
                roadList.Responsible = responsible;
                roadList.CreatedBy = createdBy;
                roadList.UpdatedBy = updatedBy;
                roadList.OutcomePaymentOrder = outcomePaymentOrder;

                toReturn = roadList;
            } else {
                if (companyCarRoadListDriver != null) {
                    companyCarRoadListDriver.User = driver;

                    toReturn.CompanyCarRoadListDrivers.Add(companyCarRoadListDriver);
                }
            }

            return roadList;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [CompanyCarRoadList] " +
            "LEFT JOIN [CompanyCar] " +
            "ON [CompanyCar].ID = [CompanyCarRoadList].CompanyCarID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [CompanyCarRoadList].ResponsibleID " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [CompanyCarRoadList].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [CompanyCarRoadList].UpdatedByID " +
            "LEFT JOIN [User] AS [CarCreatedBy] " +
            "ON [CarCreatedBy].ID = [CompanyCar].CreatedByID " +
            "LEFT JOIN [User] AS [CarUpdatedBy] " +
            "ON [CarUpdatedBy].ID = [CompanyCar].UpdatedByID " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [CompanyCarRoadList].OutcomePaymentOrderID " +
            "LEFT JOIN [CompanyCarRoadListDriver] " +
            "ON [CompanyCarRoadListDriver].CompanyCarRoadListID = [CompanyCarRoadList].ID " +
            "AND [CompanyCarRoadListDriver].Deleted = 0 " +
            "LEFT JOIN [User] AS [Driver] " +
            "ON [Driver].ID = [CompanyCarRoadListDriver].UserID " +
            "WHERE [CompanyCarRoadList].NetUID = @NetId",
            types,
            mapper,
            new { NetId = netId }
        );

        return toReturn;
    }

    public IEnumerable<CompanyCarRoadList> GetAll() {
        List<CompanyCarRoadList> toReturn = new();

        Type[] types = {
            typeof(CompanyCarRoadList),
            typeof(CompanyCar),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(OutcomePaymentOrder),
            typeof(CompanyCarRoadListDriver),
            typeof(User)
        };

        Func<object[], CompanyCarRoadList> mapper = objects => {
            CompanyCarRoadList roadList = (CompanyCarRoadList)objects[0];
            CompanyCar companyCar = (CompanyCar)objects[1];
            User responsible = (User)objects[2];
            User createdBy = (User)objects[3];
            User updatedBy = (User)objects[4];
            User carCreatedBy = (User)objects[5];
            User carUpdatedBy = (User)objects[6];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[7];
            CompanyCarRoadListDriver companyCarRoadListDriver = (CompanyCarRoadListDriver)objects[8];
            User driver = (User)objects[9];

            if (!toReturn.Any(l => l.Id.Equals(roadList.Id))) {
                if (companyCarRoadListDriver != null) {
                    companyCarRoadListDriver.User = driver;

                    roadList.CompanyCarRoadListDrivers.Add(companyCarRoadListDriver);
                }

                companyCar.CreatedBy = carCreatedBy;
                companyCar.UpdatedBy = carUpdatedBy;

                roadList.CompanyCar = companyCar;
                roadList.Responsible = responsible;
                roadList.CreatedBy = createdBy;
                roadList.UpdatedBy = updatedBy;
                roadList.OutcomePaymentOrder = outcomePaymentOrder;

                toReturn.Add(roadList);
            } else {
                if (companyCarRoadListDriver != null) {
                    companyCarRoadListDriver.User = driver;

                    toReturn.First(l => l.Id.Equals(roadList.Id)).CompanyCarRoadListDrivers.Add(companyCarRoadListDriver);
                }
            }

            return roadList;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [CompanyCarRoadList] " +
            "LEFT JOIN [CompanyCar] " +
            "ON [CompanyCar].ID = [CompanyCarRoadList].CompanyCarID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [CompanyCarRoadList].ResponsibleID " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [CompanyCarRoadList].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [CompanyCarRoadList].UpdatedByID " +
            "LEFT JOIN [User] AS [CarCreatedBy] " +
            "ON [CarCreatedBy].ID = [CompanyCar].CreatedByID " +
            "LEFT JOIN [User] AS [CarUpdatedBy] " +
            "ON [CarUpdatedBy].ID = [CompanyCar].UpdatedByID " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [CompanyCarRoadList].OutcomePaymentOrderID " +
            "LEFT JOIN [CompanyCarRoadListDriver] " +
            "ON [CompanyCarRoadListDriver].CompanyCarRoadListID = [CompanyCarRoadList].ID " +
            "AND [CompanyCarRoadListDriver].Deleted = 0 " +
            "LEFT JOIN [User] AS [Driver] " +
            "ON [Driver].ID = [CompanyCarRoadListDriver].UserID " +
            "WHERE [CompanyCarRoadList].Deleted = 0 " +
            "ORDER BY [CompanyCarRoadList].ID DESC",
            types,
            mapper
        );

        return toReturn;
    }

    public IEnumerable<CompanyCarRoadList> GetAll(Guid companyCarNetId, DateTime from, DateTime to) {
        List<CompanyCarRoadList> toReturn = new();

        Type[] types = {
            typeof(CompanyCarRoadList),
            typeof(CompanyCar),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(OutcomePaymentOrder),
            typeof(CompanyCarRoadListDriver),
            typeof(User)
        };

        Func<object[], CompanyCarRoadList> mapper = objects => {
            CompanyCarRoadList roadList = (CompanyCarRoadList)objects[0];
            CompanyCar companyCar = (CompanyCar)objects[1];
            User responsible = (User)objects[2];
            User createdBy = (User)objects[3];
            User updatedBy = (User)objects[4];
            User carCreatedBy = (User)objects[5];
            User carUpdatedBy = (User)objects[6];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[7];
            CompanyCarRoadListDriver companyCarRoadListDriver = (CompanyCarRoadListDriver)objects[8];
            User driver = (User)objects[9];

            if (!toReturn.Any(l => l.Id.Equals(roadList.Id))) {
                if (companyCarRoadListDriver != null) {
                    companyCarRoadListDriver.User = driver;

                    roadList.CompanyCarRoadListDrivers.Add(companyCarRoadListDriver);
                }

                companyCar.CreatedBy = carCreatedBy;
                companyCar.UpdatedBy = carUpdatedBy;

                roadList.CompanyCar = companyCar;
                roadList.Responsible = responsible;
                roadList.CreatedBy = createdBy;
                roadList.UpdatedBy = updatedBy;
                roadList.OutcomePaymentOrder = outcomePaymentOrder;

                toReturn.Add(roadList);
            } else {
                if (companyCarRoadListDriver != null) {
                    companyCarRoadListDriver.User = driver;

                    toReturn.First(l => l.Id.Equals(roadList.Id)).CompanyCarRoadListDrivers.Add(companyCarRoadListDriver);
                }
            }

            return roadList;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [CompanyCarRoadList] " +
            "LEFT JOIN [CompanyCar] " +
            "ON [CompanyCar].ID = [CompanyCarRoadList].CompanyCarID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [CompanyCarRoadList].ResponsibleID " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [CompanyCarRoadList].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [CompanyCarRoadList].UpdatedByID " +
            "LEFT JOIN [User] AS [CarCreatedBy] " +
            "ON [CarCreatedBy].ID = [CompanyCar].CreatedByID " +
            "LEFT JOIN [User] AS [CarUpdatedBy] " +
            "ON [CarUpdatedBy].ID = [CompanyCar].UpdatedByID " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [CompanyCarRoadList].OutcomePaymentOrderID " +
            "LEFT JOIN [CompanyCarRoadListDriver] " +
            "ON [CompanyCarRoadListDriver].CompanyCarRoadListID = [CompanyCarRoadList].ID " +
            "AND [CompanyCarRoadListDriver].Deleted = 0 " +
            "LEFT JOIN [User] AS [Driver] " +
            "ON [Driver].ID = [CompanyCarRoadListDriver].UserID " +
            "WHERE [CompanyCarRoadList].Deleted = 0 " +
            "AND [CompanyCar].NetUID = @CompanyCarNetId " +
            "AND [CompanyCarRoadList].Created >= @From " +
            "AND [CompanyCarRoadList].Created <= @To " +
            "ORDER BY [CompanyCarRoadList].ID DESC",
            types,
            mapper,
            new { CompanyCarNetId = companyCarNetId, From = from, To = to }
        );

        return toReturn;
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [CompanyCarRoadList] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [CompanyCarRoadList].NetUID = @NetId",
            new { NetId = netId }
        );
    }
}