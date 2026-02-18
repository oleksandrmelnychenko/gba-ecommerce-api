using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.AllegroServices;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.AllegroServices.Contracts;

namespace GBA.Domain.Repositories.AllegroServices;

public sealed class AllegroProductReservationRepository : IAllegroProductReservationRepository {
    private readonly IDbConnection _connection;

    public AllegroProductReservationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(AllegroProductReservation reservation) {
        _connection.Execute(
            "INSERT INTO [AllegroProductReservation] (ProductId, Qty, AllegroItemId, Updated) " +
            "VALUES (@ProductId, @Qty, @AllegroItemId, getutcdate())",
            reservation
        );
    }

    public List<AllegroProductReservation> GetAllByProductNetId(Guid netId) {
        return _connection.Query<AllegroProductReservation, Product, AllegroProductReservation>(
                "SELECT * " +
                "FROM [AllegroProductReservation] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [AllegroProductReservation].ProductID " +
                "WHERE [Product].NetUID = @NetId",
                (reservation, product) => {
                    reservation.Product = product;

                    return reservation;
                },
                new { NetId = netId }
            )
            .ToList();
    }
}