using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Messages.Supplies.Ukraine.DynamicProductPlacementRows;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Services.Actors.Supplies.Ukraine;

public sealed class DynamicProductPlacementRowsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;

    public DynamicProductPlacementRowsActor(
        IDbConnectionFactory connectionFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;

        Receive<AddNewDynamicProductPlacementRowMessage>(message => {
            try {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                if (message.Row == null) throw new Exception("DynamicProductPlacementRow entity can not be null or empty");

                if (!message.Row.IsNew()) throw new Exception("Existing DynamicProductPlacementRow is not valid payload for current request");

                if ((message.Row.SupplyOrderUkraineItem == null || message.Row.SupplyOrderUkraineItem.IsNew()) && !message.Row.SupplyOrderUkraineItemId.HasValue
                                                                                                               && (message.Row.PackingListPackageOrderItem == null ||
                                                                                                                   message.Row.PackingListPackageOrderItem.IsNew())
                                                                                                               && !message.Row.PackingListPackageOrderItemId.HasValue)
                    throw new Exception("You need to specify existing SupplyOrderUkraineItem or PackListItem in DynamicProductPlacementRow entity");

                if (message.Row.DynamicProductPlacementColumn == null && message.Row.DynamicProductPlacementColumnId.Equals(0))
                    throw new Exception("You need to specify new or existing DynamicProductPlacementColumn in DynamicProductPlacementRow entity");
                if (message.Row.DynamicProductPlacementColumn != null && message.Row.DynamicProductPlacementColumn.IsNew())
                    if (message.Row.DynamicProductPlacementColumn.SupplyOrderUkraine == null || message.Row.DynamicProductPlacementColumn.SupplyOrderUkraine.IsNew()
                        && !message.Row.DynamicProductPlacementColumn.SupplyOrderUkraineId.HasValue
                        && message.Row.DynamicProductPlacementColumn.PackingList == null || message.Row.DynamicProductPlacementColumn.PackingList.IsNew()
                        && !message.Row.DynamicProductPlacementColumn.PackingListId.HasValue)
                        throw new Exception("You need to specify existing SupplyOrderUkraine or PackList in DynamicProductPlacementColumn entity");

                IDynamicProductPlacementRowRepository rowRepository = _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementRowRepository(connection);

                if (message.Row.DynamicProductPlacementColumn != null && message.Row.DynamicProductPlacementColumn.IsNew()) {
                    message.Row.DynamicProductPlacementColumn.FromDate =
                        message.Row.DynamicProductPlacementColumn.FromDate.Year.Equals(1)
                            ? DateTime.UtcNow.Date
                            : message.Row.DynamicProductPlacementColumn.FromDate.Date;

                    if (message.Row.DynamicProductPlacementColumn.SupplyOrderUkraine != null && !message.Row.DynamicProductPlacementColumn.SupplyOrderUkraine.IsNew())
                        message.Row.DynamicProductPlacementColumn.SupplyOrderUkraineId = message.Row.DynamicProductPlacementColumn.SupplyOrderUkraine.Id;

                    if (message.Row.DynamicProductPlacementColumn.PackingList != null && !message.Row.DynamicProductPlacementColumn.PackingList.IsNew())
                        message.Row.DynamicProductPlacementColumn.PackingListId = message.Row.DynamicProductPlacementColumn.PackingList.Id;

                    message.Row.DynamicProductPlacementColumnId =
                        _supplyUkraineRepositoriesFactory
                            .NewDynamicProductPlacementColumnRepository(connection)
                            .Add(
                                message.Row.DynamicProductPlacementColumn
                            );
                }

                if (message.Row.SupplyOrderUkraineItem != null && !message.Row.SupplyOrderUkraineItem.IsNew())
                    message.Row.SupplyOrderUkraineItemId = message.Row.SupplyOrderUkraineItem.Id;

                if (message.Row.PackingListPackageOrderItem != null && !message.Row.PackingListPackageOrderItem.IsNew())
                    message.Row.PackingListPackageOrderItemId = message.Row.PackingListPackageOrderItem.Id;

                message.Row.Id = rowRepository.Add(message.Row);

                if (message.Row.DynamicProductPlacements.Any(p => !p.IsApplied)) {
                    IDynamicProductPlacementRepository dynamicProductPlacementRepository =
                        _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementRepository(connection);

                    dynamicProductPlacementRepository
                        .Add(
                            message
                                .Row
                                .DynamicProductPlacements
                                .Where(p => !p.IsApplied && p.IsNew() && p.Qty > 0)
                                .Select(placement => {
                                    placement.DynamicProductPlacementRowId = message.Row.Id;

                                    return placement;
                                })
                        );

                    dynamicProductPlacementRepository
                        .Update(
                            message
                                .Row
                                .DynamicProductPlacements
                                .Where(p => !p.IsApplied && !p.IsNew() && p.Qty > 0)
                        );
                }

                Sender.Tell(
                    rowRepository
                        .GetById(
                            message.Row.Id
                        )
                );
            } catch (Exception exc) {
                Sender.Tell(exc);
            }
        });

        Receive<UpdateDynamicProductPlacementRowMessage>(message => {
            try {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                if (message.Row == null) throw new Exception("DynamicProductPlacementRow entity can not be null or empty");

                if (message.Row.IsNew()) throw new Exception("New DynamicProductPlacementRow is not valid payload for current request");

                if (message.Row.DynamicProductPlacements.Any(e => e.StorageNumber.Length > 5))
                    throw new Exception(DynamicProductPlacementNames.CHARACTER_LIMIT_EXCEEDED);
                if (message.Row.DynamicProductPlacements.Any(e => e.RowNumber.Length > 5))
                    throw new Exception(DynamicProductPlacementNames.CHARACTER_LIMIT_EXCEEDED);
                if (message.Row.DynamicProductPlacements.Any(e => e.CellNumber.Length > 5))
                    throw new Exception(DynamicProductPlacementNames.CHARACTER_LIMIT_EXCEEDED);

                IDynamicProductPlacementRepository dynamicProductPlacementRepository = _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementRepository(connection);
                IDynamicProductPlacementRowRepository dynamicProductPlacementRowRepository =
                    _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementRowRepository(connection);

                dynamicProductPlacementRowRepository.Update(message.Row);

                IEnumerable<DynamicProductPlacement> placementsToRemove =
                    dynamicProductPlacementRepository
                        .GetAllByRowIdExceptProvided(
                            message.Row.Id,
                            message
                                .Row
                                .DynamicProductPlacements
                                .Where(p => !p.IsApplied && !p.IsNew() && p.Qty > 0)
                                .Select(p => p.Id)
                        );

                if (placementsToRemove.Any()) {
                    DynamicProductPlacementRow rowFromDb = dynamicProductPlacementRowRepository.GetByIdWithoutIncludes(message.Row.Id);

                    if (rowFromDb != null) {
                        List<DynamicProductPlacement> leftProductPlacements = message.Row.DynamicProductPlacements
                            .Where(e => !placementsToRemove
                                .Select(toRemoveId => toRemoveId.Id)
                                .Contains(e.Id))
                            .ToList();

                        if (leftProductPlacements.Any()) {
                            placementsToRemove = placementsToRemove.Select(p => {
                                p.RowNumber = "N";
                                p.CellNumber = "N";
                                p.StorageNumber = "N";
                                p.Deleted = true;
                                return p;
                            }).ToList();

                            leftProductPlacements.First().Qty += placementsToRemove.Sum(e => e.Qty);
                        } else {
                            placementsToRemove = placementsToRemove.Select(p => {
                                p.RowNumber = "N";
                                p.CellNumber = "N";
                                p.StorageNumber = "N";
                                return p;
                            }).ToList();
                        }

                        dynamicProductPlacementRepository.Update(placementsToRemove);
                        // rowFromDb.Qty -= placementsToRemove.Sum(p => p.Qty);
                        // dynamicProductPlacementRowRepository.Update(rowFromDb);
                    }

                    // dynamicProductPlacementRepository
                    //     .RemoveAllByRowIdExceptProvided(
                    //         message.Row.Id,
                    //         message
                    //             .Row
                    //             .DynamicProductPlacements
                    //             .Where(p => !p.IsApplied && !p.IsNew() && p.Qty > 0)
                    //             .Select(p => p.Id)
                    //     );
                }

                if (message.Row.DynamicProductPlacements.Any(p => !p.IsApplied)) {
                    dynamicProductPlacementRepository
                        .Add(
                            message
                                .Row
                                .DynamicProductPlacements
                                .Where(p => !p.IsApplied && p.IsNew() && p.Qty > 0)
                                .Select(placement => {
                                    placement.DynamicProductPlacementRowId = message.Row.Id;

                                    return placement;
                                })
                        );

                    dynamicProductPlacementRepository
                        .Update(
                            message
                                .Row
                                .DynamicProductPlacements
                                .Where(p => !p.IsApplied && !p.IsNew() && p.Qty > 0)
                        );
                }

                Sender.Tell(
                    dynamicProductPlacementRowRepository
                        .GetById(
                            message.Row.Id
                        )
                );
            } catch (Exception exc) {
                Sender.Tell(exc);
            }
        });

        Receive<RemoveDynamicProductPlacementRowByNetIdMessage>(message => {
            try {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                IDynamicProductPlacementRowRepository rowRepository = _supplyUkraineRepositoriesFactory.NewDynamicProductPlacementRowRepository(connection);

                DynamicProductPlacementRow row = rowRepository.GetByNetId(message.NetId);

                if (row == null) throw new Exception("DynamicProductPlacementRow with provided NetId does not exists in database");

                if (row.DynamicProductPlacements.Any(p => p.IsApplied)) throw new Exception("Current row contains applied placements, it can not be deleted");

                rowRepository.RemoveById(row.Id);

                Sender.Tell(new { IsSuccess = true });
            } catch (Exception exc) {
                Sender.Tell(exc);
            }
        });
    }
}