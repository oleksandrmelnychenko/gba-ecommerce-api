using System;
using System.Data;
using System.Globalization;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Messages.Consumables.Products;
using GBA.Domain.Repositories.Consumables.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Services.Actors.Consumables;

public sealed class ConsumableProductActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsumablesRepositoriesFactory _consumablesRepositoriesFactory;

    public ConsumableProductActor(
        IDbConnectionFactory connectionFactory,
        IConsumablesRepositoriesFactory consumablesRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _consumablesRepositoriesFactory = consumablesRepositoriesFactory;

        Receive<AddNewConsumableProductMessage>(message => {
            if (message.ConsumableProduct == null) {
                Sender.Tell(new Tuple<ConsumableProduct, string>(null, "ConsumableProduct entity can not be empty"));
            } else if ((message.ConsumableProduct.ConsumableProductCategory == null || message.ConsumableProduct.ConsumableProductCategory.IsNew()) &&
                       message.ConsumableProduct.ConsumableProductCategoryId.Equals(0)) {
                Sender.Tell(new Tuple<ConsumableProduct, string>(null, ConsumableProductResourceNames.CONSUMABLE_PRODUCT_CATEGORY_NOT_SPECIFIED));
            } else {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                IConsumableProductRepository consumableProductRepository = _consumablesRepositoriesFactory.NewConsumableProductRepository(connection);

                if (message.ConsumableProduct.ConsumableProductCategory != null)
                    message.ConsumableProduct.ConsumableProductCategoryId = message.ConsumableProduct.ConsumableProductCategory.Id;
                if (message.ConsumableProduct.MeasureUnit != null) message.ConsumableProduct.MeasureUnitId = message.ConsumableProduct.MeasureUnit.Id;

                ConsumableProduct lastRecord = consumableProductRepository.GetLastRecord();

                message.ConsumableProduct.VendorCode =
                    lastRecord != null
                        ? string.Format("{0:D3}", Convert.ToInt32(lastRecord.VendorCode) + 1)
                        : string.Format("{0:D3}", 1);

                message.ConsumableProduct.Id = consumableProductRepository.Add(message.ConsumableProduct);

                _consumablesRepositoriesFactory.NewConsumableProductTranslationRepository(connection).Add(new ConsumableProductTranslation {
                    ConsumableProductId = message.ConsumableProduct.Id,
                    Name = message.ConsumableProduct.Name,
                    CultureCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                });

                Sender.Tell(new Tuple<ConsumableProduct, string>(consumableProductRepository.GetById(message.ConsumableProduct.Id), string.Empty));
            }
        });

        Receive<GetAllFromSearchByVendorCodeMessage>(message => {
            if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewConsumableProductRepository(connection).GetAllFromSearchByVendorCode(message.Value));
        });

        Receive<UpdateConsumableProductMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IConsumableProductRepository consumableProductRepository = _consumablesRepositoriesFactory.NewConsumableProductRepository(connection);
            IConsumableProductTranslationRepository consumableProductTranslationRepository =
                _consumablesRepositoriesFactory.NewConsumableProductTranslationRepository(connection);

            if (message.ConsumableProduct.MeasureUnit != null) message.ConsumableProduct.MeasureUnitId = message.ConsumableProduct.MeasureUnit.Id;

            consumableProductRepository.Update(message.ConsumableProduct);

            ConsumableProductTranslation translation = consumableProductTranslationRepository.GetByConsumableProductId(message.ConsumableProduct.Id);

            if (translation != null) {
                translation.Name = message.ConsumableProduct.Name;

                consumableProductTranslationRepository.Update(translation);
            } else {
                consumableProductTranslationRepository.Add(new ConsumableProductTranslation {
                    ConsumableProductId = message.ConsumableProduct.Id,
                    Name = message.ConsumableProduct.Name,
                    CultureCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                });
            }

            Sender.Tell(consumableProductRepository.GetById(message.ConsumableProduct.Id));
        });

        Receive<GetAllConsumableProductsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewConsumableProductRepository(connection).GetAll());
        });

        Receive<GetConsumableProductByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewConsumableProductRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeleteConsumableProductByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _consumablesRepositoriesFactory.NewConsumableProductRepository(connection).Remove(message.NetId);
        });

        Receive<AddNewSupplyServiceConsumableProductMessage>(ProcessAddNewSupplyServiceConsumableProduct);
    }

    private void ProcessAddNewSupplyServiceConsumableProduct(AddNewSupplyServiceConsumableProductMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IConsumableProductRepository consumableProductRepository =
                _consumablesRepositoriesFactory.NewConsumableProductRepository(connection);
            IConsumableProductCategoryRepository consumableProductCategoryRepository =
                _consumablesRepositoriesFactory.NewConsumableProductCategoryRepository(connection);

            ConsumableProductCategory supplyServiceConsumableCategory =
                consumableProductCategoryRepository.GetConsumableCategoriesSupplyServiceIfExist();

            if (supplyServiceConsumableCategory == null) {
                supplyServiceConsumableCategory = new ConsumableProductCategory {
                    IsSupplyServiceCategory = true,
                    Name = "�������"
                };
                supplyServiceConsumableCategory.Id =
                    consumableProductCategoryRepository.Add(supplyServiceConsumableCategory);
            }

            message.ConsumableProduct.ConsumableProductCategoryId = supplyServiceConsumableCategory.Id;

            if (message.ConsumableProduct.MeasureUnit != null)
                message.ConsumableProduct.MeasureUnitId = message.ConsumableProduct.MeasureUnit.Id;

            consumableProductRepository.Add(message.ConsumableProduct);

            Sender.Tell(
                consumableProductRepository
                    .GetAllFromSearchByVendorCode(string.Empty));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}