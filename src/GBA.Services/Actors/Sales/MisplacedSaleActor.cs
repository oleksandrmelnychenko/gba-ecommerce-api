using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Sales.MisplacedSales;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Services.Actors.Sales;

public sealed class MisplacedSaleActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    public MisplacedSaleActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;

        Receive<GetMisplacedSaleBySaleNetIdMessage>(ProcessGetMisplacedSaleBySaleNetIdMessage);

        Receive<GetAllMisplacedSalesMessage>(ProcessGetAllMisplacedSalesMessage);

        Receive<UpdateMisplacedSaleAndReturnAllMessage>(ProcessUpdateMisplacedSaleAndReturnAllMessage);
    }

    private void ProcessUpdateMisplacedSaleAndReturnAllMessage(UpdateMisplacedSaleAndReturnAllMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IMisplacedSaleRepository misplacedSaleRepository = _saleRepositoriesFactory.NewMisplacedSaleRepository(connection);

        misplacedSaleRepository.Update(message.MisplacedSale);

        Sender.Tell(misplacedSaleRepository.GetAll());
    }

    private void ProcessGetAllMisplacedSalesMessage(GetAllMisplacedSalesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_saleRepositoriesFactory.NewMisplacedSaleRepository(connection).GetAllFiltered(
            message.Phone,
            message.From,
            message.To,
            message.IsAccepted,
            message.NetId));
    }

    private void ProcessGetMisplacedSaleBySaleNetIdMessage(GetMisplacedSaleBySaleNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _saleRepositoriesFactory.NewMisplacedSaleRepository(connection)
                .GetBySaleNetId(message.NetId)
        );
    }
}