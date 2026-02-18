using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Messages.Consumables.CompanyCarRoadLists;
using GBA.Domain.Repositories.Consumables.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Consumables;

public sealed class CompanyCarRoadListActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsumablesRepositoriesFactory _consumablesRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public CompanyCarRoadListActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IConsumablesRepositoriesFactory consumablesRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _consumablesRepositoriesFactory = consumablesRepositoriesFactory;

        Receive<AddNewCompanyCarRoadListMessage>(message => {
            if (message.CompanyCarRoadList == null) {
                Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, "Empty CompanyCarRoadList is not valid input for current request"));
            } else if (!message.CompanyCarRoadList.IsNew()) {
                Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, "Existing CompanyCarRoadList is not valid input for current request"));
            } else if (message.CompanyCarRoadList.Mileage <= 0) {
                Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, CompanyCarRoadListResourceNames.MILEAGE_NOT_SPECIFIED));
            } else if (message.CompanyCarRoadList.Responsible == null && message.CompanyCarRoadList.ResponsibleId.Equals(0)) {
                Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, CompanyCarRoadListResourceNames.RESPONSIBLE_NOT_SPECIFIED));
            } else if (message.CompanyCarRoadList.CompanyCar == null && message.CompanyCarRoadList.CompanyCarId.Equals(0)) {
                Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, CompanyCarRoadListResourceNames.COMPANY_CAR_NOT_SPECIFIED));
            } else {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                ICompanyCarRepository companyCarRepository = _consumablesRepositoriesFactory.NewCompanyCarRepository(connection);
                ICompanyCarRoadListRepository companyCarRoadListRepository = _consumablesRepositoriesFactory.NewCompanyCarRoadListRepository(connection);

                message.CompanyCarRoadList.ResponsibleId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                if (message.CompanyCarRoadList.CompanyCar != null) message.CompanyCarRoadList.CompanyCarId = message.CompanyCarRoadList.CompanyCar.Id;
                if (message.CompanyCarRoadList.OutcomePaymentOrder != null)
                    message.CompanyCarRoadList.OutcomePaymentOrderId = message.CompanyCarRoadList.OutcomePaymentOrder.Id;

                CompanyCar carFromDb = companyCarRepository.GetById(message.CompanyCarRoadList.CompanyCarId);

                if (carFromDb == null) {
                    Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, CompanyCarRoadListResourceNames.COMPANY_CAR_NOT_EXISTS));
                } else {
                    message.CompanyCarRoadList.TotalKilometers = Convert.ToInt32(message.CompanyCarRoadList.Mileage - carFromDb.Mileage);

                    if (message.CompanyCarRoadList.TotalKilometers < 0) {
                        Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, CompanyCarRoadListResourceNames.MILEAGE_LESS_THAN_EXISTING));
                    } else {
                        message.CompanyCarRoadList.CreatedById = message.CompanyCarRoadList.ResponsibleId;

                        message.CompanyCarRoadList.TotalKilometers = Convert.ToInt32(message.CompanyCarRoadList.Mileage - carFromDb.Mileage);

                        int inModesKilometers = message.CompanyCarRoadList.InCityKilometers + message.CompanyCarRoadList.OutsideCityKilometers +
                                                message.CompanyCarRoadList.MixedModeKilometers;

                        if (message.CompanyCarRoadList.TotalKilometers < inModesKilometers) {
                            Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, CompanyCarRoadListResourceNames.SUM_OF_KILOMITERS_MORE_THAN_IN_MODES_SUM));
                        } else {
                            if (message.CompanyCarRoadList.InCityKilometers.Equals(0) && message.CompanyCarRoadList.OutsideCityKilometers.Equals(0) &&
                                message.CompanyCarRoadList.MixedModeKilometers.Equals(0)) {
                                message.CompanyCarRoadList.FuelAmount = Math.Round(message.CompanyCarRoadList.TotalKilometers * carFromDb.InCityConsumption / 100, 2);
                            } else {
                                message.CompanyCarRoadList.FuelAmount = 0;

                                if (message.CompanyCarRoadList.InCityKilometers > 0)
                                    message.CompanyCarRoadList.FuelAmount =
                                        Math.Round(message.CompanyCarRoadList.FuelAmount + message.CompanyCarRoadList.InCityKilometers * carFromDb.InCityConsumption / 100,
                                            2);
                                if (message.CompanyCarRoadList.OutsideCityKilometers > 0)
                                    message.CompanyCarRoadList.FuelAmount =
                                        Math.Round(
                                            message.CompanyCarRoadList.FuelAmount +
                                            message.CompanyCarRoadList.OutsideCityKilometers * carFromDb.OutsideCityConsumption / 100, 2);
                                if (message.CompanyCarRoadList.MixedModeKilometers > 0)
                                    message.CompanyCarRoadList.FuelAmount =
                                        Math.Round(
                                            message.CompanyCarRoadList.FuelAmount + message.CompanyCarRoadList.MixedModeKilometers * carFromDb.MixedModeConsumption / 100,
                                            2);

                                message.CompanyCarRoadList.FuelAmount =
                                    Math.Round(
                                        message.CompanyCarRoadList.FuelAmount +
                                        (message.CompanyCarRoadList.TotalKilometers - inModesKilometers) * carFromDb.InCityConsumption / 100, 2);
                            }

                            carFromDb.FuelAmount = Math.Round(carFromDb.FuelAmount - message.CompanyCarRoadList.FuelAmount, 2);

                            message.CompanyCarRoadList.Id = companyCarRoadListRepository.Add(message.CompanyCarRoadList);

                            if (message.CompanyCarRoadList.CompanyCarRoadListDrivers.Any())
                                _consumablesRepositoriesFactory
                                    .NewCompanyCarRoadListDriverRepository(connection)
                                    .Add(
                                        message
                                            .CompanyCarRoadList
                                            .CompanyCarRoadListDrivers
                                            .Where(d => d.User != null && !d.User.IsNew())
                                            .Select(d => {
                                                d.UserId = d.User.Id;
                                                d.CompanyCarRoadListId = message.CompanyCarRoadList.Id;

                                                return d;
                                            })
                                    );

                            carFromDb.Mileage += message.CompanyCarRoadList.TotalKilometers;

                            companyCarRepository.Update(carFromDb);

                            Sender.Tell(new Tuple<CompanyCarRoadList, string>(companyCarRoadListRepository.GetById(message.CompanyCarRoadList.Id), string.Empty));
                        }
                    }
                }
            }
        });

        Receive<UpdateCompanyCarRoadListMessage>(message => {
            if (message.CompanyCarRoadList == null) {
                Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, "Empty CompanyCarRoadList is not valid input for current request"));
            } else if (message.CompanyCarRoadList.IsNew()) {
                Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, "New CompanyCarRoadList is not valid input for current request"));
            } else if (message.CompanyCarRoadList.Mileage <= 0) {
                Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, CompanyCarRoadListResourceNames.MILEAGE_NOT_SPECIFIED));
            } else if (message.CompanyCarRoadList.Responsible == null && message.CompanyCarRoadList.ResponsibleId.Equals(0)) {
                Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, CompanyCarRoadListResourceNames.RESPONSIBLE_NOT_SPECIFIED));
            } else if (message.CompanyCarRoadList.CompanyCar == null && message.CompanyCarRoadList.CompanyCarId.Equals(0)) {
                Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, CompanyCarRoadListResourceNames.COMPANY_CAR_NOT_SPECIFIED));
            } else if (message.CompanyCarRoadList.OutcomePaymentOrder == null && message.CompanyCarRoadList.OutcomePaymentOrderId == 0) {
                Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, CompanyCarRoadListResourceNames.OUTCOME_NOT_SPECIFIED));
            } else {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                ICompanyCarRepository companyCarRepository = _consumablesRepositoriesFactory.NewCompanyCarRepository(connection);
                ICompanyCarRoadListRepository companyCarRoadListRepository = _consumablesRepositoriesFactory.NewCompanyCarRoadListRepository(connection);

                if (message.CompanyCarRoadList.Responsible != null) message.CompanyCarRoadList.ResponsibleId = message.CompanyCarRoadList.Responsible.Id;
                if (message.CompanyCarRoadList.CompanyCar != null) message.CompanyCarRoadList.CompanyCarId = message.CompanyCarRoadList.CompanyCar.Id;
                if (message.CompanyCarRoadList.OutcomePaymentOrder != null)
                    message.CompanyCarRoadList.OutcomePaymentOrderId = message.CompanyCarRoadList.OutcomePaymentOrder.Id;

                CompanyCar carFromDb = companyCarRepository.GetById(message.CompanyCarRoadList.CompanyCarId);

                if (carFromDb == null) {
                    Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, CompanyCarRoadListResourceNames.COMPANY_CAR_NOT_EXISTS));
                } else {
                    CompanyCarRoadList roadListFromDb = companyCarRoadListRepository.GetById(message.CompanyCarRoadList.Id);

                    int mileageDifference = Convert.ToInt32(message.CompanyCarRoadList.Mileage - roadListFromDb.Mileage);

                    message.CompanyCarRoadList.TotalKilometers = roadListFromDb.TotalKilometers + mileageDifference;

                    int inModesKilometers = message.CompanyCarRoadList.InCityKilometers + message.CompanyCarRoadList.OutsideCityKilometers +
                                            message.CompanyCarRoadList.MixedModeKilometers;

                    if (message.CompanyCarRoadList.TotalKilometers < inModesKilometers) {
                        Sender.Tell(new Tuple<CompanyCarRoadList, string>(null, CompanyCarRoadListResourceNames.SUM_OF_KILOMITERS_MORE_THAN_IN_MODES_SUM));
                    } else {
                        if (message.CompanyCarRoadList.InCityKilometers.Equals(0) && message.CompanyCarRoadList.OutsideCityKilometers.Equals(0) &&
                            message.CompanyCarRoadList.MixedModeKilometers.Equals(0)) {
                            message.CompanyCarRoadList.FuelAmount = Math.Round(message.CompanyCarRoadList.TotalKilometers * carFromDb.InCityConsumption / 100, 2);
                        } else {
                            message.CompanyCarRoadList.FuelAmount = 0;

                            if (message.CompanyCarRoadList.InCityKilometers > 0)
                                message.CompanyCarRoadList.FuelAmount =
                                    Math.Round(message.CompanyCarRoadList.FuelAmount + message.CompanyCarRoadList.InCityKilometers * carFromDb.InCityConsumption / 100, 2);
                            if (message.CompanyCarRoadList.OutsideCityKilometers > 0)
                                message.CompanyCarRoadList.FuelAmount =
                                    Math.Round(
                                        message.CompanyCarRoadList.FuelAmount + message.CompanyCarRoadList.OutsideCityKilometers * carFromDb.OutsideCityConsumption / 100,
                                        2);
                            if (message.CompanyCarRoadList.MixedModeKilometers > 0)
                                message.CompanyCarRoadList.FuelAmount =
                                    Math.Round(
                                        message.CompanyCarRoadList.FuelAmount + message.CompanyCarRoadList.MixedModeKilometers * carFromDb.MixedModeConsumption / 100, 2);

                            message.CompanyCarRoadList.FuelAmount =
                                Math.Round(
                                    message.CompanyCarRoadList.FuelAmount +
                                    (message.CompanyCarRoadList.TotalKilometers - inModesKilometers) * carFromDb.InCityConsumption / 100, 2);
                        }

                        carFromDb.FuelAmount -= Math.Round(message.CompanyCarRoadList.FuelAmount - roadListFromDb.FuelAmount, 2);

                        companyCarRoadListRepository.Update(message.CompanyCarRoadList);

                        if (message.CompanyCarRoadList.CompanyCarRoadListDrivers.Any()) {
                            _consumablesRepositoriesFactory
                                .NewCompanyCarRoadListDriverRepository(connection)
                                .Add(
                                    message
                                        .CompanyCarRoadList
                                        .CompanyCarRoadListDrivers
                                        .Where(d => d.User != null && !d.User.IsNew() && d.IsNew() && !d.Deleted)
                                        .Select(d => {
                                            d.UserId = d.User.Id;
                                            d.CompanyCarRoadListId = message.CompanyCarRoadList.Id;

                                            return d;
                                        })
                                );

                            _consumablesRepositoriesFactory
                                .NewCompanyCarRoadListDriverRepository(connection)
                                .Update(
                                    message
                                        .CompanyCarRoadList
                                        .CompanyCarRoadListDrivers
                                        .Where(d => d.User != null && !d.User.IsNew() && !d.IsNew() && !d.Deleted)
                                        .Select(d => {
                                            d.UserId = d.User.Id;
                                            d.CompanyCarRoadListId = message.CompanyCarRoadList.Id;

                                            return d;
                                        })
                                );

                            _consumablesRepositoriesFactory
                                .NewCompanyCarRoadListDriverRepository(connection)
                                .RemoveAllByIds(
                                    message
                                        .CompanyCarRoadList
                                        .CompanyCarRoadListDrivers
                                        .Where(d => !d.IsNew() && d.Deleted)
                                        .Select(d => d.Id)
                                );
                        }

                        carFromDb.Mileage += mileageDifference;

                        companyCarRepository.Update(carFromDb);

                        Sender.Tell(new Tuple<CompanyCarRoadList, string>(companyCarRoadListRepository.GetById(message.CompanyCarRoadList.Id), string.Empty));
                    }
                }
            }
        });

        Receive<GetAllCompanyCarRoadListsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewCompanyCarRoadListRepository(connection).GetAll());
        });

        Receive<GetAllCompanyCarRoadListsFilteredMessage>(message => {
            if (message.From.Year.Equals(1)) message.From = DateTime.UtcNow;
            if (message.To.Year.Equals(1)) message.To = DateTime.UtcNow;

            message.To = message.To.AddHours(23).AddMinutes(59).AddSeconds(59);

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewCompanyCarRoadListRepository(connection).GetAll(message.CompanyCarNetId, message.From, message.To));
        });

        Receive<GetCompanyCarRoadListByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewCompanyCarRoadListRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeleteCompanyCarRoadListByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ICompanyCarRepository companyCarRepository = _consumablesRepositoriesFactory.NewCompanyCarRepository(connection);
            ICompanyCarRoadListRepository companyCarRoadListRepository = _consumablesRepositoriesFactory.NewCompanyCarRoadListRepository(connection);

            CompanyCarRoadList fromDb = companyCarRoadListRepository.GetByNetId(message.NetId);

            if (fromDb != null) {
                if (!fromDb.Deleted) {
                    fromDb.CompanyCar.Mileage -= fromDb.TotalKilometers;

                    fromDb.CompanyCar.FuelAmount = Math.Round(fromDb.CompanyCar.FuelAmount + fromDb.FuelAmount, 2);

                    companyCarRepository.Update(fromDb.CompanyCar);

                    companyCarRoadListRepository.Remove(message.NetId);
                }

                Sender.Tell(companyCarRepository.GetById(fromDb.CompanyCar.Id));
            }
        });

        Receive<CalculateCompanyCarRoadListMessage>(message => {
            if (message.CompanyCarRoadList.CompanyCar == null && message.CompanyCarRoadList.CompanyCarId.Equals(0)) {
                Sender.Tell(message.CompanyCarRoadList);
            } else {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                if (message.CompanyCarRoadList.CompanyCar != null) message.CompanyCarRoadList.CompanyCarId = message.CompanyCarRoadList.CompanyCar.Id;
                if (message.CompanyCarRoadList.InCityKilometers < 0) message.CompanyCarRoadList.InCityKilometers = 0;
                if (message.CompanyCarRoadList.OutsideCityKilometers < 0) message.CompanyCarRoadList.OutsideCityKilometers = 0;
                if (message.CompanyCarRoadList.MixedModeKilometers < 0) message.CompanyCarRoadList.MixedModeKilometers = 0;

                CompanyCar carFromDb = _consumablesRepositoriesFactory.NewCompanyCarRepository(connection).GetById(message.CompanyCarRoadList.CompanyCarId);

                if (carFromDb == null) {
                    Sender.Tell(message.CompanyCarRoadList);
                } else {
                    message.CompanyCarRoadList.TotalKilometers = Convert.ToInt32(message.CompanyCarRoadList.Mileage - carFromDb.Mileage);

                    int inModesKilometers = message.CompanyCarRoadList.InCityKilometers + message.CompanyCarRoadList.OutsideCityKilometers +
                                            message.CompanyCarRoadList.MixedModeKilometers;

                    if (message.CompanyCarRoadList.TotalKilometers < inModesKilometers) {
                        Sender.Tell(message.CompanyCarRoadList);
                    } else {
                        if (message.CompanyCarRoadList.InCityKilometers.Equals(0) && message.CompanyCarRoadList.OutsideCityKilometers.Equals(0) &&
                            message.CompanyCarRoadList.MixedModeKilometers.Equals(0)) {
                            message.CompanyCarRoadList.FuelAmount = Math.Round(message.CompanyCarRoadList.TotalKilometers * carFromDb.InCityConsumption / 100, 2);
                        } else {
                            message.CompanyCarRoadList.FuelAmount = 0;

                            if (message.CompanyCarRoadList.InCityKilometers > 0)
                                message.CompanyCarRoadList.FuelAmount =
                                    Math.Round(message.CompanyCarRoadList.FuelAmount + message.CompanyCarRoadList.InCityKilometers * carFromDb.InCityConsumption / 100, 2);
                            if (message.CompanyCarRoadList.OutsideCityKilometers > 0)
                                message.CompanyCarRoadList.FuelAmount =
                                    Math.Round(
                                        message.CompanyCarRoadList.FuelAmount + message.CompanyCarRoadList.OutsideCityKilometers * carFromDb.OutsideCityConsumption / 100,
                                        2);
                            if (message.CompanyCarRoadList.MixedModeKilometers > 0)
                                message.CompanyCarRoadList.FuelAmount =
                                    Math.Round(
                                        message.CompanyCarRoadList.FuelAmount + message.CompanyCarRoadList.MixedModeKilometers * carFromDb.MixedModeConsumption / 100, 2);

                            message.CompanyCarRoadList.FuelAmount =
                                Math.Round(
                                    message.CompanyCarRoadList.FuelAmount +
                                    (message.CompanyCarRoadList.TotalKilometers - inModesKilometers) * carFromDb.InCityConsumption / 100, 2);
                        }

                        Sender.Tell(message.CompanyCarRoadList);
                    }
                }
            }
        });
    }
}