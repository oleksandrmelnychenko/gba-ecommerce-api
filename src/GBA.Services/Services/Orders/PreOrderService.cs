using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Services.Services.Orders.Contracts;

namespace GBA.Services.Services.Orders;

public sealed class PreOrderService : IPreOrderService {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    public PreOrderService(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory
    ) {
        _connectionFactory = connectionFactory;

        _saleRepositoriesFactory = saleRepositoriesFactory;

        _clientRepositoriesFactory = clientRepositoriesFactory;
    }

    public Task<PreOrder> AddNewPreOrder(PreOrder preOrder, Guid clientNetId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (preOrder == null) throw new Exception("Entity can not be null");

            if (preOrder.Product == null || preOrder.Product.IsNew()) throw new Exception("Product need to be specified");

            if (!clientNetId.Equals(Guid.Empty)) {
                preOrder.ClientId =
                    _clientRepositoriesFactory
                        .NewClientRepository(connection)
                        .GetByNetIdWithoutIncludes(clientNetId)
                        ?.Id;
            } else {
                if (!string.IsNullOrEmpty(preOrder.MobileNumber))
                    preOrder.ClientId =
                        _clientRepositoriesFactory
                            .NewClientRepository(connection)
                            .SearchClientByMobileNumber(preOrder.MobileNumber)
                            ?.Id;
                else
                    preOrder.ClientId = null;
            }

            preOrder.Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            preOrder.ProductId = preOrder.Product.Id;

            IPreOrderRepository preOrderRepository = _saleRepositoriesFactory.NewPreOrderRepository(connection);

            preOrder.Id = preOrderRepository.Add(preOrder);

            return Task.FromResult(preOrderRepository.GetById(preOrder.Id));
    }
}
