using System.Data;
using System.Text.RegularExpressions;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Messages.Products;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Services.Actors.Products.ProductsGetActors;

public sealed class GetProductsAdvancedSearchActor : ReceiveActor {
    private static readonly Regex SpecialCharactersReplace = new("[$&+,:;=?@#|/\\\\'\"�<>. ^*()%!\\-]", RegexOptions.Compiled);
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public GetProductsAdvancedSearchActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<GetProductsFromAdvancedSearchWithDynamicPricesCalculated>(ProcessGetProductsFromAdvancedSearchWithDynamicPricesCalculated);
    }

    private void ProcessGetProductsFromAdvancedSearchWithDynamicPricesCalculated(GetProductsFromAdvancedSearchWithDynamicPricesCalculated message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ClientAgreement clientAgreement =
            _clientRepositoriesFactory
                .NewClientAgreementRepository(connection)
                .GetByNetIdWithAgreementAndOrganization(
                    message.NetId
                );

        Sender.Tell(
            _productRepositoriesFactory
                .NewGetMultipleProductsRepository(connection)
                .GetAllFromAdvancedSearch(
                    message.Value,
                    message.Limit,
                    message.Offset,
                    message.NetId,
                    message.Mode,
                    message.SortMode,
                    clientAgreement != null,
                    clientAgreement?.Agreement?.OrganizationId,
                    clientAgreement?.Agreement?.WithVATAccounting ?? false
                )
        );
    }
}