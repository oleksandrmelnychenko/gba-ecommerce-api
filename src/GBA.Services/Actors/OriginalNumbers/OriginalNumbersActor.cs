using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Messages.OriginalNumbers;
using GBA.Domain.Repositories.OriginalNumbers.Contracts;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Services.Actors.OriginalNumbers;

public sealed class OriginalNumbersActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IOriginalNumberRepositoryFactory _originalNumberRepositoryFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public OriginalNumbersActor(
        IDbConnectionFactory connectionFactory,
        IOriginalNumberRepositoryFactory originalNumberRepositoryFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _originalNumberRepositoryFactory = originalNumberRepositoryFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<AddOriginalNumberMessage>(ProcessAddOriginalNumberMessage);

        Receive<GetAllOriginalNumbersMessage>(ProcessGetAllOriginalNumbersMessage);

        Receive<GetOriginalNumberByNetIdMessage>(ProcessGetOriginalNumberByNetIdMessage);

        Receive<UpdateOriginalNumberMessage>(ProcessUpdateOriginalNumberMessage);

        Receive<DeleteOriginalNumberMessage>(ProcessDeleteOriginalNumberMessage);
    }

    private void ProcessAddOriginalNumberMessage(AddOriginalNumberMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IOriginalNumberRepository originalNumberRepository =
            _originalNumberRepositoryFactory.NewOriginalNumberRepository(connection);
        IProductOriginalNumberRepository productOriginalNumberRepository =
            _productRepositoriesFactory.NewProductOriginalNumberRepository(connection);
        IProductRepository productRepository =
            _productRepositoriesFactory.NewProductRepository(connection);
        IGetSingleProductRepository getSingleProductRepository =
            _productRepositoriesFactory.NewGetSingleProductRepository(connection);

        Product product = getSingleProductRepository.GetByNetId(message.ProductNetId);

        if (message.IsMain) {
            ProductOriginalNumber productOriginalNumber = productOriginalNumberRepository.GetMainByProductId(product.Id);

            if (productOriginalNumber is { IsMainOriginalNumber: true }) {
                productOriginalNumber.IsMainOriginalNumber = false;

                productOriginalNumberRepository.Update(productOriginalNumber);
            }

            product.MainOriginalNumber = message.OriginalNumber.Number;

            productRepository.UpdateMainOriginalNumber(product.MainOriginalNumber, product.NetUid);
        }

        long originalNumberId = originalNumberRepository.Add(message.OriginalNumber);

        productOriginalNumberRepository.Add(new ProductOriginalNumber {
            Created = DateTime.Now,
            Updated = DateTime.Now,
            OriginalNumberId = originalNumberId,
            ProductId = product.Id,
            IsMainOriginalNumber = message.IsMain
        });

        Sender.Tell(productOriginalNumberRepository.GetByProductId(product.Id));
    }

    private void ProcessGetAllOriginalNumbersMessage(GetAllOriginalNumbersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_originalNumberRepositoryFactory.NewOriginalNumberRepository(connection).GetAll());
    }

    private void ProcessGetOriginalNumberByNetIdMessage(GetOriginalNumberByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_originalNumberRepositoryFactory.NewOriginalNumberRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessUpdateOriginalNumberMessage(UpdateOriginalNumberMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IOriginalNumberRepository originalNumberRepository =
            _originalNumberRepositoryFactory.NewOriginalNumberRepository(connection);
        IProductOriginalNumberRepository productOriginalNumberRepository =
            _productRepositoriesFactory.NewProductOriginalNumberRepository(connection);
        IProductRepository productRepository =
            _productRepositoriesFactory.NewProductRepository(connection);
        IGetSingleProductRepository getSingleProductRepository =
            _productRepositoriesFactory.NewGetSingleProductRepository(connection);

        Product product = getSingleProductRepository.GetByNetId(message.ProductNetId);

        if (message.IsMain) {
            ProductOriginalNumber productOriginalNumber = productOriginalNumberRepository.GetMainByProductId(product.Id);

            if (productOriginalNumber is { IsMainOriginalNumber: true }) {
                productOriginalNumber.IsMainOriginalNumber = false;

                productOriginalNumberRepository.Update(productOriginalNumber);
            }

            product.MainOriginalNumber = message.OriginalNumber.Number;

            productRepository.UpdateMainOriginalNumber(product.MainOriginalNumber, product.NetUid);
        }

        ProductOriginalNumber currentOriginalNumber =
            productOriginalNumberRepository.GetByProductAndNumberId(product.Id, message.OriginalNumber.Id);

        if (currentOriginalNumber != null) {
            OriginalNumber newOriginalNumber = new() {
                MainNumber = message.OriginalNumber.MainNumber,
                Number = message.OriginalNumber.Number
            };

            newOriginalNumber.Id = originalNumberRepository.Add(newOriginalNumber);
            currentOriginalNumber.OriginalNumberId = newOriginalNumber.Id;

            currentOriginalNumber.IsMainOriginalNumber = message.IsMain;
            productOriginalNumberRepository.Update(currentOriginalNumber);
        } else {
            OriginalNumber newOriginalNumber = new() {
                MainNumber = message.OriginalNumber.MainNumber,
                Number = message.OriginalNumber.Number
            };

            newOriginalNumber.Id = originalNumberRepository.Add(newOriginalNumber);

            ProductOriginalNumber productOriginalNumber = new() {
                ProductId = product.Id,
                OriginalNumberId = newOriginalNumber.Id,
                IsMainOriginalNumber = message.IsMain
            };
        }

        Sender.Tell(productOriginalNumberRepository.GetByProductId(product.Id));
    }

    private void ProcessDeleteOriginalNumberMessage(DeleteOriginalNumberMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IOriginalNumberRepository originalNumberRepository =
            _originalNumberRepositoryFactory.NewOriginalNumberRepository(connection);

        Product product = _productRepositoriesFactory
            .NewGetSingleProductRepository(connection)
            .GetByNetId(message.ProductNetId);

        OriginalNumber originalNumber = originalNumberRepository.GetByNetId(message.NetId);

        if (originalNumber != null)
            _productRepositoriesFactory
                .NewProductOriginalNumberRepository(connection)
                .RemoveByOriginalNumberId(originalNumber.Id);

        if (product != null) Sender.Tell(_productRepositoriesFactory.NewProductOriginalNumberRepository(connection).GetByProductId(product.Id));
    }
}