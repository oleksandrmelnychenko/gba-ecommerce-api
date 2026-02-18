using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Akka.Actor;
using GBA.Common;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Messages.Consumables.CompanyCars;
using GBA.Domain.Repositories.Consumables.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Consumables;

public sealed class CompanyCarActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsumablesRepositoriesFactory _consumablesRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public CompanyCarActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IConsumablesRepositoriesFactory consumablesRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _consumablesRepositoriesFactory = consumablesRepositoriesFactory;

        Receive<AddNewCompanyCarMessage>(message => {
            if (message.CompanyCar.Organization == null) {
                Sender.Tell(new Tuple<CompanyCar, string>(null, CompanyCarResourceNames.ORGANIZATION_NOT_SPECIFIED));
            } else {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                ICompanyCarRepository companyCarRepository = _consumablesRepositoriesFactory.NewCompanyCarRepository(connection);

                if (message.CompanyCar.Organization != null) message.CompanyCar.OrganizationId = message.CompanyCar.Organization.Id;

                message.CompanyCar.InitialMileage = message.CompanyCar.Mileage;

                message.CompanyCar.CreatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                message.CompanyCar.ConsumablesStorageId = _consumablesRepositoriesFactory.NewConsumablesStorageRepository(connection).Add(
                    new ConsumablesStorage {
                        ResponsibleUserId = message.CompanyCar.CreatedById,
                        Name = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("uk")
                            ? $"{message.CompanyCar.LicensePlate} - {SharedStrings.STORAGE_UK}"
                            : $"{message.CompanyCar.LicensePlate} - {SharedStrings.STORAGE_PL}",
                        OrganizationId = message.CompanyCar.OrganizationId
                    });

                message.CompanyCar.Id = companyCarRepository.Add(message.CompanyCar);

                Sender.Tell(new Tuple<CompanyCar, string>(companyCarRepository.GetById(message.CompanyCar.Id), string.Empty));
            }
        });

        Receive<UpdateCompanyCarMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ICompanyCarRepository companyCarRepository = _consumablesRepositoriesFactory.NewCompanyCarRepository(connection);

            message.CompanyCar.UpdatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

            if (message.CompanyCar.Organization != null)
                message.CompanyCar.OrganizationId = message.CompanyCar.Organization.Id;

            companyCarRepository.Update(message.CompanyCar);

            if (message.CompanyCar.ConsumablesStorage != null && !message.CompanyCar.ConsumablesStorage.IsNew()) {
                message.CompanyCar.ConsumablesStorage.Name = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("uk")
                    ? $"{message.CompanyCar.LicensePlate} - {SharedStrings.STORAGE_UK}"
                    : $"{message.CompanyCar.LicensePlate} - {SharedStrings.STORAGE_PL}";

                _consumablesRepositoriesFactory.NewConsumablesStorageRepository(connection).Update(message.CompanyCar.ConsumablesStorage);
            }

            Sender.Tell(companyCarRepository.GetById(message.CompanyCar.Id));
        });

        Receive<GetCompanyCarByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewCompanyCarRepository(connection).GetByNetId(message.NetId));
        });

        Receive<GetAllCompanyCarsMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewCompanyCarRepository(connection).GetAll());
        });

        Receive<GetAllCompanyCarsFromSearchMessage>(message => {
            if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewCompanyCarRepository(connection).GetAllFromSearch(message.Value));
        });

        Receive<DeleteCompanyCarByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ICompanyCarRepository companyCarRepository = _consumablesRepositoriesFactory.NewCompanyCarRepository(connection);

            CompanyCar fromDb = companyCarRepository.GetByNetId(message.NetId);

            if (fromDb != null) {
                companyCarRepository.Remove(message.NetId);

                if (fromDb.ConsumablesStorage != null) _consumablesRepositoriesFactory.NewConsumablesStorageRepository(connection).Remove(fromDb.ConsumablesStorage.NetUid);
            }
        });

        Receive<CalculateCompanyCarFuelingsMessage>(message => {
            foreach (CompanyCarFueling fueling in message.CompanyCarFuelings)
                if (fueling.FuelAmount > 0 && (fueling.PricePerLiter > decimal.Zero || fueling.TotalPriceWithVat > decimal.Zero)) {
                    if (fueling.PricePerLiter > decimal.Zero)
                        fueling.TotalPriceWithVat = Math.Round(fueling.PricePerLiter * Convert.ToDecimal(fueling.FuelAmount), 2);
                    else
                        fueling.PricePerLiter = Math.Round(fueling.TotalPriceWithVat / Convert.ToDecimal(fueling.FuelAmount), 2);

                    if (fueling.VatPercent > 0) {
                        fueling.VatAmount = Math.Round(fueling.TotalPriceWithVat * Convert.ToDecimal(fueling.VatPercent) / (100m + Convert.ToDecimal(fueling.VatPercent)), 2);

                        fueling.TotalPrice = Math.Round(fueling.TotalPriceWithVat - fueling.VatAmount, 2);
                    } else if (fueling.VatAmount > decimal.Zero) {
                        fueling.VatPercent = Math.Round(Convert.ToDouble(fueling.VatAmount / (fueling.TotalPriceWithVat - fueling.VatAmount) * 100), 2);

                        fueling.TotalPrice = Math.Round(fueling.TotalPriceWithVat - fueling.VatAmount, 2);
                    }
                }

            Sender.Tell(new Tuple<IEnumerable<CompanyCarFueling>, decimal>(message.CompanyCarFuelings, Math.Round(message.CompanyCarFuelings.Sum(f => f.TotalPrice), 2)));
        });
    }
}