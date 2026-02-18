using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Messages.SchedulerTasks;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Services.Actors.SchedulerTasks;

public sealed class ClearClientAgreementsNumberDayDebtActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    public ClearClientAgreementsNumberDayDebtActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;

        ReceiveAsync<ClearClientAgreementsNumberDayDebtMessage>(ClearClientAgreementsNumberDayDebtMessageAsync);
    }

    private async Task ClearClientAgreementsNumberDayDebtMessageAsync(ClearClientAgreementsNumberDayDebtMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        List<Sale> sales2 =
            _saleRepositoriesFactory
                .NewSaleRepository(connection)
                .GetAllRanged(
                    DateTime.Now.Date.AddMonths(-1),
                    DateTime.Now
                );
        foreach (Sale sale in sales2)
            if (sale.IsVatSale && sale.IsAcceptedToPacking && sale.ClientAgreement.Agreement.NumberDaysDebt != 0) {
                DateTime daysPeriod = sale.Created.AddDays(sale.ClientAgreement.Agreement.NumberDaysDebt);
                DateTime now = DateTime.Now;
                bool isVatSale = daysPeriod.Day <= now.Day && daysPeriod.Month <= now.Month;
                if (isVatSale) saleRepository.SetIsAcceptedToPackingFalse(sale.Id);
            }
    }
}
