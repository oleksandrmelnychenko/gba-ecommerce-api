using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Sales.Debts;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Services.Actors.Sales;

public sealed class DebtsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    public DebtsActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;

        Receive<AddDebtMessage>(ProcessAddDebtMessage);

        Receive<GetAllDebtsMessage>(ProcessGetAllDebtsMessage);

        Receive<UpdateDebtMessage>(ProcessUpdateDebtMessage);

        Receive<GetDebtByNetIdMessage>(ProcessGetDebtByNetIdMessage);

        Receive<GetTopByAllClientsMessage>(ProcessGetTopByAllClientsMessage);

        Receive<GetTopByManagersMessage>(ProcessGetTopByManagersMessage);
    }

    private void ProcessAddDebtMessage(AddDebtMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDebtRepository debtRepository = _saleRepositoriesFactory.NewDebtRepository(connection);

        Sender.Tell(debtRepository.GetById(debtRepository.Add(message.Debt)));
    }

    private void ProcessGetAllDebtsMessage(GetAllDebtsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_saleRepositoriesFactory.NewDebtRepository(connection).GetAll());
    }

    private void ProcessUpdateDebtMessage(UpdateDebtMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDebtRepository debtRepository = _saleRepositoriesFactory.NewDebtRepository(connection);

        debtRepository.Update(message.Debt);

        Sender.Tell(debtRepository.GetByNetId(message.Debt.NetUid));
    }

    private void ProcessGetDebtByNetIdMessage(GetDebtByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_saleRepositoriesFactory.NewDebtRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessGetTopByAllClientsMessage(GetTopByAllClientsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<dynamic> topResult = _saleRepositoriesFactory.NewDebtRepository(connection).GetTopByAllClients();
        dynamic[] toReturnData = new dynamic[topResult.Count];

        for (int i = 0; i < topResult.Count; i++) {
            dynamic top = new ExpandoObject();

            top.FullName = topResult[i].FullName;
            top.TotalAmount = topResult[i].TotalAmount;
            top.Color = ChartColors.COLORS[i];

            toReturnData[i] = top;
        }

        Sender.Tell(toReturnData);
    }

    private void ProcessGetTopByManagersMessage(GetTopByManagersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<dynamic> topResult = _saleRepositoriesFactory.NewDebtRepository(connection).GetTopByManagers();
        dynamic[] toReturnData = new dynamic[topResult.Count];

        for (int i = 0; i < topResult.Count; i++) {
            dynamic top = new ExpandoObject();

            top.LastName = topResult[i].LastName;
            top.TotalAmount = topResult[i].TotalAmount;
            top.Color = ChartColors.COLORS[i];

            toReturnData[i] = top;
        }

        Sender.Tell(toReturnData);
    }
}