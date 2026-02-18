using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.EntityHelpers.ReSaleModels;
using GBA.Domain.Messages.ReSales;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.VatRates.Contracts;
using Google.OrTools.LinearSolver;
using Constraint = Google.OrTools.LinearSolver.Constraint;

namespace GBA.Services.Actors.ReSales;

public sealed class ReSaleAvailabilityActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IReSaleRepositoriesFactory _reSaleRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly IVatRateRepositoriesFactory _vatRateRepositoriesFactory;

    public ReSaleAvailabilityActor(
        IDbConnectionFactory connectionFactory,
        IReSaleRepositoriesFactory reSaleRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IVatRateRepositoriesFactory vatRateRepositoriesFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _reSaleRepositoriesFactory = reSaleRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _vatRateRepositoriesFactory = vatRateRepositoriesFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;

        Receive<GetAllReSaleAvailabilitiesFilteredMessage>(ProcessGetAllReSaleAvailabilitiesFiltered);

        Receive<GetAllReSaleAvailabilitySpecificationCodesMessage>(ProcessGetAllReSaleAvailabilitySpecificationCodes);

        Receive<UpdateReSaleAvailabilityListMessage>(ProcessUpdateReSaleAvailabilityList);

        Receive<GetAllReSaleAvailabilityFilterOptionsMessage>(ProcessGetAllReSaleAvailabilityFilterOptions);

        Receive<GetGenerateAutomaticallyReSaleMessage>(ProcessGetGenerateAutomaticallyReSale);
    }

    private void ProcessGetAllReSaleAvailabilitiesFiltered(GetAllReSaleAvailabilitiesFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _reSaleRepositoriesFactory
                .NewReSaleAvailabilityRepository(connection)
                .GetAllItemsFiltered(
                    message.Filter.ExtraChargePercent,
                    message.Filter.IncludedProductGroups,
                    message.Filter.IncludedStorages,
                    message.Filter.IncludedSpecificationCodes,
                    message.Filter.Search.Trim()
                )
        );
    }

    private void ProcessGetAllReSaleAvailabilitySpecificationCodes(GetAllReSaleAvailabilitySpecificationCodesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _reSaleRepositoriesFactory
                .NewReSaleAvailabilityRepository(connection)
                .GetAllReSaleAvailabilitySpecificationCodes()
        );
    }

    private void ProcessUpdateReSaleAvailabilityList(UpdateReSaleAvailabilityListMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Organization organization =
                _organizationRepositoriesFactory
                    .NewOrganizationRepository(connection)
                    .GetById(message.ReSaleAvailabilityItemModels.FirstOrDefault()?.OrganizationId ?? 0);

            decimal vatPercent = 0;

            if (organization == null) {
                Sender.Tell(new List<UpdateReSaleAvailabilityListMessage>());
                return;
            }

            if (organization.VatRateId.HasValue)
                vatPercent = Convert.ToDecimal(_vatRateRepositoriesFactory.NewVatRateRepository(connection).GetById(organization.VatRateId.Value)?.Value ?? 0);

            message.ReSaleAvailabilityItemModels = GetReSaleAvailabilityItemModels(
                message.ReSaleAvailabilityItemModels,
                vatPercent);

            Sender.Tell(new CreatedReSaleAvailabilityWithTotalModel {
                ReSaleAvailabilityItemModels = message.ReSaleAvailabilityItemModels,
                Qty = message.ReSaleAvailabilityItemModels.Select(x => x.QtyToReSale).Sum(),
                Value = message.ReSaleAvailabilityItemModels.Select(x => x.Amount).Sum(),
                Vat = message.ReSaleAvailabilityItemModels.Select(x => x.Vat).Sum(),
                Weight = message.ReSaleAvailabilityItemModels.Select(x => x.Weight * x.QtyToReSale).Sum(),
                Organization = organization
            });
        } catch {
            Sender.Tell(new CreatedReSaleAvailabilityWithTotalModel());
        }
    }

    private void ProcessGetAllReSaleAvailabilityFilterOptions(GetAllReSaleAvailabilityFilterOptionsMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);
            IProductGroupRepository productGroupRepository = _productRepositoriesFactory.NewProductGroupRepository(connection);

            Sender.Tell(new ReSaleAvailabilityFilterOptions {
                SpecificationCodes = reSaleAvailabilityRepository
                    .GetAllReSaleAvailabilitySpecificationCodes(),
                Storages = storageRepository.GetAllForReSaleAvailabilities(),
                ProductGroups = productGroupRepository.GetAllForReSaleAvailabilities()
            });
        } catch {
            Sender.Tell(new ReSaleAvailabilityFilterOptions());
        }
    }

    private void ProcessGetGenerateAutomaticallyReSale(GetGenerateAutomaticallyReSaleMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.Filter.Amount <= 0) {
                Sender.Tell(new Exception(ReSaleResourceNames.ENTER_AMOUNT));
                return;
            }

            if (message.Filter.PossibleAmountDistinct <= 0) {
                Sender.Tell(new Exception(ReSaleResourceNames.ENTER_DISTINCT));
                return;
            }

            ReSaleAvailabilityWithTotalsModel reSaleAvailabilityWithTotal =
                _reSaleRepositoriesFactory
                    .NewReSaleAvailabilityRepository(connection)
                    .GetAllItemsFiltered(
                        message.Filter.ExtraChargePercent,
                        message.Filter.IncludedProductGroups,
                        message.Filter.IncludedStorages,
                        message.Filter.IncludedSpecificationCodes,
                        message.Filter.Search.Trim(),
                        message.Filter.SelectedStorageNetId);

            ReSaleAvailabilityItemModel[] reSaleAvailabilityItemModels =
                reSaleAvailabilityWithTotal.GroupReSaleAvailabilities
                    .Select(item => new ReSaleAvailabilityItemModel {
                        Price = item.AccountingGrossPrice,
                        SalePrice = item.SalePrice,
                        Amount = item.TotalSalePrice,
                        Qty = item.Qty,
                        QtyToReSale = item.Qty,
                        OldValue = new ReSaleAvailabilityOldValue {
                            Amount = item.TotalSalePrice,
                            QtyToReSale = item.Qty,
                            SalePrice = item.SalePrice
                        },
                        Weight = item.Weight,
                        MeasureUnit = item.MeasureUnit,
                        OrganizationId = item.FromStorage.OrganizationId ?? 0,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        SpecificationCode = item.SpecificationCode,
                        VendorCode = item.VendorCode,
                        FromStorageId = item.FromStorage.Id,
                        ExchangeRate = item.ExchangeRate
                    })
                    .ToArray();

            double minAmountLimit = Convert.ToDouble(message.Filter.Amount - message.Filter.PossibleAmountDistinct);
            double maxAmountLimit = Convert.ToDouble(message.Filter.Amount + message.Filter.PossibleAmountDistinct);

            double sumAmount = Convert.ToDouble(reSaleAvailabilityItemModels.Sum(x => x.Amount));

            int countItems = reSaleAvailabilityItemModels.Length;

            List<ReSaleAvailabilityItemModel> resaleAvailabilities = new();

            if (minAmountLimit <= sumAmount) {
                Solver solver = Solver.CreateSolver("CBC");

                // Initialize constraints
                Constraint ctCost =
                    solver.MakeConstraint(
                        minAmountLimit,
                        maxAmountLimit,
                        "ctCost"
                    );

                Variable[] variables = new Variable[countItems];

                // Initialize objective function
                Objective objective = solver.Objective();
                objective.SetMaximization();

                for (int i = 0; i < countItems; i++) {
                    double variableUpperBound = reSaleAvailabilityItemModels[i].QtyToReSale;

                    string name = reSaleAvailabilityItemModels[i].ProductId.ToString() + reSaleAvailabilityItemModels[i].FromStorageId.ToString();

                    // Variable
                    variables[i] = solver.MakeIntVar(0.0, variableUpperBound, name);

                    // Summands of constraints                            
                    double salePrice = (double)reSaleAvailabilityItemModels[i].SalePrice;
                    ctCost.SetCoefficient(variables[i], salePrice);

                    // Summand of objective function
                    objective.SetCoefficient(variables[i], salePrice);
                }

                Solver.ResultStatus resultStatus = solver.Solve();

                if (resultStatus != Solver.ResultStatus.FEASIBLE && resultStatus != Solver.ResultStatus.OPTIMAL) {
                    Sender.Tell(new Exception(ReSaleResourceNames.OPTIMAL_SOLVE_NOT_EXIST));
                    return;
                }

                double solveTotalAmount = solver.Objective().Value();

                if (solveTotalAmount > maxAmountLimit || solveTotalAmount < minAmountLimit) {
                    Sender.Tell(new Exception(ReSaleResourceNames.OPTIMAL_SOLVE_NOT_EXIST));
                    return;
                }

                for (int i = 0; i < countItems; i++) {
                    double itemReSaleQuantity = variables[i].SolutionValue();

                    if (itemReSaleQuantity.Equals(0))
                        continue;

                    reSaleAvailabilityItemModels[i].QtyToReSale = itemReSaleQuantity;
                    reSaleAvailabilityItemModels[i].Amount = reSaleAvailabilityItemModels[i].SalePrice * Convert.ToDecimal(itemReSaleQuantity);

                    resaleAvailabilities.Add(reSaleAvailabilityItemModels[i]);
                }
            } else {
                resaleAvailabilities = reSaleAvailabilityItemModels.ToList();
            }

            Organization organization =
                _organizationRepositoriesFactory
                    .NewOrganizationRepository(connection)
                    .GetById(reSaleAvailabilityItemModels.FirstOrDefault()?.OrganizationId ?? 0);

            decimal vatPercent = 0;

            if (organization == null) {
                Sender.Tell(new List<UpdateReSaleAvailabilityListMessage>());
                return;
            }

            if (organization.VatRateId.HasValue)
                vatPercent = Convert.ToDecimal(_vatRateRepositoriesFactory.NewVatRateRepository(connection).GetById(organization.VatRateId.Value)?.Value ?? 0);

            resaleAvailabilities = GetReSaleAvailabilityItemModels(
                resaleAvailabilities,
                vatPercent);

            Sender.Tell(new CreatedReSaleAvailabilityWithTotalModel {
                ReSaleAvailabilityItemModels = resaleAvailabilities,
                Qty = resaleAvailabilities.Select(x => x.QtyToReSale).Sum(),
                Value = resaleAvailabilities.Select(x => x.Amount).Sum(),
                Vat = resaleAvailabilities.Select(x => x.Vat).Sum(),
                Weight = resaleAvailabilities.Select(x => x.Weight * x.QtyToReSale).Sum(),
                Organization = organization
            });
        } catch (Exception e) {
            Sender.Tell(e);
        }
    }

    private List<ReSaleAvailabilityItemModel> GetReSaleAvailabilityItemModels(
        List<ReSaleAvailabilityItemModel> resaleAvailabilities,
        decimal vatPercent) {
        foreach (ReSaleAvailabilityItemModel updated in resaleAvailabilities) {
            if (!updated.Amount.Equals(updated.OldValue.Amount)) {
                updated.SalePrice = updated.Amount / Convert.ToDecimal(updated.QtyToReSale);
            } else if (!updated.SalePrice.Equals(updated.OldValue.SalePrice)) {
                updated.Amount = updated.SalePrice * Convert.ToDecimal(updated.QtyToReSale);
            } else if (!updated.QtyToReSale.Equals(updated.OldValue.QtyToReSale)) {
                if (updated.QtyToReSale > updated.Qty)
                    updated.QtyToReSale = updated.Qty;

                if (updated.QtyToReSale.Equals(0) || updated.QtyToReSale < 0)
                    updated.QtyToReSale = 1;

                updated.Amount = updated.SalePrice * Convert.ToDecimal(updated.QtyToReSale);
            } else {
                updated.Amount = updated.SalePrice * Convert.ToDecimal(updated.QtyToReSale);
            }

            decimal amountWithoutExtraCharge = Convert.ToDecimal(updated.QtyToReSale) * updated.Price;

            if (updated.Price.Equals(updated.SalePrice))
                updated.Profit = 0;
            else
                updated.Profit = updated.Amount - amountWithoutExtraCharge;

            if (updated.Profit.Equals(0))
                updated.Profitability = 0;
            else if (amountWithoutExtraCharge.Equals(0))
                updated.Profitability = 100;
            else
                updated.Profitability = updated.Amount / amountWithoutExtraCharge * 100 - 100;

            updated.Vat = updated.Amount * vatPercent / (100 + vatPercent);

            updated.Vat = decimal.Round(updated.Vat, 2, MidpointRounding.AwayFromZero);
            updated.Profitability = decimal.Round(updated.Profitability, 2, MidpointRounding.AwayFromZero);
            updated.Profit = decimal.Round(updated.Profit, 2, MidpointRounding.AwayFromZero);
        }

        return resaleAvailabilities;
    }
}