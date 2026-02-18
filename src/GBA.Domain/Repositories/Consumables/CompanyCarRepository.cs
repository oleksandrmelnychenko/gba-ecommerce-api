using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Repositories.Consumables.Contracts;

namespace GBA.Domain.Repositories.Consumables;

public sealed class CompanyCarRepository : ICompanyCarRepository {
    private readonly IDbConnection _connection;

    public CompanyCarRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(CompanyCar companyCar) {
        return _connection.Query<long>(
                "INSERT INTO [CompanyCar] " +
                "(LicensePlate, TankCapacity, InCityConsumption, OutsideCityConsumption, MixedModeConsumption, Mileage, CreatedById, FuelAmount, InitialMileage, " +
                "ConsumablesStorageId, OrganizationId, CarBrand, Updated) " +
                "VALUES (@LicensePlate, @TankCapacity, @InCityConsumption, @OutsideCityConsumption, @MixedModeConsumption, @Mileage, @CreatedById, @FuelAmount, " +
                "@InitialMileage, @ConsumablesStorageId, @OrganizationId, @CarBrand, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                companyCar
            )
            .Single();
    }

    public void Update(CompanyCar companyCar) {
        _connection.Execute(
            "UPDATE [CompanyCar] " +
            "SET LicensePlate = @LicensePlate, TankCapacity = @TankCapacity, InCityConsumption = @InCityConsumption, OutsideCityConsumption = @OutsideCityConsumption, " +
            "MixedModeConsumption = @MixedModeConsumption, Mileage = @Mileage, UpdatedById = @UpdatedById, FuelAmount = @FuelAmount, ConsumablesStorageId = @ConsumablesStorageId, " +
            "OrganizationId = @OrganizationId, CarBrand = @CarBrand, Updated = getutcdate() " +
            "WHERE [CompanyCar].ID = @Id",
            companyCar
        );
    }

    public void UpdateFuelAmountByCarId(long id, double toAddFuelAmount) {
        _connection.Execute(
            "UPDATE [CompanyCar] " +
            "SET FuelAmount = ROUND(FuelAmount + @ToAddFuelAmount, 2), Updated = getutcdate() " +
            "WHERE [CompanyCar].ID = @Id",
            new { Id = id, ToAddFuelAmount = toAddFuelAmount }
        );
    }

    public CompanyCar GetById(long id) {
        return _connection.Query<CompanyCar, User, User, Organization, ConsumablesStorage, CompanyCar>(
                "SELECT * " +
                "FROM [CompanyCar] " +
                "LEFT JOIN [User] AS [CreatedBy] " +
                "ON [CreatedBy].ID = [CompanyCar].CreatedByID " +
                "LEFT JOIN [User] AS [UpdatedBy] " +
                "ON [UpdatedBy].ID = [CompanyCar].UpdatedByID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [CompanyCar].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [CompanyCar].ConsumablesStorageID " +
                "WHERE [CompanyCar].ID = @Id",
                (car, createdBy, updatedBy, organization, storage) => {
                    storage.Organization = organization;

                    car.Organization = organization;
                    car.ConsumablesStorage = storage;
                    car.CreatedBy = createdBy;
                    car.UpdatedBy = updatedBy;

                    return car;
                },
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public CompanyCar GetByNetId(Guid netId) {
        return _connection.Query<CompanyCar, User, User, Organization, ConsumablesStorage, CompanyCar>(
                "SELECT * " +
                "FROM [CompanyCar] " +
                "LEFT JOIN [User] AS [CreatedBy] " +
                "ON [CreatedBy].ID = [CompanyCar].CreatedByID " +
                "LEFT JOIN [User] AS [UpdatedBy] " +
                "ON [UpdatedBy].ID = [CompanyCar].UpdatedByID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [CompanyCar].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [CompanyCar].ConsumablesStorageID " +
                "WHERE [CompanyCar].NetUID = @NetId",
                (car, createdBy, updatedBy, organization, storage) => {
                    storage.Organization = organization;

                    car.Organization = organization;
                    car.ConsumablesStorage = storage;
                    car.CreatedBy = createdBy;
                    car.UpdatedBy = updatedBy;

                    return car;
                },
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public IEnumerable<CompanyCar> GetAll() {
        return _connection.Query<CompanyCar, User, User, Organization, ConsumablesStorage, CompanyCar>(
            "SELECT * " +
            "FROM [CompanyCar] " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [CompanyCar].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [CompanyCar].UpdatedByID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [CompanyCar].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [CompanyCar].ConsumablesStorageID " +
            "WHERE [CompanyCar].Deleted = 0",
            (car, createdBy, updatedBy, organization, storage) => {
                storage.Organization = organization;

                car.Organization = organization;
                car.ConsumablesStorage = storage;
                car.CreatedBy = createdBy;
                car.UpdatedBy = updatedBy;

                return car;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public IEnumerable<CompanyCar> GetAllFromSearch(string value) {
        return _connection.Query<CompanyCar, User, User, Organization, ConsumablesStorage, CompanyCar>(
            "SELECT * " +
            "FROM [CompanyCar] " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [CompanyCar].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [CompanyCar].UpdatedByID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [CompanyCar].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [CompanyCar].ConsumablesStorageID " +
            "WHERE [CompanyCar].Deleted = 0 " +
            "AND [CompanyCar].LicensePlate like '%' + @Value + '%'",
            (car, createdBy, updatedBy, organization, storage) => {
                storage.Organization = organization;

                car.Organization = organization;
                car.ConsumablesStorage = storage;
                car.CreatedBy = createdBy;
                car.UpdatedBy = updatedBy;

                return car;
            },
            new { Value = value, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [CompanyCar] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [CompanyCar].NetUID = @NetId",
            new { NetId = netId }
        );
    }
}