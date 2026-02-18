using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Categories;
using GBA.Domain.Repositories.Categories.Contracts;

namespace GBA.Services.Actors.Categories.CategoriesGetActors;

public sealed class BaseCategoriesGetActor : ReceiveActor {
    private readonly ICategoryRepositoryFactory _categoryRepositoryFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public BaseCategoriesGetActor(
        IDbConnectionFactory connectionFactory,
        ICategoryRepositoryFactory categoryRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _categoryRepositoryFactory = categoryRepositoryFactory;

        Receive<GetAllCategoriesMessage>(ProcessGetAllCategoriesMessage);

        Receive<GetCategoryByNetIdMessage>(ProcessGetCategoryByNetIdMessage);
    }

    private void ProcessGetAllCategoriesMessage(GetAllCategoriesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_categoryRepositoryFactory.NewCategoryRepository(connection).GetAll());
    }

    private void ProcessGetCategoryByNetIdMessage(GetCategoryByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_categoryRepositoryFactory.NewCategoryRepository(connection).GetByNetId(message.NetId));
    }
}