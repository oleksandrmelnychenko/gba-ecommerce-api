using System;
using System.Data;
using System.Globalization;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Messages.Products.ProductWriteOffRules;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Products;

public sealed class ProductWriteOffRulesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public ProductWriteOffRulesActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<AddOrUpdateProductWriteOffRuleMessage>(ProcessAddOrUpdateProductWriteOffRuleMessage);

        Receive<GetGeneralProductWriteOffRulesMessage>(ProcessGetGeneralProductWriteOffRulesMessage);

        Receive<GetProductWriteOffRulesByProductNetIdMessage>(ProcessGetProductWriteOffRulesByProductNetIdMessage);

        Receive<GetProductWriteOffRulesByProductGroupNetIdMessage>(ProcessGetProductWriteOffRulesByProductGroupNetIdMessage);

        Receive<DeleteProductWriteOffRuleByNetIdMessage>(ProcessDeleteProductWriteOffRuleByNetIdMessage);
    }

    private void ProcessAddOrUpdateProductWriteOffRuleMessage(AddOrUpdateProductWriteOffRuleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.ProductWriteOffRule != null) {
            IProductWriteOffRuleRepository productWriteOffRuleRepository = _productRepositoriesFactory.NewProductWriteOffRuleRepository(connection);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            message.ProductWriteOffRule.RuleLocale =
                !string.IsNullOrEmpty(message.ProductWriteOffRule.RuleLocale)
                    ? message.ProductWriteOffRule.RuleLocale
                    : CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            if (message.ProductWriteOffRule.Product != null && !message.ProductWriteOffRule.Product.IsNew()) {
                ProductWriteOffRule ruleFromDb =
                    productWriteOffRuleRepository
                        .GetByProductIdAndRuleLocaleIfExists(
                            message.ProductWriteOffRule.Product.Id,
                            message.ProductWriteOffRule.RuleLocale
                        );

                if (ruleFromDb != null) {
                    if (!ruleFromDb.RuleType.Equals(message.ProductWriteOffRule.RuleType)) {
                        ruleFromDb.RuleType = message.ProductWriteOffRule.RuleType;
                        ruleFromDb.UpdatedById = user.Id;

                        productWriteOffRuleRepository
                            .Update(ruleFromDb);
                    }

                    Sender.Tell(
                        new Tuple<ProductWriteOffRule, string>(
                            productWriteOffRuleRepository.GetById(ruleFromDb.Id),
                            string.Empty
                        )
                    );
                } else {
                    message.ProductWriteOffRule.Id =
                        productWriteOffRuleRepository
                            .Add(
                                new ProductWriteOffRule {
                                    CreatedById = user.Id,
                                    ProductId = message.ProductWriteOffRule.Product.Id,
                                    RuleLocale = message.ProductWriteOffRule.RuleLocale,
                                    RuleType = message.ProductWriteOffRule.RuleType
                                }
                            );

                    Sender.Tell(
                        new Tuple<ProductWriteOffRule, string>(
                            productWriteOffRuleRepository.GetById(message.ProductWriteOffRule.Id),
                            string.Empty
                        )
                    );
                }
            } else if (message.ProductWriteOffRule.ProductGroup != null && !message.ProductWriteOffRule.ProductGroup.IsNew()) {
                ProductWriteOffRule ruleFromDb =
                    productWriteOffRuleRepository
                        .GetByProductGroupIdAndRuleLocaleIfExists(
                            message.ProductWriteOffRule.ProductGroup.Id,
                            message.ProductWriteOffRule.RuleLocale
                        );

                if (ruleFromDb != null) {
                    if (!ruleFromDb.RuleType.Equals(message.ProductWriteOffRule.RuleType)) {
                        ruleFromDb.RuleType = message.ProductWriteOffRule.RuleType;
                        ruleFromDb.UpdatedById = user.Id;

                        productWriteOffRuleRepository
                            .Update(ruleFromDb);
                    }

                    Sender.Tell(
                        new Tuple<ProductWriteOffRule, string>(
                            productWriteOffRuleRepository.GetById(ruleFromDb.Id),
                            string.Empty
                        )
                    );
                } else {
                    message.ProductWriteOffRule.Id =
                        productWriteOffRuleRepository
                            .Add(
                                new ProductWriteOffRule {
                                    CreatedById = user.Id,
                                    ProductGroupId = message.ProductWriteOffRule.ProductGroup.Id,
                                    RuleLocale = message.ProductWriteOffRule.RuleLocale,
                                    RuleType = message.ProductWriteOffRule.RuleType
                                }
                            );

                    Sender.Tell(
                        new Tuple<ProductWriteOffRule, string>(
                            productWriteOffRuleRepository.GetById(message.ProductWriteOffRule.Id),
                            string.Empty
                        )
                    );
                }
            } else {
                ProductWriteOffRule ruleFromDb =
                    productWriteOffRuleRepository
                        .GetByRuleLocale(
                            message.ProductWriteOffRule.RuleLocale
                        );

                if (ruleFromDb != null) {
                    if (!ruleFromDb.RuleType.Equals(message.ProductWriteOffRule.RuleType)) {
                        ruleFromDb.RuleType = message.ProductWriteOffRule.RuleType;
                        ruleFromDb.UpdatedById = user.Id;

                        productWriteOffRuleRepository
                            .Update(ruleFromDb);
                    }

                    Sender.Tell(
                        new Tuple<ProductWriteOffRule, string>(
                            productWriteOffRuleRepository.GetById(ruleFromDb.Id),
                            string.Empty
                        )
                    );
                } else {
                    message.ProductWriteOffRule.Id =
                        productWriteOffRuleRepository
                            .Add(
                                new ProductWriteOffRule {
                                    CreatedById = user.Id,
                                    RuleLocale = message.ProductWriteOffRule.RuleLocale,
                                    RuleType = message.ProductWriteOffRule.RuleType
                                }
                            );

                    Sender.Tell(
                        new Tuple<ProductWriteOffRule, string>(
                            productWriteOffRuleRepository.GetById(message.ProductWriteOffRule.Id),
                            string.Empty
                        )
                    );
                }
            }
        } else {
            Sender.Tell(new Tuple<ProductWriteOffRule, string>(null, ProductWriteOffRulesResourceNames.PRODUCT_WRITE_OFF_EMPTY));
        }
    }

    private void ProcessGetGeneralProductWriteOffRulesMessage(GetGeneralProductWriteOffRulesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductWriteOffRuleRepository(connection)
                .GetDefaultRules()
        );
    }

    private void ProcessGetProductWriteOffRulesByProductNetIdMessage(GetProductWriteOffRulesByProductNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductWriteOffRuleRepository(connection)
                .GetByProductNetId(message.ProductNetId)
        );
    }

    private void ProcessGetProductWriteOffRulesByProductGroupNetIdMessage(GetProductWriteOffRulesByProductGroupNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductWriteOffRuleRepository(connection)
                .GetByProductGroupNetId(message.ProductGroupNetId)
        );
    }

    private void ProcessDeleteProductWriteOffRuleByNetIdMessage(DeleteProductWriteOffRuleByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

        _productRepositoriesFactory
            .NewProductWriteOffRuleRepository(connection)
            .Remove(
                message.RuleNetId,
                user.Id
            );
    }
}