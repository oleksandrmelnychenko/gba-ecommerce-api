using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Products;
using GBA.Domain.Messages.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Services.Actors.Products;

public sealed class ProductGroupsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public ProductGroupsActor(
        IDbConnectionFactory connectionFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<AddProductGroupMessage>(ProcessAddProductGroupMessage);

        Receive<UpdateProductGroupMessage>(ProcessUpdateProductGroupMessage);

        Receive<GetAllProductGroupsMessage>(ProcessGetAllProductGroupsMessage);

        Receive<GetProductGroupByNetIdMessage>(ProcessGetProductGroupByNetIdMessage);

        Receive<DeleteProductGroupMessage>(ProcessDeleteProductGroupMessage);

        Receive<GetAllProductGroupsByProductNetIdMessage>(ProcessGetAllProductGroupsByProductNetIdMessage);

        Receive<GetAllProductGroupsFilteredMessage>(ProcessGetAllProductGroupsFiltered);

        Receive<GetWithRootGroupsProductGroupMessage>(ProcessGetWithRootGroupsProductGroup);

        Receive<GetFilteredSubGroupsProductGroupMessage>(ProcessGetFilteredSubGroupsProductGroup);

        Receive<GetRootProductGroupsMessage>(ProcessGetRootProductGroups);

        Receive<UpdateProductGroupWithContentMessage>(ProcessUpdateProductGroupWithContent);

        Receive<GetFilteredProductsByProductGroupNetIdMessage>(ProcessGetFilteredProductsByProductGroupNetId);
    }

    private void ProcessAddProductGroupMessage(AddProductGroupMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IProductGroupRepository productGroupRepository = _productRepositoriesFactory.NewProductGroupRepository(connection);

        message.ProductGroup.Id = productGroupRepository.Add(message.ProductGroup);

        if (message.ProductGroup.SubProductGroups.Any()) {
            foreach (ProductSubGroup productSubGroup in message.ProductGroup.SubProductGroups)
                if (productSubGroup.SubProductGroup != null) {
                    productSubGroup.RootProductGroupId = message.ProductGroup.Id;

                    productSubGroup.SubProductGroupId = productSubGroup.SubProductGroup.IsNew()
                        ? _productRepositoriesFactory.NewProductGroupRepository(connection).Add(productSubGroup.SubProductGroup)
                        : productSubGroup.SubProductGroup.Id;
                }

            _productRepositoriesFactory.NewProductSubGroupRepository(connection).Add(message.ProductGroup.SubProductGroups);
        }

        if (message.ProductGroup.RootProductGroups.Any()) {
            foreach (ProductSubGroup rootProductGroup in message.ProductGroup.RootProductGroups.Where(x => x.IsNew())) {
                if (rootProductGroup.RootProductGroup == null) continue;

                rootProductGroup.RootProductGroupId = rootProductGroup.RootProductGroup.Id;
                rootProductGroup.SubProductGroupId = message.ProductGroup.Id;
            }

            productGroupRepository.SetIsSubGroup(message.ProductGroup.Id);

            _productRepositoriesFactory
                .NewProductSubGroupRepository(connection)
                .Add(message.ProductGroup.RootProductGroups.Where(x => x.IsNew()));
        }

        Sender.Tell(productGroupRepository.GetById(message.ProductGroup.Id));
    }

    private void ProcessUpdateProductGroupMessage(UpdateProductGroupMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IProductGroupRepository productGroupRepository = _productRepositoriesFactory.NewProductGroupRepository(connection);

        if (message.ProductGroup.SubProductGroups.Any()) {
            foreach (ProductSubGroup productSubGroup in message.ProductGroup.SubProductGroups)
                if (productSubGroup.SubProductGroup != null) {
                    productSubGroup.RootProductGroupId = message.ProductGroup.Id;

                    if (productSubGroup.SubProductGroup.IsNew()) {
                        productSubGroup.SubProductGroupId = _productRepositoriesFactory.NewProductGroupRepository(connection).Add(productSubGroup.SubProductGroup);
                    } else {
                        productSubGroup.SubProductGroupId = productSubGroup.SubProductGroup.Id;
                        productGroupRepository.Update(productSubGroup.SubProductGroup);
                    }
                }

            _productRepositoriesFactory.NewProductSubGroupRepository(connection).Update(message.ProductGroup.SubProductGroups);
        }

        productGroupRepository.Update(message.ProductGroup);

        Sender.Tell(productGroupRepository.GetByNetId(message.ProductGroup.NetUid));
    }

    private void ProcessGetAllProductGroupsMessage(GetAllProductGroupsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_productRepositoriesFactory.NewProductGroupRepository(connection).GetAll());
    }

    private void ProcessGetProductGroupByNetIdMessage(GetProductGroupByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_productRepositoriesFactory.NewProductGroupRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessDeleteProductGroupMessage(DeleteProductGroupMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _productRepositoriesFactory.NewProductGroupRepository(connection).Remove(message.NetId);
    }

    private void ProcessGetAllProductGroupsByProductNetIdMessage(GetAllProductGroupsByProductNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_productRepositoriesFactory.NewProductGroupRepository(connection).GetAllByProductNetId(message.ProductNetId));
    }

    private void ProcessGetAllProductGroupsFiltered(GetAllProductGroupsFilteredMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _productRepositoriesFactory
                    .NewProductGroupRepository(connection)
                    .GetAllFiltered(message.Value));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetWithRootGroupsProductGroup(GetWithRootGroupsProductGroupMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _productRepositoriesFactory
                    .NewProductGroupRepository(connection)
                    .GetByNetIdWithRootGroups(message.NetId));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetFilteredSubGroupsProductGroup(GetFilteredSubGroupsProductGroupMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _productRepositoriesFactory
                    .NewProductGroupRepository(connection)
                    .GetFilteredSubGroupsProductGroup(
                        message.NetId,
                        message.Limit,
                        message.Offset,
                        message.Value));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetRootProductGroups(GetRootProductGroupsMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _productRepositoriesFactory
                    .NewProductGroupRepository(connection)
                    .GetRootProductGroupsByNetId(
                        message.NetId));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateProductGroupWithContent(UpdateProductGroupWithContentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IProductGroupRepository productGroupRepository =
                _productRepositoriesFactory.NewProductGroupRepository(connection);
            IProductSubGroupRepository productSubGroupRepository =
                _productRepositoriesFactory.NewProductSubGroupRepository(connection);

            if (message.ProductGroup.SubProductGroups.Any()) {
                if (message.ProductGroup.SubProductGroups.Any(x => x.IsNew())) {
                    foreach (ProductSubGroup subGroup in message.ProductGroup.SubProductGroups
                                 .Where(x => x.IsNew())) {
                        subGroup.RootProductGroupId = message.ProductGroup.Id;
                        subGroup.SubProductGroupId = subGroup.SubProductGroup.Id;
                    }

                    productSubGroupRepository.Add(message.ProductGroup.SubProductGroups
                        .Where(x => x.IsNew()));
                }

                if (message.ProductGroup.SubProductGroups.Any(x => x.Deleted.Equals(true)))
                    productSubGroupRepository.Remove(message.ProductGroup.SubProductGroups
                        .Where(x => x.Deleted.Equals(true)));
            }

            if (message.ProductGroup.RootProductGroups.Any()) {
                if (message.ProductGroup.RootProductGroups.Any(x => x.IsNew())) {
                    foreach (ProductSubGroup rootGroup in message.ProductGroup.RootProductGroups
                                 .Where(x => x.IsNew())) {
                        ProductSubGroup existProductGroup =
                            productSubGroupRepository.GetByRootAndSubIds(rootGroup.RootProductGroup.Id, message.ProductGroup.Id);

                        if (existProductGroup != null) {
                            if (existProductGroup.Deleted) productSubGroupRepository.Restore(existProductGroup.Id);
                        } else {
                            rootGroup.RootProductGroupId = rootGroup.RootProductGroup.Id;
                            rootGroup.SubProductGroupId = message.ProductGroup.Id;

                            productSubGroupRepository.Add(rootGroup);
                        }
                    }

                    message.ProductGroup.IsSubGroup = true;
                } else {
                    message.ProductGroup.IsSubGroup = false;
                }

                if (message.ProductGroup.RootProductGroups.Any(x => x.Deleted.Equals(true)))
                    productSubGroupRepository.RemoveAllByIds(message.ProductGroup.RootProductGroups
                        .Where(x => x.Deleted.Equals(true)).Select(x => x.Id));
            }

            productGroupRepository.Update(message.ProductGroup);

            Sender.Tell(
                productGroupRepository
                    .GetByNetIdWithRootGroups(message.ProductGroup.NetUid)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetFilteredProductsByProductGroupNetId(GetFilteredProductsByProductGroupNetIdMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _productRepositoriesFactory
                    .NewProductGroupRepository(connection)
                    .GetFilteredProductByProductGroupNetId(
                        message.NetId,
                        message.Limit,
                        message.Offset,
                        message.Value));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}