using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Carriers;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class CarrierStathamRepository : ICarrierStathamRepository {
    private readonly IDbConnection _connection;

    public CarrierStathamRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Statham statham) {
        return _connection.Query<long>(
                "INSERT INTO [Statham] (FirstName, LastName, MiddleName, Updated) " +
                "VALUES (@FirstName, @LastName, @MiddleName, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                statham
            )
            .Single();
    }

    public void Update(Statham statham) {
        _connection.Execute(
            "UPDATE [Statham] " +
            "SET FirstName = @FirstName, LastName = @LastName, MiddleName = @MiddleName, Updated = GETUTCDATE() " +
            "WHERE [Statham].ID = @Id",
            statham
        );
    }

    public Statham GetById(long id) {
        Statham toReturn = null;

        _connection.Query<Statham, StathamCar, StathamPassport, Statham>(
            "SELECT * " +
            "FROM [Statham] " +
            "LEFT JOIN [StathamCar] " +
            "ON [StathamCar].StathamID = [Statham].ID " +
            "AND [StathamCar].Deleted = 0 " +
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].StathamID = [Statham].ID " +
            "AND [StathamPassport].Deleted = 0 " +
            "WHERE [Statham].ID = @Id",
            (statham, car, passport) => {
                if (toReturn != null) {
                    if (car != null && !toReturn.StathamCars.Any(c => c.Id.Equals(car.Id))) toReturn.StathamCars.Add(car);
                    if (passport != null && !toReturn.StathamPassports.Any(p => p.Id.Equals(passport.Id))) toReturn.StathamPassports.Add(passport);
                } else {
                    if (car != null) statham.StathamCars.Add(car);
                    if (passport != null) statham.StathamPassports.Add(passport);

                    toReturn = statham;
                }

                return statham;
            },
            new { Id = id }
        );

        return toReturn;
    }

    public Statham GetByNetId(Guid netId) {
        Statham toReturn = null;

        _connection.Query<Statham, StathamCar, StathamPassport, Statham>(
            "SELECT * " +
            "FROM [Statham] " +
            "LEFT JOIN [StathamCar] " +
            "ON [StathamCar].StathamID = [Statham].ID " +
            "AND [StathamCar].Deleted = 0 " +
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].StathamID = [Statham].ID " +
            "AND [StathamPassport].Deleted = 0 " +
            "WHERE [Statham].NetUID = @NetId",
            (statham, car, passport) => {
                if (toReturn != null) {
                    if (car != null && !toReturn.StathamCars.Any(c => c.Id.Equals(car.Id))) toReturn.StathamCars.Add(car);
                    if (passport != null && !toReturn.StathamPassports.Any(p => p.Id.Equals(passport.Id))) toReturn.StathamPassports.Add(passport);
                } else {
                    if (car != null) statham.StathamCars.Add(car);
                    if (passport != null) statham.StathamPassports.Add(passport);

                    toReturn = statham;
                }

                return statham;
            },
            new { NetId = netId }
        );

        return toReturn;
    }

    public IEnumerable<Statham> GetAll() {
        List<Statham> toReturn = new();

        _connection.Query<Statham, StathamCar, StathamPassport, Statham>(
            "SELECT * " +
            "FROM [Statham] " +
            "LEFT JOIN [StathamCar] " +
            "ON [StathamCar].StathamID = [Statham].ID " +
            "AND [StathamCar].Deleted = 0 " +
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].StathamID = [Statham].ID " +
            "AND [StathamPassport].Deleted = 0 " +
            "WHERE [Statham].Deleted = 0 " +
            "ORDER BY [Statham].LastName",
            (statham, car, passport) => {
                if (toReturn.Any(s => s.Id.Equals(statham.Id))) {
                    Statham fromList = toReturn.First(s => s.Id.Equals(statham.Id));

                    if (car != null && !fromList.StathamCars.Any(c => c.Id.Equals(car.Id))) fromList.StathamCars.Add(car);
                    if (passport != null && !fromList.StathamPassports.Any(p => p.Id.Equals(passport.Id))) fromList.StathamPassports.Add(passport);
                } else {
                    if (car != null) statham.StathamCars.Add(car);
                    if (passport != null) statham.StathamPassports.Add(passport);

                    toReturn.Add(statham);
                }

                return statham;
            }
        );

        return toReturn;
    }

    public IEnumerable<Statham> GetAllFromSearch(string value) {
        List<Statham> toReturn = new();

        _connection.Query<Statham, StathamCar, StathamPassport, Statham>(
            "SELECT * " +
            "FROM [Statham] " +
            "LEFT JOIN [StathamCar] " +
            "ON [StathamCar].StathamID = [Statham].ID " +
            "AND [StathamCar].Deleted = 0 " +
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].StathamID = [Statham].ID " +
            "AND [StathamPassport].Deleted = 0 " +
            "WHERE [Statham].Deleted = 0 " +
            "AND (" +
            "[Statham].LastName like '%' + @Value + '%' " +
            "OR " +
            "[StathamCar].Number like '%' + @Value + '%'" +
            ") " +
            "ORDER BY [Statham].LastName",
            (statham, car, passport) => {
                if (toReturn.Any(s => s.Id.Equals(statham.Id))) {
                    Statham fromList = toReturn.First(s => s.Id.Equals(statham.Id));

                    if (car != null && !fromList.StathamCars.Any(c => c.Id.Equals(car.Id))) fromList.StathamCars.Add(car);
                    if (passport != null && !fromList.StathamPassports.Any(p => p.Id.Equals(passport.Id))) fromList.StathamPassports.Add(passport);
                } else {
                    if (car != null) statham.StathamCars.Add(car);
                    if (passport != null) statham.StathamPassports.Add(passport);

                    toReturn.Add(statham);
                }

                return statham;
            },
            new { Value = value }
        );

        return toReturn;
    }

    public void RemoveByNetId(Guid netId) {
        _connection.Execute(
            "UPDATE [Statham] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [Statham].NetUID = @NetId",
            new { NetId = netId }
        );
    }
}