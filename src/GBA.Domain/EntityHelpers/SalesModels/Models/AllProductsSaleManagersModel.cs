using System;
using System.Collections.Generic;
using GBA.Common.Helpers;

namespace GBA.Domain.EntityHelpers.SalesModels.Models;

public sealed class AllProductsSaleManagersModel {
    public AllProductsSaleManagersModel() {
        Products = new List<ProductsSalesByManagersModel>();

        Managers = new List<ManagerWithTotalValueSoldModel> {
            new() {
                ManagerName = "Інтернет замовлення",
                NetId = Guid.NewGuid(),
                TypeOrder = OrderSource.Shop
            },
            new() {
                ManagerName = "Інтернет пропозиція",
                NetId = Guid.NewGuid(),
                TypeOrder = OrderSource.Offer
            }
        };
    }

    public List<ProductsSalesByManagersModel> Products { get; }

    public List<ManagerWithTotalValueSoldModel> Managers { get; }
}