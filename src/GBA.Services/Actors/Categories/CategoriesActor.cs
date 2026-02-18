using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Categories;
using GBA.Domain.Repositories.Categories.Contracts;

namespace GBA.Services.Actors.Categories;

public sealed class CategoriesActor : ReceiveActor {
    private readonly ICategoryRepositoryFactory _categoryRepositoryFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public CategoriesActor(
        IDbConnectionFactory connectionFactory,
        ICategoryRepositoryFactory categoryRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _categoryRepositoryFactory = categoryRepositoryFactory;

        Receive<AddCategoryMessage>(ProcessAddCategoryMessage);

        Receive<UpdateCategoryMessage>(ProcessUpdateCategoryMessage);

        Receive<DeleteCategoryMessage>(ProcessDeleteCategoryMessage);
    }

    private void ProcessAddCategoryMessage(AddCategoryMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICategoryRepository categoryRepository = _categoryRepositoryFactory.NewCategoryRepository(connection);

        Sender.Tell(categoryRepository.GetById(categoryRepository.Add(message.Category)));
    }

    private void ProcessUpdateCategoryMessage(UpdateCategoryMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICategoryRepository categoryRepository = _categoryRepositoryFactory.NewCategoryRepository(connection);

        categoryRepository.Update(message.Category);

        Sender.Tell(categoryRepository.GetByNetId(message.Category.NetUid));
    }

    private void ProcessDeleteCategoryMessage(DeleteCategoryMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _categoryRepositoryFactory.NewCategoryRepository(connection).Remove(message.NetId);
    }
}