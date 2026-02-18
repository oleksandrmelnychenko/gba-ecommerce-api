using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.ConsignmentNoteSettings;
using GBA.Domain.Repositories.ConsignmentNoteSettings.Contracts;

namespace GBA.Domain.Repositories.ConsignmentNoteSettings;

public sealed class ConsignmentNoteSettingRepository : IConsignmentNoteSettingRepository {
    private readonly IDbConnection _connection;

    public ConsignmentNoteSettingRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ConsignmentNoteSetting consignmentNoteSetting) {
        _connection.Execute(
            "INSERT INTO [ConsignmentNoteSetting]([Updated], [BrandAndNumberCar], [TrailerNumber], [Driver], [Carrier], " +
            "[TypeTransportation], [UnloadingPoint], [LoadingPoint], [Customer], [Name], [ForReSale], " +
            "[CarGrossWeight], CarHeight, CarLabel, CarLength, CarNetWeight, CarWidth, TrailerGrossWeight, TrailerHeight," +
            "TrailerLabel, TrailerLength, TrailerNetWeight, TrailerWidth) " +
            "VALUES (getutcdate(), @BrandAndNumberCar, @TrailerNumber, @Driver, @Carrier, " +
            "@TypeTransportation, @UnloadingPoint, @LoadingPoint, @Customer, @Name, @ForReSale, " +
            "@CarGrossWeight, @CarHeight, @CarLabel, @CarLength, @CarNetWeight, @CarWidth, @TrailerGrossWeight, @TrailerHeight, " +
            "@TrailerLabel, @TrailerLength, @TrailerNetWeight, @TrailerWidth); ",
            consignmentNoteSetting);
    }

    public void Update(ConsignmentNoteSetting consignmentNoteSetting) {
        _connection.Execute(
            "UPDATE [ConsignmentNoteSetting] " +
            "SET [Updated] = getutcdate() " +
            ", [BrandAndNumberCar] = @BrandAndNumberCar " +
            ", [TrailerNumber] = @TrailerNumber " +
            ", [Driver] = @Driver " +
            ", [Carrier] = @Carrier " +
            ", [TypeTransportation] = @TypeTransportation " +
            ", [UnloadingPoint] = @UnloadingPoint " +
            ", [LoadingPoint] = @LoadingPoint " +
            ", [Customer] = @Customer " +
            ", [Name] = @Name " +
            ", [CarLabel] = @CarLabel " +
            ", [CarLength] = @CarLength " +
            ", [CarWidth] = @CarWidth " +
            ", [CarHeight] = @CarHeight " +
            ", [CarNetWeight] = @CarNetWeight " +
            ", [CarGrossWeight] = @CarGrossWeight " +
            ", [TrailerLabel] = @TrailerLabel " +
            ", [TrailerLength] = @TrailerLength " +
            ", [TrailerWidth] = @TrailerWidth " +
            ", [TrailerHeight] = @TrailerHeight " +
            ", [TrailerNetWeight] = @TrailerNetWeight " +
            ", [TrailerGrossWeight] = @TrailerGrossWeight " +
            "WHERE [ConsignmentNoteSetting].[ID] = @Id; ",
            consignmentNoteSetting);
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [ConsignmentNoteSetting] " +
            "SET [Updated] = getutcdate() " +
            ", [Deleted] = 1 " +
            "WHERE [ConsignmentNoteSetting].[NetUID] = @NetId; ",
            new { NetId = netId });
    }

    public ConsignmentNoteSetting GetByNetId(Guid netId) {
        return _connection.Query<ConsignmentNoteSetting>(
            "SELECT * FROM [ConsignmentNoteSetting] " +
            "WHERE [ConsignmentNoteSetting].[NetUID] = @NetId; ",
            new { NetId = netId }).FirstOrDefault();
    }

    public List<ConsignmentNoteSetting> GetAll(
        bool forReSale) {
        return _connection.Query<ConsignmentNoteSetting>(
            "SELECT * FROM [ConsignmentNoteSetting] " +
            "WHERE [ConsignmentNoteSetting].[Deleted] = 0 " +
            "AND [ConsignmentNoteSetting].[ForReSale] = @ForReSale; ",
            new { ForReSale = forReSale }).AsList();
    }
}