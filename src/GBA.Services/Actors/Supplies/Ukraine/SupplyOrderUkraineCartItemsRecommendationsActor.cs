using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers.SupplyOrders;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;

namespace GBA.Services.Actors.Supplies.Ukraine;

public sealed class SupplyOrderUkraineCartItemsRecommendationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;

    public SupplyOrderUkraineCartItemsRecommendationsActor(
        IDbConnectionFactory connectionFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<string>(_ => {
            try {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);

                List<SupplyOrderUkraineCartItem> ukraineCartItems = new();

                long storageIdPl = storageRepository.GetIdByLocale("pl");
                long storageIdUk = storageRepository.GetIdByLocale("uk");

                if (storageIdPl == 0 || storageIdUk == 0) {
                    Sender.Tell(ukraineCartItems);
                    return;
                }

                IEnumerable<CartItemRecommendedProduct> cartItemRecommendedProducts =
                    _productRepositoriesFactory
                        .NewProductAvailabilityCartLimitsRepository(connection)
                        .GetAll(storageIdPl, storageIdUk);

                IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);

                ukraineCartItems.AddRange(from recommendedProduct in cartItemRecommendedProducts
                    let product = getSingleProductRepository.GetById(recommendedProduct.ProductId)
                    where product != null
                    select new SupplyOrderUkraineCartItem {
                        UploadedQty = recommendedProduct.Qty,
                        FromDate = DateTime.UtcNow,
                        ItemPriority = SupplyOrderUkraineCartItemPriority.High,
                        Product = product,
                        ProductId = product.Id
                    });

                Sender.Tell(ukraineCartItems);
            } catch (Exception exc) {
                Sender.Tell(exc);
            }
        });
    }
}