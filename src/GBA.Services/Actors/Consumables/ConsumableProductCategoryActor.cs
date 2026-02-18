using System.Data;
using System.Globalization;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Consumables.Categories;
using GBA.Domain.Repositories.Consumables.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Services.Actors.Consumables;

public sealed class ConsumableProductCategoryActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsumablesRepositoriesFactory _consumablesRepositoriesFactory;

    public ConsumableProductCategoryActor(
        IDbConnectionFactory connectionFactory,
        IConsumablesRepositoriesFactory consumablesRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _consumablesRepositoriesFactory = consumablesRepositoriesFactory;

        Receive<AddNewConsumableProductCategoryMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IConsumableProductCategoryRepository consumableProductCategoryRepository = _consumablesRepositoriesFactory.NewConsumableProductCategoryRepository(connection);

            if (message.ConsumableProductCategory.IsSupplyServiceCategory) {
                bool isExistCategorySupplyService = consumableProductCategoryRepository.IsCategoryForSupplyService();

                if (isExistCategorySupplyService)
                    consumableProductCategoryRepository.UpdateAllCategorySupplyService();
            }

            message.ConsumableProductCategory.Id = consumableProductCategoryRepository.Add(message.ConsumableProductCategory);

            _consumablesRepositoriesFactory.NewConsumableProductCategoryTranslationRepository(connection).Add(new ConsumableProductCategoryTranslation {
                ConsumableProductCategoryId = message.ConsumableProductCategory.Id,
                CultureCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Name = message.ConsumableProductCategory.Name,
                Description = message.ConsumableProductCategory.Description
            });

            Sender.Tell(consumableProductCategoryRepository.GetById(message.ConsumableProductCategory.Id));
        });

        Receive<UpdateConsumableProductCategoryMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IConsumableProductCategoryRepository consumableProductCategoryRepository = _consumablesRepositoriesFactory.NewConsumableProductCategoryRepository(connection);
            IConsumableProductCategoryTranslationRepository consumableProductCategoryTranslationRepository =
                _consumablesRepositoriesFactory.NewConsumableProductCategoryTranslationRepository(connection);

            if (message.ConsumableProductCategory.IsSupplyServiceCategory) {
                bool isExistCategorySupplyService = consumableProductCategoryRepository.IsCategoryForSupplyService();

                if (isExistCategorySupplyService)
                    consumableProductCategoryRepository.UpdateAllCategorySupplyService();
            }

            consumableProductCategoryRepository.Update(message.ConsumableProductCategory);

            ConsumableProductCategoryTranslation translation =
                consumableProductCategoryTranslationRepository.GetByConsumableProductCategoryId(message.ConsumableProductCategory.Id);

            if (translation != null) {
                translation.Name = message.ConsumableProductCategory.Name;
                translation.Description = message.ConsumableProductCategory.Description;

                consumableProductCategoryTranslationRepository.Update(translation);
            } else {
                consumableProductCategoryTranslationRepository.Add(new ConsumableProductCategoryTranslation {
                    ConsumableProductCategoryId = message.ConsumableProductCategory.Id,
                    CultureCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    Name = message.ConsumableProductCategory.Name,
                    Description = message.ConsumableProductCategory.Description
                });
            }

            Sender.Tell(consumableProductCategoryRepository.GetById(message.ConsumableProductCategory.Id));
        });

        Receive<GetAllConsumableProductCategoriesMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewConsumableProductCategoryRepository(connection).GetAll());
        });

        Receive<GetAllConsumableProductCategoriesFromSearchMessage>(message => {
            if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewConsumableProductCategoryRepository(connection).GetAllFromSearch(message.Value));
        });

        Receive<GetConsumableProductCategoryByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewConsumableProductCategoryRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeleteConsumableProductCategoryByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _consumablesRepositoriesFactory.NewConsumableProductCategoryRepository(connection).Remove(message.NetId);
        });

        Receive<GetConsumableProductCategoriesSupplyServicesMessage>(ProcessGetConsumableProductCategoriesSupplyServices);
    }

    private void ProcessGetConsumableProductCategoriesSupplyServices(GetConsumableProductCategoriesSupplyServicesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_consumablesRepositoriesFactory.NewConsumableProductCategoryRepository(connection).GetConsumableProductCategoriesSupplyService(message.Value));
    }
}