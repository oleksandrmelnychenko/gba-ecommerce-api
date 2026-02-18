using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Messages.Pricings;
using GBA.Domain.Repositories.Pricings.Contracts;

namespace GBA.Services.Actors.Pricings;

public sealed class PricingsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;

    public PricingsActor(
        IDbConnectionFactory connectionFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _pricingRepositoriesFactory = pricingRepositoriesFactory;

        Receive<AddPricingMessage>(ProcessAddPricingMessage);

        Receive<UpdatePricingMessage>(ProcessUpdatePricingMessage);

        Receive<UpdatePricingPriorityMessage>(ProcessUpdatePricingPriorityMessage);

        Receive<GetAllPricingsMessage>(ProcessGetAllPricingsMessage);

        Receive<GetAllBasePricingsMessage>(ProcessGetAllBasePricingsMessage);

        Receive<GetPricingByNetIdMessage>(ProcessGetPricingByNetIdMessage);

        Receive<DeletePricingMessage>(ProcessDeletePricingMessage);
    }

    private void ProcessAddPricingMessage(AddPricingMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPricingRepository pricingRepository = _pricingRepositoriesFactory.NewPricingRepository(connection);

        if (message.Pricing.BasePricingId == null || message.Pricing.BasePricingId == 0) message.Pricing.BasePricingId = message.Pricing?.BasePricing?.Id;

        if (message.Pricing.CurrencyId == null || message.Pricing.CurrencyId == 0) message.Pricing.CurrencyId = message.Pricing?.Currency?.Id;

        if (message.Pricing.PriceTypeId == null || message.Pricing.PriceTypeId == 0) message.Pricing.PriceTypeId = message.Pricing?.PriceType?.Id;

        if (string.IsNullOrEmpty(message.Pricing.Culture)) message.Pricing.Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        if (message.Pricing.BasePricing != null && !message.Pricing.BasePricing.IsNew()) {
            Pricing basePricing = pricingRepository.GetById(message.Pricing.BasePricing.Id);

            if (basePricing != null) {
                message.Pricing.BasePricingId = basePricing.Id;

                if (!basePricing.CalculatedExtraCharge.Equals(decimal.Zero) && message.Pricing.ExtraCharge.HasValue)
                    message.Pricing.CalculatedExtraCharge =
                        decimal.Round(
                            basePricing.CalculatedExtraCharge + Convert.ToDecimal(message.Pricing.ExtraCharge.Value) +
                            basePricing.CalculatedExtraCharge * Convert.ToDecimal(message.Pricing.ExtraCharge.Value) / 100,
                            4
                        );
                else
                    message.Pricing.CalculatedExtraCharge =
                        message.Pricing.ExtraCharge.HasValue ? Convert.ToDecimal(message.Pricing.ExtraCharge.Value) : decimal.Zero;
            } else {
                message.Pricing.CalculatedExtraCharge =
                    message.Pricing.ExtraCharge.HasValue ? Convert.ToDecimal(message.Pricing.ExtraCharge.Value) : decimal.Zero;
            }
        } else {
            message.Pricing.BasePricingId = null;
            message.Pricing.CalculatedExtraCharge =
                message.Pricing.ExtraCharge.HasValue ? Convert.ToDecimal(message.Pricing.ExtraCharge.Value) : decimal.Zero;
            message.Pricing.ExtraCharge = 0;
        }

        List<Pricing> pricings = pricingRepository.GetAll().OrderByDescending(p => p.SortingPriority).ToList();

        message.Pricing.SortingPriority = pricings.First().SortingPriority + 1;
        message.Pricing.Id = pricingRepository.Add(message.Pricing);

        if (message.Pricing.PricingTranslations.Any()) {
            message.Pricing.PricingTranslations.ToList().ForEach(t => {
                t.PricingId = message.Pricing.Id;

                if (string.IsNullOrEmpty(t.Name)) t.Name = string.Empty;
            });

            _pricingRepositoriesFactory.NewPricingTranslationRepository(connection).Add(message.Pricing.PricingTranslations);
        }

        Sender.Tell(pricingRepository.GetById(message.Pricing.Id));
    }

    private void ProcessUpdatePricingMessage(UpdatePricingMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPricingRepository pricingRepository = _pricingRepositoriesFactory.NewPricingRepository(connection);

        if (message.Pricing.Currency != null && !message.Pricing.Currency.IsNew()) message.Pricing.CurrencyId = message.Pricing.Currency.Id;

        if (message.Pricing.PriceType != null && !message.Pricing.PriceType.IsNew()) message.Pricing.PriceTypeId = message.Pricing.PriceType.Id;

        if (string.IsNullOrEmpty(message.Pricing.Culture)) message.Pricing.Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        if (message.Pricing.BasePricing != null && message.Pricing.BasePricing.Id.Equals(message.Pricing.Id)) {
            Sender.Tell(new Tuple<Pricing, string>(null, PricingResourceNames.PRICING_CAN_NOT_BE_BASE_FOR_IT_SELF));
        } else {
            if (message.Pricing.BasePricing != null && !message.Pricing.BasePricing.IsNew()) {
                message.Pricing.BasePricingId = message.Pricing.BasePricing.Id;

                Pricing basePricing = pricingRepository.GetById(message.Pricing.BasePricing.Id);

                if (basePricing != null) {
                    if (!basePricing.CalculatedExtraCharge.Equals(decimal.Zero) && message.Pricing.ExtraCharge.HasValue)
                        message.Pricing.CalculatedExtraCharge =
                            decimal.Round(
                                basePricing.CalculatedExtraCharge + Convert.ToDecimal(message.Pricing.ExtraCharge.Value) +
                                basePricing.CalculatedExtraCharge * Convert.ToDecimal(message.Pricing.ExtraCharge.Value) / 100,
                                4
                            );
                    else
                        message.Pricing.CalculatedExtraCharge =
                            message.Pricing.ExtraCharge.HasValue ? Convert.ToDecimal(message.Pricing.ExtraCharge.Value) : decimal.Zero;
                } else {
                    message.Pricing.CalculatedExtraCharge =
                        message.Pricing.ExtraCharge.HasValue ? Convert.ToDecimal(message.Pricing.ExtraCharge.Value) : decimal.Zero;
                }
            } else {
                message.Pricing.BasePricingId = null;
                message.Pricing.CalculatedExtraCharge =
                    message.Pricing.ExtraCharge.HasValue ? Convert.ToDecimal(message.Pricing.ExtraCharge.Value) : decimal.Zero;
                message.Pricing.ExtraCharge = 0;
            }

            pricingRepository.Update(message.Pricing);

            if (message.Pricing.PricingTranslations.Any()) {
                IPricingTranslationRepository pricingTranslationRepository = _pricingRepositoriesFactory.NewPricingTranslationRepository(connection);

                message.Pricing.PricingTranslations.ToList().ForEach(t => {
                    if (t.IsNew()) {
                        t.PricingId = message.Pricing.Id;

                        if (string.IsNullOrEmpty(t.Name)) t.Name = string.Empty;

                        pricingTranslationRepository.Add(t);
                    } else {
                        pricingTranslationRepository.Update(t);
                    }
                });
            }

            Sender.Tell(new Tuple<Pricing, string>(pricingRepository.GetByNetId(message.Pricing.NetUid), string.Empty));
        }
    }

    private void ProcessUpdatePricingPriorityMessage(UpdatePricingPriorityMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPricingRepository pricingRepository = _pricingRepositoriesFactory.NewPricingRepository(connection);

        List<Pricing> pricings = pricingRepository.GetAll();

        Pricing pricingFromDb = pricings.First(pricing => pricing.Id.Equals(message.PricingId));

        pricingFromDb.SortingPriority = message.Raise ? pricingFromDb.SortingPriority - 1 : pricingFromDb.SortingPriority + 1;

        if (pricingFromDb.SortingPriority >= 0 && pricingFromDb.SortingPriority < pricings.Count) {
            int currentPricingIndex = pricings.IndexOf(pricingFromDb);

            if (message.Raise) {
                Pricing pricingToLower = pricings[currentPricingIndex - 1];
                pricingToLower.SortingPriority += 1;
                pricingRepository.Update(pricingToLower);
            } else if (currentPricingIndex != pricings.Count - 1) {
                Pricing pricingToRaise = pricings[currentPricingIndex + 1];
                pricingToRaise.SortingPriority = pricingToRaise.SortingPriority <= 0
                    ? pricingToRaise.SortingPriority
                    : pricingToRaise.SortingPriority - 1;
                pricingRepository.Update(pricingToRaise);
            }

            pricingRepository.Update(pricingFromDb);
        }

        Sender.Tell(new Tuple<List<Pricing>, string>(pricingRepository.GetAll(), string.Empty));
    }

    private void ProcessGetAllPricingsMessage(GetAllPricingsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_pricingRepositoriesFactory.NewPricingRepository(connection).GetAll());
    }

    private void ProcessGetAllBasePricingsMessage(GetAllBasePricingsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_pricingRepositoriesFactory.NewPricingRepository(connection).GetAllBasePricings());
    }

    private void ProcessGetPricingByNetIdMessage(GetPricingByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_pricingRepositoriesFactory.NewPricingRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessDeletePricingMessage(DeletePricingMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPricingRepository pricingRepository = _pricingRepositoriesFactory.NewPricingRepository(connection);

        Pricing pricing = pricingRepository.GetByNetId(message.NetId);

        if (pricing != null)
            switch (pricing.PriceTypeId) {
                case 1 when !pricingRepository.IsAnyAssignedToBasePricing(pricing.Id):
                    pricingRepository.Remove(message.NetId);

                    Sender.Tell(new Tuple<string, Pricing>(string.Empty, pricing));
                    break;
                case 1:
                    Sender.Tell(new Tuple<string, Pricing>(PricingResourceNames.IN_USE_AS_BASE_PRICING, null));
                    break;
                default:
                    pricingRepository.Remove(message.NetId);

                    Sender.Tell(new Tuple<string, Pricing>(string.Empty, pricing));
                    break;
            }
        else
            Sender.Tell(new Tuple<string, Pricing>(string.Empty, null));
    }
}